using System;

using System.Collections.ObjectModel;

using System.ComponentModel;

using System.Linq;

using System.Runtime.CompilerServices;

using System.Windows;

using System.Windows.Input;

using ToyControlApp.Models;

using ToyControlApp.Services;

using ToyControlApp.ViewModels;



namespace ToyControlApp

{

    public partial class ProfileDialog : Window, INotifyPropertyChanged

    {

        private readonly ProfileService _profileService;

        private readonly Action<Profile> _onProfileSelected;

        private readonly Func<Profile> _getCurrentProfileData;



        private Profile _selectedProfile;

        private string _newProfileName = "";

        private string _currentProfileName = "";



        public ObservableCollection<Profile> Profiles { get; }



        public Profile SelectedProfile

        {

            get => _selectedProfile;

            set

            {

                _selectedProfile = value;

                OnPropertyChanged();

                RaiseCommandsCanExecuteChanged();

            }

        }



        public string NewProfileName

        {

            get => _newProfileName;

            set

            {

                _newProfileName = value;

                OnPropertyChanged();

                ((RelayCommand)CreateProfileCommand).RaiseCanExecuteChanged();

            }

        }



        public string CurrentProfileName

        {

            get => _currentProfileName;

            set

            {

                _currentProfileName = value;

                OnPropertyChanged();

            }

        }



        // Commands

        public ICommand CreateProfileCommand { get; }

        public ICommand LoadProfileCommand { get; }

        public ICommand SaveCurrentProfileCommand { get; }

        public ICommand RenameProfileCommand { get; }

        public ICommand DeleteProfileCommand { get; }



        public ProfileDialog(ProfileService profileService, Action<Profile> onProfileSelected,

                           Func<Profile> getCurrentProfileData, string currentProfileName)

        {

            InitializeComponent();

            _profileService = profileService;

            _onProfileSelected = onProfileSelected;

            _getCurrentProfileData = getCurrentProfileData;

            CurrentProfileName = currentProfileName;



            Profiles = new ObservableCollection<Profile>();

            DataContext = this;



            // Initialize commands

            CreateProfileCommand = new RelayCommand(CreateProfile, () => !string.IsNullOrWhiteSpace(NewProfileName));

            LoadProfileCommand = new RelayCommand(LoadProfile, () => SelectedProfile != null);

            SaveCurrentProfileCommand = new RelayCommand(SaveCurrentProfile);

            RenameProfileCommand = new RelayCommand(RenameProfile, () => SelectedProfile != null);

            DeleteProfileCommand = new RelayCommand(DeleteProfile, () => SelectedProfile != null);



            LoadProfiles();

        }



        private void LoadProfiles()

        {

            try

            {

                var profiles = _profileService.LoadAllProfiles();

                Profiles.Clear();

                foreach (var profile in profiles)

                {

                    Profiles.Add(profile);

                }



                // Select current profile if it exists

                SelectedProfile = Profiles.FirstOrDefault(p => p.Name == CurrentProfileName) ?? Profiles.FirstOrDefault();

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Error loading profiles: {ex.Message}", "Error",

                    MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }



        private void CreateProfile()

        {

            try

            {

                var profile = _profileService.CreateProfile(NewProfileName);

                Profiles.Add(profile);



                // Sort profiles by name

                var sortedProfiles = Profiles.OrderBy(p => p.Name).ToList();

                Profiles.Clear();

                foreach (var p in sortedProfiles)

                {

                    Profiles.Add(p);

                }



                SelectedProfile = profile;

                NewProfileName = "";

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Error creating profile: {ex.Message}", "Error",

                    MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }



        private void LoadProfile()

        {

            if (SelectedProfile == null) return;



            try

            {

                _onProfileSelected(SelectedProfile);

                CurrentProfileName = SelectedProfile.Name;

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Error loading profile: {ex.Message}", "Error",

                    MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }



        private void SaveCurrentProfile()

        {

            try

            {

                var currentData = _getCurrentProfileData();

                if (currentData != null)

                {

                    _profileService.SaveProfile(currentData);



                    // Update the profile in the list

                    var existingProfile = Profiles.FirstOrDefault(p => p.Name == currentData.Name);

                    if (existingProfile != null)

                    {

                        var index = Profiles.IndexOf(existingProfile);

                        Profiles[index] = currentData;

                        SelectedProfile = currentData;

                    }



                    MessageBox.Show($"Profile '{currentData.Name}' saved successfully!", "Profile Saved",

                        MessageBoxButton.OK, MessageBoxImage.Information);

                }

            }

            catch (Exception ex)

            {

                MessageBox.Show($"Error saving profile: {ex.Message}", "Error",

                    MessageBoxButton.OK, MessageBoxImage.Error);

            }

        }



        private void RenameProfile()

        {

            if (SelectedProfile == null) return;



            var dialog = new InputDialog("Rename Profile", "Enter new profile name:", SelectedProfile.Name);

            if (dialog.ShowDialog() == true)

            {

                try

                {

                    var oldName = SelectedProfile.Name;

                    _profileService.RenameProfile(SelectedProfile, dialog.InputText);



                    if (CurrentProfileName == oldName)

                    {

                        CurrentProfileName = dialog.InputText;

                    }



                    LoadProfiles(); // Refresh the list

                }

                catch (Exception ex)

                {

                    MessageBox.Show($"Error renaming profile: {ex.Message}", "Error",

                        MessageBoxButton.OK, MessageBoxImage.Error);

                }

            }

        }



        private void DeleteProfile()

        {

            if (SelectedProfile == null) return;



            var result = MessageBox.Show($"Are you sure you want to delete profile '{SelectedProfile.Name}'?",

                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);



            if (result == MessageBoxResult.Yes)

            {

                try

                {

                    var profileToDelete = SelectedProfile;

                    _profileService.DeleteProfile(profileToDelete);

                    Profiles.Remove(profileToDelete);



                    // If we deleted the current profile, clear it and switch to no profile

                    if (CurrentProfileName == profileToDelete.Name)

                    {

                        // Clear current profile - user will have no profile loaded

                        _onProfileSelected(null);

                        CurrentProfileName = "No Profile";

                    }



                    // Select the first remaining profile, or null if no profiles left

                    SelectedProfile = Profiles.FirstOrDefault();

                }

                catch (Exception ex)

                {

                    MessageBox.Show($"Error deleting profile: {ex.Message}", "Error",

                        MessageBoxButton.OK, MessageBoxImage.Error);

                }

            }

        }



        private void RaiseCommandsCanExecuteChanged()

        {

            ((RelayCommand)LoadProfileCommand).RaiseCanExecuteChanged();

            ((RelayCommand)RenameProfileCommand).RaiseCanExecuteChanged();

            ((RelayCommand)DeleteProfileCommand).RaiseCanExecuteChanged();

        }



        private void NewProfileNameTextBox_KeyDown(object sender, KeyEventArgs e)

        {

            if (e.Key == Key.Enter && CreateProfileCommand.CanExecute(null))

            {

                CreateProfileCommand.Execute(null);

                e.Handled = true;

            }

        }



        public event PropertyChangedEventHandler PropertyChanged;



        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)

        {

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        }

    }

}