using Buttplug.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using ToyControlApp.Models;

namespace ToyControlApp
{
    public partial class MouseBindingDialog : Window, INotifyPropertyChanged
    {
        private readonly MouseInputBinding _binding;
        private readonly List<ButtplugClientDevice> _availableDevices;
        private MouseInput _selectedMouseInput;

        public ObservableCollection<DeviceBinding> DeviceBindings { get; }

        public MouseBindingDialog(MouseInputBinding binding, List<ButtplugClientDevice> devices)
        {
            InitializeComponent();
            _binding = binding;
            _availableDevices = devices;

            // Initialize device bindings collection
            DeviceBindings = new ObservableCollection<DeviceBinding>();

            // If editing existing binding, populate with existing device bindings
            if (_binding.DeviceBindings.Any())
            {
                foreach (var deviceBinding in _binding.DeviceBindings)
                {
                    DeviceBindings.Add(new DeviceBinding(deviceBinding.DeviceName, deviceBinding.Intensity, deviceBinding.DurationMs, deviceBinding.IsHoldMode));
                }

                // Set the selected mouse input if editing
                _selectedMouseInput = _binding.Input;
            }
            else
            {
                _selectedMouseInput = new MouseInput(MouseInputType.LeftClick); // Default selection
            }

            // Set up data context for binding
            DataContext = this;

            // Populate mouse input combo box
            var mouseInputItems = new List<MouseInputItem>();
            foreach (MouseInputType inputType in Enum.GetValues<MouseInputType>())
            {
                mouseInputItems.Add(new MouseInputItem
                {
                    InputType = inputType,
                    DisplayName = new MouseInput(inputType).ToString()
                });
            }
            MouseInputComboBox.ItemsSource = mouseInputItems;
            MouseInputComboBox.DisplayMemberPath = "DisplayName";
            MouseInputComboBox.SelectedValuePath = "InputType";

            // Set initial mouse input selection
            MouseInputComboBox.SelectedValue = _selectedMouseInput.InputType;

            // Update hold mode availability after setting the selection
            UpdateHoldModeAvailability();

            // Populate device combo box
            DeviceComboBox.ItemsSource = devices;
            if (devices.Count > 0)
            {
                DeviceComboBox.SelectedIndex = 0;
            }

            UpdateOkButtonState();
            UpdateDurationControlsVisibility(); // Set initial state
        }

        private void MouseInputComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MouseInputComboBox.SelectedValue != null)
            {
                var selectedInputType = (MouseInputType)MouseInputComboBox.SelectedValue;
                _selectedMouseInput = new MouseInput(selectedInputType);
                UpdateOkButtonState();
                UpdateHoldModeAvailability();
            }
        }

        // Update hold mode availability based on selected input
        private void UpdateHoldModeAvailability()
        {
            // Wheel events can't be held (they're instantaneous), so disable hold mode for them
            bool isWheelEvent = _selectedMouseInput.InputType == MouseInputType.WheelUp ||
                               _selectedMouseInput.InputType == MouseInputType.WheelDown;

            HoldModeCheckBox.IsEnabled = !isWheelEvent;

            if (isWheelEvent && HoldModeCheckBox.IsChecked == true)
            {
                HoldModeCheckBox.IsChecked = false;
                UpdateDurationControlsVisibility();
            }

            // Update tooltip to explain why hold mode is disabled for wheel events
            if (isWheelEvent)
            {
                HoldModeCheckBox.ToolTip = "Hold mode is not available for mouse wheel events";
            }
            else
            {
                HoldModeCheckBox.ToolTip = null;
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

        private void AddDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (DeviceComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a device.", "No Device Selected",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isHoldMode = HoldModeCheckBox.IsChecked == true;
            int duration = 1000; // Default duration

            // Only validate duration if not in hold mode
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

                if (duration > 60000) // 1 minute max
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

            // Check if device is already added
            if (DeviceBindings.Any(db => db.DeviceName == selectedDevice.Name))
            {
                MessageBox.Show($"Device '{selectedDevice.Name}' is already added to this mouse binding.",
                               "Device Already Added", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Create new device binding with hold mode support
            var deviceBinding = new DeviceBinding(
                selectedDevice.Name,
                IntensitySlider.Value / 100.0,
                duration,
                isHoldMode // Pass hold mode flag
            );

            DeviceBindings.Add(deviceBinding);
            UpdateOkButtonState();

            // Reset the form
            DeviceComboBox.SelectedIndex = 0;
            IntensitySlider.Value = 50;
            HoldModeCheckBox.IsChecked = false; // Reset hold mode
            DurationTextBox.Text = "1000";
            UpdateDurationControlsVisibility(); // Update visibility after reset
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
            // Enable OK button if we have a mouse input and at least one device binding
            OkButton.IsEnabled = MouseInputComboBox.SelectedItem != null &&
                                DeviceBindings.Count > 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            if (MouseInputComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a mouse input.", "Invalid Input",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                MouseInputComboBox.Focus();
                return;
            }

            if (DeviceBindings.Count == 0)
            {
                MessageBox.Show("Please add at least one device binding.", "No Device Bindings",
                               MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Update the binding
            _binding.Input = _selectedMouseInput;
            _binding.DeviceBindings.Clear();

            foreach (var deviceBinding in DeviceBindings)
            {
                _binding.DeviceBindings.Add(new DeviceBinding(
                    deviceBinding.DeviceName,
                    deviceBinding.Intensity,
                    deviceBinding.DurationMs,
                    deviceBinding.IsHoldMode // Include hold mode
                ));
            }

            _binding.UpdateDescription();

            DialogResult = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private class MouseInputItem
        {
            public MouseInputType InputType { get; set; }
            public string DisplayName { get; set; }
        }
    }
}