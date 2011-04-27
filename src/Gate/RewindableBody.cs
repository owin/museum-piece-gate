using System;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using Gate.Utils;

namespace Gate
{
    using BodyAction = Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action>;

    /*
     * notes:
     * Should buffer something like 16*4k pages to memory before going into temp file.
     * Sink class can probably be folded up into main class
     */

    public class RewindableBody
    {
        readonly BodyAction _body;
        readonly Spool _spool;

        static readonly MethodInfo RewindableBodyInvoke = typeof (RewindableBody).GetMethod("Invoke");
        Sink _sink;
        Action _cancel;

        public RewindableBody(BodyAction body)
        {
            _body = body;
            _spool = new Spool(true);
        }

        public static BodyAction Wrap(BodyAction body)
        {
            if (body == null)
            {
                return null;
            }
            if (body.Method == RewindableBodyInvoke)
            {
                return body;
            }
            return new RewindableBody(body).Invoke;
        }

        public static BodyDelegate Wrap(BodyDelegate body)
        {
            if (body == null)
            {
                return null;
            }
            if (body.Method == RewindableBodyInvoke)
            {
                return body;
            }
            return new RewindableBody(body.ToAction()).Invoke;
        }

        public Action Invoke(
            Func<ArraySegment<byte>, Action, bool> next,
            Action<Exception> error,
            Action complete)
        {
            if (_sink == null)
            {
                _sink = new Sink(_spool);
                var cancel1 = _sink.Start();
                var cancel2 = _body(
                    (data, continuation) =>
                    {
                        _spool.Push(data, null);
                        return next(data, continuation);
                    },
                    ex =>
                    {
                        _spool.PushComplete();
                        error(ex);
                    },
                    () =>
                    {
                        _spool.PushComplete();
                        complete();
                    });
                return () =>
                {
                    cancel1();
                    cancel2();
                };
            }

            return _sink.Replay(next, error, complete);
        }

        class Sink // : IDisposable
        {
            readonly Spool _spool;
            string _tempFileName;
            FileStream _tempFile;
            readonly ManualResetEvent _wait = new ManualResetEvent(false);

            public Sink(Spool spool)
            {
                _spool = spool;
            }

            class State
            {
                public bool Cancelled;
                public Action<int> Go;
            }

            public Action Start()
            {
                const int size = 4096;
                var buffer = new byte[size];
                var retval = new[] {0};
                StreamAwaiter write = null;

                var state = new State();
                state.Go = mark =>
                {
                    try
                    {
                        switch (mark)
                        {
                            case 1:
                                goto mark1;
                            case 2:
                                goto mark2;
                        }

                        _tempFileName = Path.GetTempFileName();
                        _tempFile = new FileStream(
                            _tempFileName,
                            FileMode.Create,
                            FileSystemRights.WriteData | FileSystemRights.ReadData,
                            FileShare.None,
                            4096,
                            FileOptions.SequentialScan | FileOptions.Asynchronous | FileOptions.WriteThrough | FileOptions.DeleteOnClose);

                        mark0:

                        // grab data from the spool
                        retval[0] = 0;
                        if (_spool.Pull(new ArraySegment<byte>(buffer, 0, size), retval, () => state.Go(1)))
                            return;
                        mark1:

                        // no more data - normal completion
                        if (retval[0] == 0)
                            goto markreturn;

                        // cancelled from outside the loop
                        if (state.Cancelled)
                            goto markreturn;

                        // send along to temp file
                        write = StreamAwaiter.Write(_tempFile, buffer, 0, retval[0]);
                        if (write.BeginAwait(() => state.Go(2)))
                            return;
                        mark2:
                        write.EndAwait();

                        // cancelled from outside the loop
                        if (state.Cancelled)
                            goto markreturn;

                        goto mark0;

                        markreturn:
                        _wait.Set();
                    }
                    catch (Exception ex)
                    {
                        _wait.Set();
                    }
                };
                state.Go(0);
                return () => state.Cancelled = true;
            }

            public Action Replay(Func<ArraySegment<byte>, Action, bool> next, Action<Exception> error, Action complete)
            {
                if (!_wait.WaitOne(TimeSpan.FromMinutes(3)))
                    throw new TimeoutException();

                _tempFile.Seek(0, SeekOrigin.Begin);

                const int size = 4096;
                var buffer = new byte[size];
                StreamAwaiter read = null;

                var state = new State();
                state.Go = mark =>
                {
                    try
                    {
                        switch (mark)
                        {
                            case 1:
                                goto mark1;
                            case 2:
                                goto mark2;
                        }

                        mark0:

                        // read some data from the temp file
                        read = StreamAwaiter.Read(_tempFile, buffer, 0, size);
                        if (read.BeginAwait(() => state.Go(1)))
                            return;
                        mark1:
                        var count = read.EndAwait();

                        // end of file, normal completion
                        if (count == 0)
                            goto markreturn;

                        // cancelled from outside the loop
                        if (state.Cancelled)
                            return;

                        // send along to output
                        if (next(new ArraySegment<byte>(buffer, 0, count), () => state.Go(2)))
                            return;
                        mark2:

                        // cancelled from outside the loop
                        if (state.Cancelled)
                            return;

                        // loop for more
                        goto mark0;

                        markreturn:
                        complete();
                    }
                    catch (Exception ex)
                    {
                        error(ex);
                    }
                };
                state.Go(0);
                return () => state.Cancelled = true;
            }
        }
    }
}