namespace ToyControlApp.Models
{
    public class DeviceBinding
    {
        public string DeviceName { get; set; }
        public double Intensity { get; set; } // 0.0 to 1.0
        public int DurationMs { get; set; }
        public bool IsHoldMode { get; set; } // NEW: Hold mode flag

        public DeviceBinding()
        {
            DeviceName = "";
            Intensity = 0.5;
            DurationMs = 1000;
            IsHoldMode = false; // Default to timed mode
        }

        public DeviceBinding(string deviceName, double intensity, int durationMs, bool isHoldMode = false)
        {
            DeviceName = deviceName;
            Intensity = intensity;
            DurationMs = durationMs;
            IsHoldMode = isHoldMode;
        }

        public override string ToString()
        {
            if (IsHoldMode)
            {
                return $"{DeviceName} ({Intensity:P0}, Hold Mode)";
            }
            else
            {
                return $"{DeviceName} ({Intensity:P0}, {DurationMs}ms)";
            }
        }
    }
}