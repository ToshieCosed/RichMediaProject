using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace RichMediaJournal
{
    /// <summary>
    /// Interaction logic for ProfileSettings.xaml
    /// </summary>
    public partial class ProfileSettings : Window
    {
        private BitmapImage? displayimage;
        private BitmapImage? NewProfileImg;
        private string last_filepath;
        private MainWindow _mainWindow;

        public ProfileSettings(MainWindow _window)
        {
            InitializeComponent();

            _mainWindow = _window;

            LoadExistingProfiles();

            if (_window._defaultUserProfileImage != null)
            {
                displayimage = _window._defaultUserProfileImage;

                // Create a new Image control
                System.Windows.Controls.Image imageControl = new System.Windows.Controls.Image();

                // Set the Source property of the Image control to your BitmapImage
                imageControl.Source = displayimage;

                // Assuming ProfileImage is the name of your Image control in XAML
                this.ProfileImage.Source = displayimage; // Direct assignment if ProfileImage is a System.Windows.Controls.Image



            }


        }

        private void NewProfileUserTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void CreateProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(last_filepath)) // Check if the path is valid
            {
                try
                {
                    // 1. Create the Profiles directory if it doesn't exist
                    string profilesDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
                    if (!Directory.Exists(profilesDirectory))
                    {
                        Directory.CreateDirectory(profilesDirectory);
                    }

                    // 2. Create a copy of the image in the Profiles directory
                    string newFilePath = System.IO.Path.Combine(profilesDirectory, System.IO.Path.GetFileName(last_filepath));
                    File.Copy(last_filepath, newFilePath, true); // Overwrite if the file exists

                    // 3. Load the BitmapImage from the *new* file path
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(newFilePath); // Use the new file path!
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    // *** Key Change: Store the relative path ***
                    string relativePath = GetRelativePath(newFilePath); // Helper function (see below)


                    // 4. Create the UserProfile, using the *new* file path

                    //Edit
                    //UserProfile userProfile = new UserProfile(this.UserNameCreationLabel.Content.ToString(), newFilePath, false, bitmapImage); // Store New path

                    //Edit above into:
                    UserProfile userProfile = new UserProfile(this.UserNameCreationLabel.Content.ToString(), newFilePath, false, bitmapImage, Guid.NewGuid());



                    userProfile.ProfileImagePath = relativePath; // Store the new file path
                    userProfile.userProfileImage = bitmapImage;
                    _mainWindow._defaultUserProfileImage = bitmapImage;
                    _mainWindow._currentProfile = userProfile;

                    // ... (rest of your code remains the same)
                    _mainWindow.ValidProfiles.Add(userProfile);

                    //We're going to use current profile here just to test something
                    _mainWindow._currentProfile = userProfile;

                    string profiles_string = _mainWindow.Serialize_Profiles(_mainWindow.ValidProfiles);
                    File.WriteAllText("Profiles/profiles.txt", profiles_string);
                    Console.WriteLine("it works?");

                    string eep = profiles_string;
                    Console.WriteLine(eep);
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that might occur (e.g., invalid path, image format)
                    MessageBox.Show($"Error loading image: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("No image path available.");
            }

            LoadExistingProfiles();
        }

        private void NewProfileNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
                this.UserNameCreationLabel.Content = this.NewProfileNameTextBox.Text;
            }
        }

        /* (Last functioning piece
        private void LoadExistingProfiles()
        {
            ProfileDisplayScrollViewer.Content = null; // Clear any previous content
            //ProfileDisplayScrollViewer.Children.Clear(); // Clear any previous children

            WrapPanel profilesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            ProfileDisplayScrollViewer.Content = profilesPanel; // Set the WrapPanel as the content

            int profilesInRow = 0; // Keep track of profiles in the current row

            foreach (UserProfile profile in _mainWindow.ValidProfiles)
            {
                Grid profileContainer = new Grid { Margin = new Thickness(5) };
                profileContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                profileContainer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                ButtonBase profileImageButton = new Button { Width = 50, Height = 50, Margin = new Thickness(5) };
                Image profileImage = new Image { Source = profile.userProfileImage, Stretch = Stretch.UniformToFill };
                profileImageButton.Content = profileImage;
                profileImageButton.Click += (sender, e) => ProfileImage_Click(profile);

                Label usernameLabel = new Label
                {
                    Content = profile.Username,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0)
                };

                profileContainer.Children.Add(profileImageButton);
                profileContainer.Children.Add(usernameLabel);
                Grid.SetColumn(profileImageButton, 0);
                Grid.SetColumn(usernameLabel, 1);

                profilesPanel.Children.Add(profileContainer);

                profilesInRow++;
                if (profilesInRow == 6)
                {
                    profilesInRow = 0;
                }

                if (_mainWindow._currentProfile == null)
                {
                    _mainWindow._currentProfile = profile;
                    ProfileMenuCurrentUser.Content = profile.Username;
                    ProfileImage.Source = profile.userProfileImage;
                }
            }
        }
        */

        /// <summary>
        /// Experimental version
        /// </summary>
        /// <param name="profile"></param>
        /// REALLY OLD VERSION STILL DOESNT LOAD IMAGES BACK AFTER SAVING QUITTING RESTARTING
        /*
        



        private void LoadExistingProfiles()
        {
            ProfileDisplayScrollViewer.Content = null;

            WrapPanel profilesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            ProfileDisplayScrollViewer.Content = profilesPanel;

            foreach (UserProfile profile in _mainWindow.ValidProfiles)
            {
                Grid profileContainer = new Grid { Margin = new Thickness(5), Width = 100, Height = 120 };

                ButtonBase profileImageButton = new Button
                {
                    Width = 100,
                    Height = 100,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Content = new Image { Source = profile.userProfileImage, Stretch = Stretch.UniformToFill }
                };

                profileImageButton.Click += (sender, e) => ProfileImage_Click(profile);
                profileContainer.Children.Add(profileImageButton);

                Label usernameLabel = new Label
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    Padding = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 20
                };

                TextBlock textBlock = new TextBlock
                {
                    Text = profile.Username,
                    TextWrapping = TextWrapping.NoWrap,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };

                usernameLabel.Content = textBlock;
                profileContainer.Children.Add(usernameLabel);
                profilesPanel.Children.Add(profileContainer);


                if (_mainWindow._currentProfile == null)
                {
                    _mainWindow._currentProfile = profile;
                    ProfileMenuCurrentUser.Content = profile.Username;
                    ProfileImage.Source = profile.userProfileImage;
                }
            }
        }
        */

        /* NEWEST OLD CODE FEB 16 2025
        private void LoadExistingProfiles()
        {
            ProfileDisplayScrollViewer.Content = null;
            WrapPanel profilesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            ProfileDisplayScrollViewer.Content = profilesPanel;

            // Use a for loop with an index to modify the list
            for (int i = 0; i < _mainWindow.ValidProfiles.Count; i++)
            {
                UserProfile profile = _mainWindow.ValidProfiles[i]; // Get the profile by index
                Grid profileContainer = new Grid { Margin = new Thickness(5), Width = 100, Height = 120 };

                string fullImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, profile.ProfileImagePath);

                Image profileImage = new Image();
                try
                {
                    BitmapImage loadedImage = new BitmapImage(new Uri(fullImagePath));
                    loadedImage.CacheOption = BitmapCacheOption.OnLoad;
                    profileImage.Source = loadedImage;
                    profileImage.Stretch = Stretch.UniformToFill;

                    // *** Update the UserProfile using the index ***
                    profile.userProfileImage = loadedImage; // Assign the loaded image
                    _mainWindow.ValidProfiles[i] = profile; // Update the list with the modified profile

                }
                catch (Exception ex)
                {
                    // Handle image loading errors
                    MessageBox.Show($"Error loading image for {profile.Username}: {ex.Message}");
                    // Provide a default image or placeholder if needed
                    // profileImage.Source = _mainWindow._defaultUserProfileImage; // Or your default image
                    // profileImage.Stretch = Stretch.UniformToFill;

                    // Important: if you don't update the profile image on error, it will remain null.
                    // Consider using a default image here as well:
                    profile.userProfileImage = _mainWindow._defaultUserProfileImage; // Or your default image
                    _mainWindow.ValidProfiles[i] = profile; // Update the list with the modified profile

                }

                ButtonBase profileImageButton = new Button
                {
                    Width = 100,
                    Height = 100,
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Content = profileImage // Use the loaded/default image
                };

                profileImageButton.Click += (sender, e) => ProfileImage_Click(profile);
                profileContainer.Children.Add(profileImageButton);

                Label usernameLabel = new Label
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromArgb(128, 0, 0, 0)),
                    Padding = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 20
                };

                TextBlock textBlock = new TextBlock { Text = profile.Username, TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis };
                usernameLabel.Content = textBlock;
                profileContainer.Children.Add(usernameLabel);
                profilesPanel.Children.Add(profileContainer);

                if (_mainWindow._currentProfile == null)
                {
                    _mainWindow._currentProfile = profile;
                    _mainWindow._defaultUserProfileImage = profile.userProfileImage; // Use the updated image
                    ProfileMenuCurrentUser.Content = profile.Username;
                    ProfileImage.Source = profileImage.Source; // Use the image from the button
                }
            }
        }
        */

        private void LoadExistingProfiles()
        {
            ProfileDisplayScrollViewer.Content = null;
            WrapPanel profilesPanel = new WrapPanel { Orientation = Orientation.Horizontal };
            ProfileDisplayScrollViewer.Content = profilesPanel;

            for (int i = 0; i < _mainWindow.ValidProfiles.Count; i++)
            {
                UserProfile profile = _mainWindow.ValidProfiles[i];

                // Create a container Grid for the profile image and an overlaid title bar.
                Grid profileContainer = new Grid
                {
                    Width = 100,
                    Height = 120,
                    Margin = new Thickness(5)
                };

                // Add the profile image (fills the container)
                Image profileImage = new Image { Stretch = Stretch.UniformToFill };
                string fullImagePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, profile.ProfileImagePath);
                try
                {
                    BitmapImage loadedImage = new BitmapImage();
                    loadedImage.BeginInit();
                    loadedImage.UriSource = new Uri(fullImagePath);
                    loadedImage.CacheOption = BitmapCacheOption.OnLoad;
                    loadedImage.EndInit();
                    loadedImage.Freeze();
                    profileImage.Source = loadedImage;
                    // Update profile image reference
                    profile.userProfileImage = loadedImage;
                    _mainWindow.ValidProfiles[i] = profile;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image for {profile.Username}: {ex.Message}");
                    profile.userProfileImage = _mainWindow._defaultUserProfileImage;
                    _mainWindow.ValidProfiles[i] = profile;
                }
                profileContainer.Children.Add(profileImage);

                // Create an overlaid label at the top for the username.
                Label usernameLabel = new Label
                {
                    Content = profile.Username,
                    Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)), // Semi-transparent black
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    Padding = new Thickness(2),
                    Height = 25,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                // Optional: add a slight margin so the label doesn't cover the very top edge
                usernameLabel.Margin = new Thickness(0, 2, 0, 0);
                Panel.SetZIndex(usernameLabel, 1);
                profileContainer.Children.Add(usernameLabel);

                // Wrap the container in a Button to make it clickable.
                Button profileButton = new Button
                {
                    Content = profileContainer,
                    Width = 100,
                    Height = 120,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0)
                };
                profileButton.Click += (sender, e) => ProfileImage_Click(profile);

                profilesPanel.Children.Add(profileButton);

                // Set the current profile if not already set.
                if (_mainWindow._currentProfile == null)
                {
                    _mainWindow._currentProfile = profile;
                    _mainWindow._defaultUserProfileImage = profile.userProfileImage;
                    ProfileMenuCurrentUser.Content = profile.Username;
                    ProfileImage.Source = profileImage.Source;
                }
            }
        }



        private void ProfileImage_Click(UserProfile profile)
        {
            _mainWindow._currentProfile = profile;
            _mainWindow._defaultUserProfileImage = profile.userProfileImage;
            ProfileMenuCurrentUser.Content = profile.Username;
            ProfileImage.Source = profile.userProfileImage;
            // Optionally close the ProfileSettings window:
            // this.Close();
        }
        private void SelectNewProfileImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select an image to attach",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                try
                {
                    NewProfileImg = new BitmapImage(new Uri(selectedFilePath));
                    this.NewProfileImagePic.Source = NewProfileImg;
                    this.last_filepath = selectedFilePath;

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading profile image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // *** Helper function to get the relative path ***
        private string GetRelativePath(string fullPath)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return System.IO.Path.GetRelativePath(currentDirectory, fullPath);
        }

        //New profile Handling for renaming profiles:

        private void ReAssignProfileImgButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select an image for your profile",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;
                try
                {
                    // Load the new image.
                    BitmapImage newBitmap = new BitmapImage();
                    newBitmap.BeginInit();
                    newBitmap.UriSource = new Uri(selectedFilePath);
                    newBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    newBitmap.EndInit();

                    // Update the current profile image.
                    UserProfile updatedProfile = _mainWindow._currentProfile.Value;
                    updatedProfile.userProfileImage = newBitmap;
                    // Optionally update ProfileImagePath if you copy the file into your Profiles folder.
                    // For example:
                    string profilesDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
                    Directory.CreateDirectory(profilesDirectory);
                    string newFilePath = Path.Combine(profilesDirectory, Path.GetFileName(selectedFilePath));
                    File.Copy(selectedFilePath, newFilePath, true);
                    string relativePath = GetRelativePath(newFilePath);
                    updatedProfile.ProfileImagePath = relativePath;

                    _mainWindow._currentProfile = updatedProfile;

                    // Update the matching entry in ValidProfiles.
                    for (int i = 0; i < _mainWindow.ValidProfiles.Count; i++)
                    {
                        if (_mainWindow.ValidProfiles[i].UserUUID == updatedProfile.UserUUID)
                        {
                            _mainWindow.ValidProfiles[i] = updatedProfile;
                            break;
                        }
                    }

                    // Update the current profile display.
                    ProfileMenuCurrentUser.Content = updatedProfile.Username;
                    ProfileImage.Source = newBitmap;

                    // Re-serialize and reload profiles.
                    string profilesString = _mainWindow.Serialize_Profiles(_mainWindow.ValidProfiles);
                    File.WriteAllText("Profiles/profiles.txt", profilesString);
                    LoadExistingProfiles();

                    // Optionally refresh chat history if needed.
                    _mainWindow.RefreshChatHistory();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading profile image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RenameProfileEntry_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string newName = RenameProfileEntry.Text.Trim();
                if (!string.IsNullOrEmpty(newName) && _mainWindow._currentProfile != null)
                {
                    // Update current profile's username.
                    UserProfile updatedProfile = _mainWindow._currentProfile.Value;
                    updatedProfile.Username = newName;
                    _mainWindow._currentProfile = updatedProfile;

                    // Update the matching entry in ValidProfiles.
                    for (int i = 0; i < _mainWindow.ValidProfiles.Count; i++)
                    {
                        if (_mainWindow.ValidProfiles[i].UserUUID == updatedProfile.UserUUID)
                        {
                            _mainWindow.ValidProfiles[i] = updatedProfile;
                            break;
                        }
                    }

                    // Update the current user display.
                    ProfileMenuCurrentUser.Content = newName;

                    // Re-serialize the profiles.
                    string profilesString = _mainWindow.Serialize_Profiles(_mainWindow.ValidProfiles);
                    File.WriteAllText("Profiles/profiles.txt", profilesString);

                    // Reload the profiles UI.
                    LoadExistingProfiles();

                    // Optionally, refresh the chat history if messages display the username.
                    _mainWindow.RefreshChatHistory();
                }
            }
        }



    }
}
