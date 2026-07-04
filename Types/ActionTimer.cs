using System.Diagnostics;

namespace WFollowBot.Types
{
    public class ActionTimer
    {
        private readonly Stopwatch _stopwatch;
        private double _nextAllowedTime = 0;

        private double _lastDelay = 0;

        public ActionTimer()
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();
        }

        public bool TimerIsRunning => _stopwatch.IsRunning;

        public bool IsActionAllowed
        {
            get
            {
                return _stopwatch.Elapsed.TotalMilliseconds >= _nextAllowedTime;
            }
        }

        public double ElapsedTime => _stopwatch.Elapsed.TotalMilliseconds;

        public double RemainingTime
        {
            get
            {
                double remaining = _nextAllowedTime - _stopwatch.Elapsed.TotalMilliseconds;
                return remaining > 0 ? remaining : 0;
            }
        }

        public double LastDelay { get => _lastDelay; private set => _lastDelay = value; }

        public void SetDelay(double delayMilliseconds)
        {
            _nextAllowedTime = _stopwatch.Elapsed.TotalMilliseconds + delayMilliseconds;
            LastDelay = delayMilliseconds;
        }
        public void Start()
        {
            _stopwatch.Start();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public void Restart()
        {
            _stopwatch.Restart();
            _nextAllowedTime = 0;
        }

        public void ResetAndSetDelay(double delayMilliseconds)
        {
            _stopwatch.Restart();
            SetDelay(delayMilliseconds);
        }
    }
}
