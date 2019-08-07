namespace DataTanker.Utils
{
    using System;
    using System.Threading;

    internal class TimerHelper : IDisposable
    {
        private Timer _timer;
        private readonly object _locker = new object();

        public event Action<Timer, object> Elapsed;

        public void Start(TimeSpan timerInterval)
        {
            lock (_locker)
            {
                if (_timer == null)
                    _timer = new Timer(TimerElapsed, null, Timeout.Infinite, Timeout.Infinite);

                _timer.Change(timerInterval, TimeSpan.FromMilliseconds(-1));
            }
        }

        public void Stop()
        {
            lock (_locker)
            {
                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void Dispose()
        {
            lock (_locker)
            {
                if (_timer != null)
                {
                    WaitHandle waitHandle = new AutoResetEvent(false);
                    _timer.Dispose(waitHandle);
                    WaitHandle.WaitAll(new[] { waitHandle }, TimeSpan.FromMinutes(1));
                    _timer = null;
                }
            }
            Elapsed = null;
        }

        private void TimerElapsed(object state)
        {
            if (Monitor.TryEnter(_locker))
            {
                try
                {
                    if (_timer == null)
                        return;

                    Action<Timer, object> timerEvent = Elapsed;
                    timerEvent?.Invoke(_timer, state);
                }
                finally
                {
                    Monitor.Exit(_locker);
                }
            }
        }
    }
}