using System;

using System.Collections.Generic;

using System.IO;

using System.Linq;

using System.Text.Json;

using ToyControlApp.Models;



namespace ToyControlApp.Services

{

    public class ProfileService

    {

        private readonly string _profilesDirectory;

        private readonly string _settingsFile;



        public ProfileService()

        {

            // Store profiles in AppData/Roaming/ToyControlApp

            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            _profilesDirectory = Path.Combine(appDataPath, "ToyControlApp", "Profiles");

            _settingsFile = Path.Combine(appDataPath, "ToyControlApp", "settings.json");



            // Ensure directories exist

            Directory.CreateDirectory(_profilesDirectory);

            Directory.CreateDirectory(Path.GetDirectoryName(_settingsFile));

        }



        public List<Profile> LoadAllProfiles()

        {

            var profiles = new List<Profile>();



            try

            {

                if (!Directory.Exists(_profilesDirectory))

                    return profiles;



                var profileFiles = Directory.GetFiles(_profilesDirectory, "*.json");



                foreach (var file in profileFiles)

                {

                    try

                    {

                        var json = File.ReadAllText(file);

                        var profile = JsonSerializer.Deserialize<Profile>(json);

                        if (profile != null)

                        {

                            profiles.Add(profile);

                        }

                    }

                    catch (Exception ex)

                    {

                        System.Diagnostics.Debug.WriteLine($"Error loading profile {file}: {ex.Message}");

                    }

                }

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"Error loading profiles: {ex.Message}");

            }



            return profiles.OrderBy(p => p.Name).ToList();

        }



        public void SaveProfile(Profile profile)

        {

            try

            {

                profile.LastUsed = DateTime.Now;

                var json = JsonSerializer.Serialize(profile, new JsonSerializerOptions

                {

                    WriteIndented = true

                });



                var fileName = GetSafeFileName(profile.Name) + ".json";

                var filePath = Path.Combine(_profilesDirectory, fileName);

                File.WriteAllText(filePath, json);

            }

            catch (Exception ex)

            {

                throw new InvalidOperationException($"Failed to save profile '{profile.Name}': {ex.Message}", ex);

            }

        }



        public void DeleteProfile(Profile profile)

        {

            try

            {

                var fileName = GetSafeFileName(profile.Name) + ".json";

                var filePath = Path.Combine(_profilesDirectory, fileName);



                if (File.Exists(filePath))

                {

                    File.Delete(filePath);

                }

            }

            catch (Exception ex)

            {

                throw new InvalidOperationException($"Failed to delete profile '{profile.Name}': {ex.Message}", ex);

            }

        }



        public Profile CreateProfile(string name)

        {

            if (string.IsNullOrWhiteSpace(name))

                throw new ArgumentException("Profile name cannot be empty");



            // Check if profile with this name already exists

            var existingProfiles = LoadAllProfiles();

            if (existingProfiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))

                throw new InvalidOperationException($"Profile '{name}' already exists");



            var profile = new Profile(name);

            SaveProfile(profile);

            return profile;

        }



        public void RenameProfile(Profile profile, string newName)

        {

            if (string.IsNullOrWhiteSpace(newName))

                throw new ArgumentException("Profile name cannot be empty");



            // Check if profile with new name already exists

            var existingProfiles = LoadAllProfiles();

            if (existingProfiles.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && p != profile))

                throw new InvalidOperationException($"Profile '{newName}' already exists");



            // Delete old file

            DeleteProfile(profile);



            // Update name and save with new name

            profile.Name = newName;

            SaveProfile(profile);

        }



        public string GetLastUsedProfileName()

        {

            try

            {

                if (File.Exists(_settingsFile))

                {

                    var json = File.ReadAllText(_settingsFile);

                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (settings != null && settings.TryGetValue("lastUsedProfile", out var profileName))

                    {

                        return profileName.ToString();

                    }

                }

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"Error loading last used profile: {ex.Message}");

            }



            return null; // Return null instead of "Default"

        }



        public void SetLastUsedProfileName(string profileName)

        {

            try

            {

                var settings = new Dictionary<string, object>

                {

                    ["lastUsedProfile"] = profileName

                };



                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions

                {

                    WriteIndented = true

                });



                File.WriteAllText(_settingsFile, json);

            }

            catch (Exception ex)

            {

                System.Diagnostics.Debug.WriteLine($"Error saving last used profile: {ex.Message}");

            }

        }



        private string GetSafeFileName(string fileName)

        {

            // Remove invalid file name characters

            var invalidChars = Path.GetInvalidFileNameChars();

            var safeName = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());



            // Ensure it's not empty and not too long

            if (string.IsNullOrWhiteSpace(safeName))

                safeName = "Profile";



            if (safeName.Length > 100)

                safeName = safeName.Substring(0, 100);



            return safeName;

        }

    }

}