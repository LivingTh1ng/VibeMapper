using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ToyControlApp
{
    public partial class InputDialog : Window, INotifyPropertyChanged
    {
        private string _title;
        private string _prompt;
        private string _inputText;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        public string Prompt
        {
            get => _prompt;
            set
            {
                _prompt = value;
                OnPropertyChanged();
            }
        }

        public string InputText
        {
            get => _inputText;
            set
            {
                _inputText = value;
                OnPropertyChanged();
            }
        }

        public InputDialog(string title, string prompt, string defaultText = "")
        {
            InitializeComponent();
            DataContext = this;

            Title = title;
            Prompt = prompt;
            InputText = defaultText;

            // Focus and select text in the input box when dialog loads
            Loaded += (s, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(InputText))
            {
                MessageBox.Show("Please enter a value.", "Invalid Input",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                InputTextBox.Focus();
                return;
            }

            DialogResult = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}