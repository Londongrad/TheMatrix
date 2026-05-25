using Matrix.Resources.Domain.Scenarios.ClassicCity.Enums;

namespace Matrix.Resources.Domain.Scenarios.ClassicCity.Systems
{
    public sealed class CityPendingResupplyState
    {
        private CityPendingResupplyState() { }

        private CityPendingResupplyState(
            bool isScheduled,
            string focus,
            string intensity,
            Guid? focusDistrictId,
            long readyAtTickId)
        {
            IsScheduled = isScheduled;
            Focus = focus;
            Intensity = intensity;
            FocusDistrictId = focusDistrictId;
            ReadyAtTickId = readyAtTickId;
        }

        public bool IsScheduled { get; private set; }
        public string Focus { get; private set; } = string.Empty;
        public string Intensity { get; private set; } = string.Empty;
        public Guid? FocusDistrictId { get; private set; }
        public long ReadyAtTickId { get; private set; }

        public static CityPendingResupplyState None()
        {
            return new CityPendingResupplyState(
                isScheduled: false,
                focus: string.Empty,
                intensity: string.Empty,
                focusDistrictId: null,
                readyAtTickId: 0);
        }

        public void Schedule(
            ResupplyFocus focus,
            ResupplyIntensity intensity,
            Guid? focusDistrictId,
            long readyAtTickId)
        {
            IsScheduled = true;
            Focus = focus.ToString();
            Intensity = intensity.ToString();
            FocusDistrictId = focusDistrictId;
            ReadyAtTickId = Math.Max(
                val1: 0,
                val2: readyAtTickId);
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
            FocusDistrictId = null;
            ReadyAtTickId = 0;
        }
    }
}
