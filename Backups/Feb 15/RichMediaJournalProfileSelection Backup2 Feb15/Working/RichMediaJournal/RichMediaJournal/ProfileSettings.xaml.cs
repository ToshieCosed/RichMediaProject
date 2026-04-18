using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
                    BitmapImage bitmapImage = new BitmapImage(); // Create the BitmapImage

                    bitmapImage.BeginInit(); // Begin initialization
                    bitmapImage.UriSource = new Uri(last_filepath); // Set the source
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // Recommended for performance
                    bitmapImage.EndInit();   // End initialization



                    UserProfile userProfile = new UserProfile(this.UserNameCreationLabel.Content.ToString(), last_filepath, false, bitmapImage);
                    userProfile.ProfileImagePath = last_filepath;
                    userProfile.userProfileImage = bitmapImage;
                    _mainWindow._defaultUserProfileImage = bitmapImage; // Now assign it
                    _mainWindow._currentProfile = userProfile;

                    //Add it to the list so it can be referenced by refresh chat.
                    _mainWindow.ValidProfiles.Add(userProfile);

                    LoadExistingProfiles(); // Refresh the profile display
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


        private void ProfileImage_Click(UserProfile profile)
        {
            _mainWindow._currentProfile = profile;
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
    }
}
