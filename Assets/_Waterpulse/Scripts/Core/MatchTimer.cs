namespace Waterpulse.Core
{
    // Cuenta atrás del partido. Solo avanza mientras está en marcha
    public class MatchTimer
    {
        public float Duration { get; }
        public float Remaining { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsExpired => Remaining <= 0f;

        public MatchTimer(float duration)
        {
            Duration = duration > 0f ? duration : 0f;
            Remaining = Duration;
        }

        public void Start() => IsRunning = true;
        public void Pause() => IsRunning = false;

        // Devuelve true solo en el tick en que se agota
        public bool Tick(float dt)
        {
            if (!IsRunning || IsExpired || dt <= 0f) return false;
            Remaining -= dt;
            if (Remaining > 0f) return false;
            Remaining = 0f;
            IsRunning = false;
            return true;
        }
    }
}
