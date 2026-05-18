using UnityEngine;

namespace Core.Extentions
{
    public struct SharedTimer
    {
        private float _endTime;
        public bool IsRunning { get; private set; }

        public void Start(float duration)
        {
            _endTime = Time.time + duration;
            IsRunning = true;
        }

        public void Stop() => IsRunning = false;

        public bool Tick()
        {
            if (!IsRunning) return false;
            if (Time.time < _endTime) return false;
            IsRunning = false;
            return true;
        }

        public float Remaining => IsRunning ? Mathf.Max(0f, _endTime - Time.time) : 0f;
        public float Progress(float duration) => IsRunning ? 1f - (Remaining / duration) : 1f;
    }
}
