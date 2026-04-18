using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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

        public ProfileSettings()
        {
            InitializeComponent();

            if (MainWindow._defaultUserProfileImage != null)
            {
                displayimage = MainWindow._defaultUserProfileImage;

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

        }

        private void NewProfileNameTextBox_KeyDown(object sender, KeyEventArgs e)
        {

            if (e.Key == Key.Enter)
            {
               this.UserNameCreationLabel.Content = this.NewProfileNameTextBox.Text;
            }
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
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading profile image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
