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
     */

    public class RewindableBody : IMiddleware
    {
        static readonly MethodInfo RewindableBodyInvoke = typeof (RewindableBody).GetMethod("Invoke");

        AppDelegate IMiddleware.Create(AppDelegate app)
        {
            return Create(app);
        }

        public static AppDelegate Create(AppDelegate app)
        {
            return (env, result, fault) =>
            {
                var owin = new Environment(env);
                owin.Body = Wrap(owin.Body);
                app(env, result, fault);
            };
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
            return new Wrapper(body).Invoke;
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
            return new Wrapper(body.ToAction()).Invoke;
        }


        class Wrapper // : IDisposable
        {
            readonly BodyAction _body;
            readonly Spool _spool;
            readonly Signal _spoolComplete;

            int _invokeCount;
            string _tempFileName;
            FileStream _tempFile;

            public Wrapper(BodyAction body)
            {
                _body = body;
                _spool = new Spool();
                _spoolComplete = new Signal();
            }

            public Action Invoke(
                Func<ArraySegment<byte>, Action, bool> next,
                Action<Exception> error,
                Action complete)
            {
                if (Interlocked.Increment(ref _invokeCount) == 1)
                    return Start(next, error, complete);
                return Replay(next, error, complete);
            }

            class State
            {
                public bool Cancelled;
                public Action<int> Go;
            }

            public Action Start(
                Func<ArraySegment<byte>, Action, bool> next,
                Action<Exception> error,
                Action complete)
            {
                const int bufferSize = 4096;
                var buffer = new byte[bufferSize];
                var retval = new[] {0};
                StreamAwaiter write = null;

                // This machine moves data from the spool to the replay storage
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
                        if (_spool.Pull(new ArraySegment<byte>(buffer, 0, bufferSize), retval, () => state.Go(1)))
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
                        _spoolComplete.Set();
                    }
                    catch (Exception ex)
                    {
                        /* need to comminucate failures outward!!! */
                        _spoolComplete.Set();
                    }
                };
                state.Go(0);

                // This machine moves data from the source to the caller and the spool
                var bodyCancel = _body(
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

                // returns a cancellation of both machines
                return () =>
                {
                    state.Cancelled = true;
                    bodyCancel();
                };
            }

            public Action Replay(Func<ArraySegment<byte>, Action, bool> next, Action<Exception> error, Action complete)
            {
                const int bufferSize = 4096;
                var buffer = new byte[bufferSize];
                StreamAwaiter read = null;

                // This machine draws from the temp file and sends to the caller
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

                        _tempFile.Seek(0, SeekOrigin.Begin);

                        mark0:

                        // read some data from the temp file
                        read = StreamAwaiter.Read(_tempFile, buffer, 0, bufferSize);
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

                // starts the machine only when the earlier transfer form spool to file is fully complete
                _spoolComplete.Continue(() => state.Go(0));

                return () => state.Cancelled = true;
            }
        }
    }
}