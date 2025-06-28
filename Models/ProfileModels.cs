using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ToyControlApp.Models;

namespace ToyControlApp.Models
{
    public class Profile : INotifyPropertyChanged
    {
        private string _name;
        private DateTime _lastUsed;
        private DateTime _created;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public DateTime LastUsed
        {
            get => _lastUsed;
            set
            {
                _lastUsed = value;
                OnPropertyChanged();
            }
        }

        public DateTime Created
        {
            get => _created;
            set
            {
                _created = value;
                OnPropertyChanged();
            }
        }

        public List<KeyBindingData> KeyBindings { get; set; } = new List<KeyBindingData>();
        public List<ControllerBindingData> ControllerBindings { get; set; } = new List<ControllerBindingData>();
        public List<MouseBindingData> MouseBindings { get; set; } = new List<MouseBindingData>();

        public Profile()
        {
            Name = "New Profile";
            Created = DateTime.Now;
            LastUsed = DateTime.Now;
        }

        public Profile(string name) : this()
        {
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Serializable versions of the binding classes for JSON storage
    public class KeyBindingData
    {
        public string Key { get; set; } // Store as string for JSON compatibility
        public List<DeviceBindingData> DeviceBindings { get; set; } = new List<DeviceBindingData>();
        public string Description { get; set; }

        public KeyBindingData() { }

        public KeyBindingData(KeyBinding binding)
        {
            Key = binding.Key.ToString();
            Description = binding.Description;
            foreach (var deviceBinding in binding.DeviceBindings)
            {
                DeviceBindings.Add(new DeviceBindingData(deviceBinding));
            }
        }

        public KeyBinding ToKeyBinding()
        {
            var binding = new KeyBinding();
            if (Enum.TryParse<System.Windows.Forms.Keys>(Key, out var parsedKey))
            {
                binding.Key = parsedKey;
            }
            binding.Description = Description;
            foreach (var deviceBindingData in DeviceBindings)
            {
                binding.DeviceBindings.Add(deviceBindingData.ToDeviceBinding());
            }
            binding.UpdateDescription();
            return binding;
        }
    }

    public class ControllerBindingData
    {
        public int ControllerIndex { get; set; }
        public string InputType { get; set; } // Store as string for JSON compatibility
        public List<DeviceBindingData> DeviceBindings { get; set; } = new List<DeviceBindingData>();
        public string Description { get; set; }

        public ControllerBindingData() { }

        public ControllerBindingData(ControllerBinding binding)
        {
            ControllerIndex = binding.Input.ControllerIndex;
            InputType = binding.Input.InputType.ToString();
            Description = binding.Description;
            foreach (var deviceBinding in binding.DeviceBindings)
            {
                DeviceBindings.Add(new DeviceBindingData(deviceBinding));
            }
        }

        public ControllerBinding ToControllerBinding()
        {
            var binding = new ControllerBinding();
            if (Enum.TryParse<ControllerInputType>(InputType, out var parsedInputType))
            {
                binding.Input = new ControllerInput(ControllerIndex, parsedInputType);
            }
            binding.Description = Description;
            foreach (var deviceBindingData in DeviceBindings)
            {
                binding.DeviceBindings.Add(deviceBindingData.ToDeviceBinding());
            }
            binding.UpdateDescription();
            return binding;
        }
    }

    public class MouseBindingData
    {
        public string InputType { get; set; } // Store as string for JSON compatibility
        public List<DeviceBindingData> DeviceBindings { get; set; } = new List<DeviceBindingData>();
        public string Description { get; set; }

        public MouseBindingData() { }

        // UPDATED: Changed parameter type to MouseInputBinding
        public MouseBindingData(MouseInputBinding binding)
        {
            InputType = binding.Input.InputType.ToString();
            Description = binding.Description;
            foreach (var deviceBinding in binding.DeviceBindings)
            {
                DeviceBindings.Add(new DeviceBindingData(deviceBinding));
            }
        }

        // UPDATED: Changed return type to MouseInputBinding
        public MouseInputBinding ToMouseBinding()
        {
            var binding = new MouseInputBinding();
            if (Enum.TryParse<MouseInputType>(InputType, out var parsedInputType))
            {
                binding.Input = new MouseInput(parsedInputType);
            }
            binding.Description = Description;
            foreach (var deviceBindingData in DeviceBindings)
            {
                binding.DeviceBindings.Add(deviceBindingData.ToDeviceBinding());
            }
            binding.UpdateDescription();
            return binding;
        }
    }

    public class DeviceBindingData
    {
        public string DeviceName { get; set; }
        public double Intensity { get; set; }
        public int DurationMs { get; set; }
        public bool IsHoldMode { get; set; } // NEW: Hold mode support

        public DeviceBindingData() { }

        public DeviceBindingData(DeviceBinding binding)
        {
            DeviceName = binding.DeviceName;
            Intensity = binding.Intensity;
            DurationMs = binding.DurationMs;
            IsHoldMode = binding.IsHoldMode;
        }

        public DeviceBinding ToDeviceBinding()
        {
            return new DeviceBinding(DeviceName, Intensity, DurationMs, IsHoldMode);
        }
    }
}