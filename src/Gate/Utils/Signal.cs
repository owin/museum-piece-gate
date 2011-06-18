using System;

namespace Gate.Utils
{
    public class Signal
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
}