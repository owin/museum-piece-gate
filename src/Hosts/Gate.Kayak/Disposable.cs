﻿using System;

namespace Gate.Kayak
{
    class Disposable : IDisposable
    {
        readonly Action dispose;

        public Disposable(Action dispose)
        {
            this.dispose = dispose;
        }

        public void Dispose()
        {
            dispose();
        }
    }
}
