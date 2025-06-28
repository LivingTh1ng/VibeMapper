using System;
using System.Collections.Generic;
using System.Linq;

namespace ToyControlApp.Models
{
    public enum ControllerInputType
    {
        ButtonA,
        ButtonB,
        ButtonX,
        ButtonY,
        LeftBumper,
        RightBumper,
        LeftTrigger,
        RightTrigger,
        Back,
        Start,
        LeftStickClick,
        RightStickClick,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight,
        LeftStickUp,
        LeftStickDown,
        LeftStickLeft,
        LeftStickRight,
        RightStickUp,
        RightStickDown,
        RightStickLeft,
        RightStickRight
    }

    public struct ControllerInput : IEquatable<ControllerInput>
    {
        public int ControllerIndex { get; }
        public ControllerInputType InputType { get; }

        public ControllerInput(int controllerIndex, ControllerInputType inputType)
        {
            ControllerIndex = controllerIndex;
            InputType = inputType;
        }

        public override string ToString()
        {
            string controllerName = $"Controller {ControllerIndex + 1}";
            string inputName = InputType switch
            {
                ControllerInputType.ButtonA => "A Button",
                ControllerInputType.ButtonB => "B Button",
                ControllerInputType.ButtonX => "X Button",
                ControllerInputType.ButtonY => "Y Button",
                ControllerInputType.LeftBumper => "Left Bumper",
                ControllerInputType.RightBumper => "Right Bumper",
                ControllerInputType.LeftTrigger => "Left Trigger",
                ControllerInputType.RightTrigger => "Right Trigger",
                ControllerInputType.Back => "Back Button",
                ControllerInputType.Start => "Start Button",
                ControllerInputType.LeftStickClick => "Left Stick Click",
                ControllerInputType.RightStickClick => "Right Stick Click",
                ControllerInputType.DPadUp => "D-Pad Up",
                ControllerInputType.DPadDown => "D-Pad Down",
                ControllerInputType.DPadLeft => "D-Pad Left",
                ControllerInputType.DPadRight => "D-Pad Right",
                ControllerInputType.LeftStickUp => "Left Stick Up",
                ControllerInputType.LeftStickDown => "Left Stick Down",
                ControllerInputType.LeftStickLeft => "Left Stick Left",
                ControllerInputType.LeftStickRight => "Left Stick Right",
                ControllerInputType.RightStickUp => "Right Stick Up",
                ControllerInputType.RightStickDown => "Right Stick Down",
                ControllerInputType.RightStickLeft => "Right Stick Left",
                ControllerInputType.RightStickRight => "Right Stick Right",
                _ => InputType.ToString()
            };

            return $"{controllerName} - {inputName}";
        }

        public bool Equals(ControllerInput other)
        {
            return ControllerIndex == other.ControllerIndex && InputType == other.InputType;
        }

        public override bool Equals(object obj)
        {
            return obj is ControllerInput other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ControllerIndex, InputType);
        }

        public static bool operator ==(ControllerInput left, ControllerInput right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ControllerInput left, ControllerInput right)
        {
            return !left.Equals(right);
        }
    }

    public class ControllerBinding
    {
        public ControllerInput Input { get; set; }
        public List<DeviceBinding> DeviceBindings { get; set; }
        public string Description { get; set; }

        public ControllerBinding()
        {
            Input = new ControllerInput(0, ControllerInputType.ButtonA);
            DeviceBindings = new List<DeviceBinding>();
            Description = "";
        }

        public ControllerBinding(ControllerInput input)
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