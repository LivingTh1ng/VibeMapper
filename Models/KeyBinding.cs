using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace ToyControlApp.Models
{
    public class KeyBinding
    {
        public Keys Key { get; set; }
        public List<DeviceBinding> DeviceBindings { get; set; }
        public string Description { get; set; }

        public KeyBinding()
        {
            Key = Keys.None;
            DeviceBindings = new List<DeviceBinding>();
            Description = "";
        }

        // Backward compatibility properties
        public string DeviceName
        {
            get => DeviceBindings.FirstOrDefault()?.DeviceName ?? "";
            set
            {
                if (DeviceBindings.Count == 0)
                    DeviceBindings.Add(new DeviceBinding());
                DeviceBindings[0].DeviceName = value;
            }
        }

        public double Intensity
        {
            get => DeviceBindings.FirstOrDefault()?.Intensity ?? 0.5;
            set
            {
                if (DeviceBindings.Count == 0)
                    DeviceBindings.Add(new DeviceBinding());
                DeviceBindings[0].Intensity = value;
            }
        }

        public int DurationMs
        {
            get => DeviceBindings.FirstOrDefault()?.DurationMs ?? 1000;
            set
            {
                if (DeviceBindings.Count == 0)
                    DeviceBindings.Add(new DeviceBinding());
                DeviceBindings[0].DurationMs = value;
            }
        }

        public bool IsHoldMode
        {
            get => DeviceBindings.FirstOrDefault()?.IsHoldMode ?? false;
            set
            {
                if (DeviceBindings.Count == 0)
                    DeviceBindings.Add(new DeviceBinding());
                DeviceBindings[0].IsHoldMode = value;
            }
        }

        public void UpdateDescription()
        {
            if (DeviceBindings.Count == 0)
            {
                Description = $"{Key} → No devices";
            }
            else if (DeviceBindings.Count == 1)
            {
                var binding = DeviceBindings[0];
                if (binding.IsHoldMode)
                {
                    Description = $"{Key} → {binding.DeviceName} ({binding.Intensity:P0}, Hold Mode)";
                }
                else
                {
                    Description = $"{Key} → {binding.DeviceName} ({binding.Intensity:P0}, {binding.DurationMs}ms)";
                }
            }
            else
            {
                var holdModeCount = DeviceBindings.Count(d => d.IsHoldMode);
                var timedCount = DeviceBindings.Count - holdModeCount;

                if (holdModeCount > 0 && timedCount > 0)
                {
                    Description = $"{Key} → {DeviceBindings.Count} devices (Mixed: {holdModeCount} hold, {timedCount} timed)";
                }
                else if (holdModeCount > 0)
                {
                    Description = $"{Key} → {DeviceBindings.Count} devices (Hold Mode)";
                }
                else
                {
                    Description = $"{Key} → {DeviceBindings.Count} devices (Timed)";
                }
            }
        }

        public override string ToString()
        {
            return Description;
        }
    }
}