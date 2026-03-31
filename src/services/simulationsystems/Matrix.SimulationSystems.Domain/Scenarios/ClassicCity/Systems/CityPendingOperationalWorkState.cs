namespace Matrix.SimulationSystems.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityPendingOperationalWorkState
    {
        private CityPendingOperationalWorkState() { }

        private CityPendingOperationalWorkState(
            bool isScheduled,
            string focus,
            string intensity,
            long readyAtTickId)
        {
            IsScheduled = isScheduled;
            Focus = focus;
            Intensity = intensity;
            ReadyAtTickId = readyAtTickId;
        }

        public bool IsScheduled { get; private set; }
        public string Focus { get; private set; } = string.Empty;
        public string Intensity { get; private set; } = string.Empty;
        public long ReadyAtTickId { get; private set; }

        public static CityPendingOperationalWorkState None()
        {
            return new CityPendingOperationalWorkState(
                isScheduled: false,
                focus: string.Empty,
                intensity: string.Empty,
                readyAtTickId: 0);
        }

        public void Schedule(
            string focus,
            string intensity,
            long readyAtTickId)
        {
            IsScheduled = true;
            Focus = focus ?? string.Empty;
            Intensity = intensity ?? string.Empty;
            ReadyAtTickId = Math.Max(0, readyAtTickId);
        }

        public bool IsReady(long tickId)
        {
            return IsScheduled && tickId >= ReadyAtTickId;
        }

        public void Clear()
        {
            IsScheduled = false;
            Focus = string.Empty;
            Intensity = string.Empty;
            ReadyAtTickId = 0;
        }
    }
}
