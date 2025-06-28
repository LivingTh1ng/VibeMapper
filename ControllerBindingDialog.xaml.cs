using Buttplug.Client;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using ToyControlApp.Models;

namespace ToyControlApp
{
    public partial class ControllerBindingDialog : Window, INotifyPropertyChanged
    {
        private readonly ControllerBinding _binding;
        private readonly List<ButtplugClientDevice> _availableDevices;
        private readonly List<Controller> _availableControllers;
        private ControllerInput _selectedControllerInput = new ControllerInput(0, ControllerInputType.ButtonA);
        private bool _capturingInput = false;
        private System.Threading.Timer _inputCaptureTimer;

        public ObservableCollection<DeviceBinding> DeviceBindings { get; }

        public ControllerBindingDialog(ControllerBinding binding, List<ButtplugClientDevice> devices, List<Controller> controllers)
        {
            InitializeComponent();
            _binding = binding;
            _availableDevices = devices;
            _availableControllers = controllers;

            // Initialize device bindings collection
            DeviceBindings = new ObservableCollection<DeviceBinding>();

            // If editing existing binding, populate with existing device bindings
            if (_binding.DeviceBindings.Any())
            {
                foreach (var deviceBinding in _binding.DeviceBindings)
                {
                    DeviceBindings.Add(new DeviceBinding(deviceBinding.DeviceName, deviceBinding.Intensity, deviceBinding.DurationMs, deviceBinding.IsHoldMode));
                }

                // Set the selected controller input if editing
                _selectedControllerInput = _binding.Input;
                UpdateInputDisplay();
            }
            else
            {
                _selectedControllerInput = new ControllerInput(-1, default(ControllerInputType)); // Use -1 to indicate no selection
            }

            // Set up data context for binding
            DataContext = this;

            // Populate controller combo box
            var controllerItems = new List<ControllerItem>();
            for (int i = 0; i < controllers.Count; i++)
            {
                if (controllers[i].IsConnected)
                {
                    controllerItems.Add(new ControllerItem { Index = i, Name = $"Controller {i + 1}" });
                }
            }
            ControllerComboBox.ItemsSource = controllerItems;
            ControllerComboBox.DisplayMemberPath = "Name";
            ControllerComboBox.SelectedValuePath = "Index";

            // Set initial controller selection if editing
            if (_binding.DeviceBindings.Any())
            {
                ControllerComboBox.SelectedValue = _binding.Input.ControllerIndex;
            }
            else if (controllerItems.Count > 0)
            {
                ControllerComboBox.SelectedIndex = 0;
            }

            // Set up input capture
            InputTextBox.GotFocus += InputTextBox_GotFocus;
            InputTextBox.LostFocus += InputTextBox_LostFocus;

            // Populate device combo box
            DeviceComboBox.ItemsSource = devices;
            if (devices.Count > 0)
            {
                DeviceComboBox.SelectedIndex = 0;
            }

            UpdateOkButtonState();
            UpdateDurationControlsVisibility(); // Set initial state

            // Focus the input textbox and start capture immediately
            InputTextBox.Focus();

            // Start input capture immediately if we have a controller selected
            if (ControllerComboBox.SelectedItem != null)
            {
                StartInputCapture();
            }
        }

        private void ControllerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_capturingInput && ControllerComboBox.SelectedItem != null)
            {
                StartInputCapture();
            }
        }

        // Handle hold mode checkbox changes
        private void HoldModeCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            UpdateDurationControlsVisibility();
        }

        // Update visibility of duration controls based on hold mode
        private void UpdateDurationControlsVisibility()
        {
            bool isHoldMode = HoldModeCheckBox.IsChecked == true;

            // Grey out and disable duration controls when hold mode is enabled
            DurationLabel.IsEnabled = !isHoldMode;
            DurationTextBox.IsEnabled = !isHoldMode;

            // Use theme resources instead of hardcoded colors
            DurationTextBox.Background = isHoldMode ?
                (System.Windows.Media.SolidColorBrush)FindResource("DarkDisabledBrush") :
                (System.Windows.Media.SolidColorBrush)FindResource("DarkSurfaceBrush");

            // Set helpful text when disabled
            if (isHoldMode && DurationTextBox.Text == "1000")
            {
                DurationTextBox.Text = "N/A (Hold Mode)";
            }
            else if (!isHoldMode && DurationTextBox.Text == "N/A (Hold Mode)")
            {
                DurationTextBox.Text = "1000";
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            // Stop any active input capture
            StopInputCapture();

            // Reset the selected input
            _selectedControllerInput = new ControllerInput(-1, default(ControllerInputType));

            // Immediately restart capture instead of showing inactive state
            if (ControllerComboBox.SelectedItem != null)
            {
                StartInputCapture();
            }
            else
            {
                InputTextBox.Text = "Select a controller first";
                InputTextBox.Background = (System.Windows.Media.SolidColorBrush)FindResource("DarkSurfaceBrush");
            }

            // Update button state
            UpdateOkButtonState();
        }

        private void InputTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!_capturingInput && ControllerComboBox.SelectedItem != null)
            {
                StartInputCapture();
            }
        }

        private void InputTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            StopInputCapture();
        }

        private void StartInputCapture()
        {
            if (_capturingInput || ControllerComboBox.SelectedItem == null) return;

            _capturingInput = true;
            InputTextBox.Text = "Waiting for controller input...";
            // Use normal color instead of green
            InputTextBox.Background = (System.Windows.Media.SolidColorBrush)FindResource("DarkSurfaceBrush");

            // Start polling the selected controller for input
            var selectedControllerIndex = (int)ControllerComboBox.SelectedValue;
            _inputCaptureTimer = new System.Threading.Timer(CaptureControllerInput, selectedControllerIndex, 100, 100);
        }

        private void StopInputCapture()
        {
            if (!_capturingInput) return;

            _capturingInput = false;
            _inputCaptureTimer?.Dispose();
            _inputCaptureTimer = null;
            InputTextBox.Background = (System.Windows.Media.SolidColorBrush)FindResource("DarkSurfaceBrush");
        }

        private void CaptureControllerInput(object state)
        {
            if (!_capturingInput) return;

            var controllerIndex = (int)state;

            try
            {
                if (controllerIndex < 0 || controllerIndex >= _availableControllers.Count)
                    return;

                var controller = _availableControllers[controllerIndex];
                if (!controller.IsConnected)
                    return;

                var currentState = controller.GetState();
                var gamepad = currentState.Gamepad;

                // Check for button presses
                var buttonInput = CheckForButtonPress(controllerIndex, gamepad);
                if (buttonInput.HasValue)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _selectedControllerInput = buttonInput.Value;
                        UpdateInputDisplay();
                        StopInputCapture();
                        UpdateOkButtonState();
                    });
                    return;
                }

                // Check for trigger presses
                var triggerInput = CheckForTriggerPress(controllerIndex, gamepad);
                if (triggerInput.HasValue)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _selectedControllerInput = triggerInput.Value;
                        UpdateInputDisplay();
                        StopInputCapture();
                        UpdateOkButtonState();
                    });
                    return;
                }

                // Check for thumbstick movement
                var stickInput = CheckForStickMovement(controllerIndex, gamepad);
                if (stickInput.HasValue)
                {
                    Dispatcher.Invoke(() =>
                    {
                        _selectedControllerInput = stickInput.Value;
                        UpdateInputDisplay();
                        StopInputCapture();
                        UpdateOkButtonState();
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StopInputCapture();
                    MessageBox.Show($"Error reading controller: {ex.Message}", "Controller Error",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                });
            }
        }

        private ControllerInput? CheckForButtonPress(int controllerIndex, SharpDX.XInput.Gamepad gamepad)
        {
            var buttonMappings = new Dictionary<SharpDX.XInput.GamepadButtonFlags, ControllerInputType>
            {
                { SharpDX.XInput.GamepadButtonFlags.A, ControllerInputType.ButtonA },
                { SharpDX.XInput.GamepadButtonFlags.B, ControllerInputType.ButtonB },
                { SharpDX.XInput.GamepadButtonFlags.X, ControllerInputType.ButtonX },
                { SharpDX.XInput.GamepadButtonFlags.Y, ControllerInputType.ButtonY },
                { SharpDX.XInput.GamepadButtonFlags.LeftShoulder, ControllerInputType.LeftBumper },
                { SharpDX.XInput.GamepadButtonFlags.RightShoulder, ControllerInputType.RightBumper },
                { SharpDX.XInput.GamepadButtonFlags.Back, ControllerInputType.Back },
                { SharpDX.XInput.GamepadButtonFlags.Start, ControllerInputType.Start },
                { SharpDX.XInput.GamepadButtonFlags.LeftThumb, ControllerInputType.LeftStickClick },
                { SharpDX.XInput.GamepadButtonFlags.RightThumb, ControllerInputType.RightStickClick },
                { SharpDX.XInput.GamepadButtonFlags.DPadUp, ControllerInputType.DPadUp },
                { SharpDX.XInput.GamepadButtonFlags.DPadDown, ControllerInputType.DPadDown },
                { SharpDX.XInput.GamepadButtonFlags.DPadLeft, ControllerInputType.DPadLeft },
                { SharpDX.XInput.GamepadButtonFlags.DPadRight, ControllerInputType.DPadRight }
            };

            foreach (var mapping in buttonMappings)
            {
                bool isPressed = (gamepad.Buttons & mapping.Key) != 0;
                if (isPressed)
                {
                    return new ControllerInput(controllerIndex, mapping.Value);
                }
            }

            return null;
        }

        private ControllerInput? CheckForTriggerPress(int controllerIndex, SharpDX.XInput.Gamepad gamepad)
        {
            const byte triggerThreshold = 64;

            if (gamepad.LeftTrigger > triggerThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.LeftTrigger);

            if (gamepad.RightTrigger > triggerThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.RightTrigger);

            return null;
        }

        private ControllerInput? CheckForStickMovement(int controllerIndex, SharpDX.XInput.Gamepad gamepad)
        {
            const short stickThreshold = 8192;

            // Left stick
            if (gamepad.LeftThumbY > stickThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.LeftStickUp);
            if (gamepad.LeftThumbY < -stickThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.LeftStickDown);
            if (gamepad.LeftThumbX < -stickThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.LeftStickLeft);
            if (gamepad.LeftThumbX > stickThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.LeftStickRight);

            // Right stick
            if (gamepad.RightThumbY > stickThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.RightStickUp);
            if (gamepad.RightThumbY < -stickThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.RightStickDown);
            if (gamepad.RightThumbX < -stickThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.RightStickLeft);
            if (gamepad.RightThumbX > stickThreshold)
                return new ControllerInput(controllerIndex, ControllerInputType.RightStickRight);

            return null;
        }

        private void UpdateInputDisplay()
        {
            if (_selectedControllerInput.ControllerIndex >= 0)
            {
                InputTextBox.Text = _selectedControllerInput.ToString();
                InputTextBox.Background = (System.Windows.Media.SolidColorBrush)FindResource("DarkSurfaceBrush");
            }
        }

        private void AddDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a device.", "No Device Selected",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isHoldMode = HoldModeCheckBox.IsChecked == true;
            int duration = 1000;

            if (!isHoldMode)
            {
                if (!int.TryParse(DurationTextBox.Text, out duration) || duration < 0)
                {
                    MessageBox.Show("Please enter a valid duration in milliseconds (0 or greater).", "Invalid Duration",
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    DurationTextBox.Focus();
                    DurationTextBox.SelectAll();
                    return;
                }

                if (duration > 60000)
                {
                    var result = MessageBox.Show("Duration is longer than 1 minute. Are you sure?", "Long Duration",
                                               MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result != MessageBoxResult.Yes)
                    {
                        DurationTextBox.Focus();
                        DurationTextBox.SelectAll();
                        return;
                    }
                }
            }

            var selectedDevice = (ButtplugClientDevice)DeviceComboBox.SelectedItem;

            if (DeviceBindings.Any(db => db.DeviceName == selectedDevice.Name))
            {
                MessageBox.Show($"Device '{selectedDevice.Name}' is already added to this controller binding.",
                               "Device Already Added", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var deviceBinding = new DeviceBinding(
                selectedDevice.Name,
                IntensitySlider.Value / 100.0,
                duration,
                isHoldMode
            );

            DeviceBindings.Add(deviceBinding);
            UpdateOkButtonState();

            // Reset the form
            DeviceComboBox.SelectedIndex = 0;
            IntensitySlider.Value = 50;
            HoldModeCheckBox.IsChecked = false;
            DurationTextBox.Text = "1000";
            UpdateDurationControlsVisibility();
        }

        private void RemoveDeviceBinding_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is DeviceBinding binding)
            {
                DeviceBindings.Remove(binding);
                UpdateOkButtonState();
            }
        }

        private void UpdateOkButtonState()
        {
            var hasController = ControllerComboBox.SelectedItem != null;
            var hasInput = _selectedControllerInput.ControllerIndex >= 0;
            var hasDevices = DeviceBindings.Count > 0;

            OkButton.IsEnabled = hasController && hasInput && hasDevices;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            StopInputCapture();

            if (ControllerComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a controller.", "Invalid Input",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                ControllerComboBox.Focus();
                return;
            }

            if (_selectedControllerInput.ControllerIndex < 0)
            {
                MessageBox.Show("Please press a controller button to select an input.", "Invalid Input",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                InputTextBox.Focus();
                return;
            }

            if (DeviceBindings.Count == 0)
            {
                MessageBox.Show("Please add at least one device binding.", "No Device Bindings",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update the binding
            _binding.Input = _selectedControllerInput;
            _binding.DeviceBindings.Clear();

            foreach (var deviceBinding in DeviceBindings)
            {
                _binding.DeviceBindings.Add(new DeviceBinding(
                    deviceBinding.DeviceName,
                    deviceBinding.Intensity,
                    deviceBinding.DurationMs,
                    deviceBinding.IsHoldMode
                ));
            }

            _binding.UpdateDescription();

            DialogResult = true;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopInputCapture();
            base.OnClosed(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class ControllerItem
        {
            public int Index { get; set; }
            public string Name { get; set; }
        }
    }
}