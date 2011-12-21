﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.AccessControl;
using System.Threading;
using Gate.Owin;
using Gate.Utils;

namespace Gate.Middleware
{
    public static class RewindableBody
    {
        static readonly MethodInfo RewindableBodyInvoke = typeof (RewindableBody).GetMethod("Invoke");
        const int DefaultTempFileThresholdBytes = 64 << 10; //64k

        public static IAppBuilder UseRewindableBody(this IAppBuilder builder)
        {
            return builder.Use(Middleware);
        }

        static AppDelegate Middleware(AppDelegate app)
        {
            return (env, result, fault) =>
            {
                var owin = new Environment(env);
                owin.BodyAction = Wrap(owin.BodyAction);
                app(env, result, fault);
            };
        }

        public static Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action> Wrap(Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action> body)
        {
            if (body == null)
            {
                return null;
            }
            if (body.Method == RewindableBodyInvoke)
            {
                return body;
            }
            return new Wrapper(body, DefaultTempFileThresholdBytes).Invoke;
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
            return new Wrapper(body.ToAction(), DefaultTempFileThresholdBytes).Invoke;
        }


        class Wrapper // : IDisposable
        {
            readonly Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action> _body;
            readonly Spool _spool = new Spool();
            readonly Signal _spoolComplete = new Signal();

            int _invokeCount;
            readonly List<ArraySegment<byte>> _pages = new List<ArraySegment<byte>>();
            readonly int _tempFileThresholdBytes;
            string _tempFileName;
            FileStream _tempFile;

            public Wrapper(
                Func<Func<ArraySegment<byte>, Action, bool>, Action<Exception>, Action, Action> body,
                int tempFileThresholdBytes)
            {
                _body = body;
                _tempFileThresholdBytes = tempFileThresholdBytes;
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
                long totalBytes = 0;

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
                            case 3:
                                goto mark3;
                        }

                        //// MEMORY LOOP
                        mark4:

                        // grab data from the spool
                        retval[0] = 0;
                        if (_spool.Pull(new ArraySegment<byte>(buffer, 0, bufferSize), retval, () => state.Go(3)))
                            return;
                        mark3:

                        if (retval[0] <= 0)
                            goto markreturn;

                        if (state.Cancelled)
                            goto markreturn;

                        // add to memory until threshold reached
                        _pages.Add(new ArraySegment<byte>(buffer, 0, retval[0]));

                        buffer = new byte[bufferSize]; // todo: get these from a page pool

                        totalBytes += retval[0];

                        if (totalBytes < _tempFileThresholdBytes)
                            goto mark4;
                        //// MEMORY LOOP

                        // create temp file at this point
                        _tempFileName = Path.GetTempFileName();
                        _tempFile = new FileStream(
                            _tempFileName,
                            FileMode.Create,
                            FileSystemRights.WriteData | FileSystemRights.ReadData,
                            FileShare.None,
                            4096,
                            FileOptions.SequentialScan | FileOptions.Asynchronous | FileOptions.WriteThrough | FileOptions.DeleteOnClose);

                        //// TEMPFILE LOOP
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
                        //// TEMPFILE LOOP


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
                var pageEnumerator = _pages.GetEnumerator();

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
                            case 4:
                                goto mark4;
                        }

                        //// MEMORY LOOP
                        mark5:
                        // move to the next memory page
                        if (!pageEnumerator.MoveNext())
                            goto mark3;

                        // send along to output
                        if (next(pageEnumerator.Current, () => state.Go(4)))
                            return;
                        mark4:

                        if (state.Cancelled)
                            goto markreturn;
                        goto mark5;
                        //// MEMORY LOOP

                        mark3:

                        if (_tempFile == null)
                            goto markreturn;

                        _tempFile.Seek(0, SeekOrigin.Begin);


                        //// TEMPFILE LOOP
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
                        //// TEMPFILE LOOP

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

        class Signal
        {
            bool _signaled;
            Action _continuation;
    
            public void Set()
            {
                lock (this)
                {
                    _signaled = true;
                }
                if (_continuation != null)
                {
                    var continuation = _continuation;
                    _continuation = null;
                    continuation();
                }
            }
    
            public void Continue(Action continuation)
            {
                if (_signaled)
                {
                    continuation();
                    return;
                }
                lock (this)
                {
                    if (_signaled)
                    {
                        continuation();
                        return;
                    }
    
                    if (_continuation == null)
                    {
                        _continuation = continuation;
                    }
                    else
                    {
                        var prior = _continuation;
                        _continuation = () =>
                        {
                            prior();
                            continuation();
                        };
                    }
                }
            }
        }

        class StreamAwaiter
        {
            readonly Func<IAsyncResult, int> _end;
            IAsyncResult _result;
            Action _continuation;
    
            StreamAwaiter(Func<IAsyncResult, int> end)
            {
                _end = end;
            }
    
            public static StreamAwaiter Write(Stream stream, byte[] buffer, int offset, int count)
            {
                var awaiter = new StreamAwaiter(result =>
                {
                    stream.EndWrite(result);
                    return 0;
                });
    
                var sr = stream.BeginWrite(buffer, offset, count, ar => { if (!ar.CompletedSynchronously) awaiter.SetResult(ar); }, null);
    
                if (sr.CompletedSynchronously) awaiter.SetResult(sr);
    
                return awaiter;
            }
    
            public static StreamAwaiter Read(Stream stream, byte[] buffer, int offset, int count)
            {
                var awaiter = new StreamAwaiter(stream.EndRead);
    
                var sr = stream.BeginRead(buffer, offset, count, ar => { if (!ar.CompletedSynchronously) awaiter.SetResult(ar); }, null);
    
                if (sr.CompletedSynchronously) awaiter.SetResult(sr);
    
                return awaiter;
            }
    
            public bool BeginAwait(Action continuation)
            {
                lock (this)
                {
                    if (_result != null)
                        return false;
                    _continuation = continuation;
                    return true;
                }
            }
    
            public int EndAwait()
            {
                return _end(_result);
            }
    
            void SetResult(IAsyncResult result)
            {
                lock (this)
                {
                    _result = result;
                    if (_continuation != null)
                        _continuation();
                }
            }
        }
    }
}