using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyControlApp.Models
{
    public enum MouseInputType
    {
        LeftClick,
        RightClick,
        MiddleClick,
        WheelUp,
        WheelDown,
        XButton1,  // Side button 1
        XButton2   // Side button 2
    }

    public struct MouseInput : IEquatable<MouseInput>
    {
        public MouseInputType InputType { get; }

        public MouseInput(MouseInputType inputType)
        {
            InputType = inputType;
        }

        public override string ToString()
        {
            return InputType switch
            {
                MouseInputType.LeftClick => "Left Click",
                MouseInputType.RightClick => "Right Click",
                MouseInputType.MiddleClick => "Middle Click",
                MouseInputType.WheelUp => "Wheel Up",
                MouseInputType.WheelDown => "Wheel Down",
                MouseInputType.XButton1 => "Side Button 1",
                MouseInputType.XButton2 => "Side Button 2",
                _ => InputType.ToString()
            };
        }

        public bool Equals(MouseInput other)
        {
            return InputType == other.InputType;
        }

        public override bool Equals(object obj)
        {
            return obj is MouseInput other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int)InputType;
        }

        public static bool operator ==(MouseInput left, MouseInput right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MouseInput left, MouseInput right)
        {
            return !left.Equals(right);
        }
    }

    // RENAMED: MouseBinding -> MouseInputBinding
    public class MouseInputBinding
    {
        public MouseInput Input { get; set; }
        public List<DeviceBinding> DeviceBindings { get; set; }
        public string Description { get; set; }

        public MouseInputBinding()
        {
            Input = new MouseInput(MouseInputType.LeftClick);
            DeviceBindings = new List<DeviceBinding>();
            Description = "";
        }

        public MouseInputBinding(MouseInput input)
        {
            Input = input;
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
                Description = $"{Input} → No devices";
            }
            else if (DeviceBindings.Count == 1)
            {
                var binding = DeviceBindings[0];
                if (binding.IsHoldMode)
                {
                    Description = $"{Input} → {binding.DeviceName} ({binding.Intensity:P0}, Hold Mode)";
                }
                else
                {
                    Description = $"{Input} → {binding.DeviceName} ({binding.Intensity:P0}, {binding.DurationMs}ms)";
                }
            }
            else
            {
                var holdModeCount = DeviceBindings.Count(d => d.IsHoldMode);
                var timedCount = DeviceBindings.Count - holdModeCount;

                if (holdModeCount > 0 && timedCount > 0)
                {
                    Description = $"{Input} → {DeviceBindings.Count} devices (Mixed: {holdModeCount} hold, {timedCount} timed)";
                }
                else if (holdModeCount > 0)
                {
                    Description = $"{Input} → {DeviceBindings.Count} devices (Hold Mode)";
                }
                else
                {
                    Description = $"{Input} → {DeviceBindings.Count} devices (Timed)";
                }
            }
        }

        public override string ToString()
        {
            return Description;
        }
    }
}