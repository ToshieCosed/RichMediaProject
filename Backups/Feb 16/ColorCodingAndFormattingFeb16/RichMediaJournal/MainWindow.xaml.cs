using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static RichMediaJournal.MainWindow;
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace RichMediaJournal
{
    public struct Message
    {
        public string MessageString { get; set; }
        public DateTime MessageTimeStamp { get; set; }
        public string MessageSender { get; set; }
        public BitmapImage SenderProfileImage { get; set; } // Add this for the profile picture
        public List<MediaContainer> Attachments { get; set; }
        public UserProfile MessageSenderProfile { get; set; }

        public Message(string sender, string message, BitmapImage profileImage = null) // Constructor overload
        {
            MessageSender = sender;
            MessageString = message;
            MessageTimeStamp = DateTime.Now;
            Attachments = new List<MediaContainer>();
            SenderProfileImage = profileImage; // Initialize the profile image
            MessageSenderProfile = new UserProfile();
        }
    }

    public struct UserProfile
    {
        public string Username { get; set; }
        public string ProfileImagePath { get; set; }
        public bool IsDefault { get; set; }
        [JsonIgnore]
        public BitmapImage userProfileImage { get; set; }

        public UserProfile(string username, string profileImagePath, bool isDefault, BitmapImage userImage)
        {
            Username = username;
            ProfileImagePath = profileImagePath;
            IsDefault = isDefault;
            userProfileImage = userImage;
        }
    }

    public partial class MainWindow : Window
    {
        // -------------------- NEW: Color tag dictionary --------------------
        // You can update this dictionary as needed.
        private static readonly Dictionary<string, Brush> colorTags = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase)
        {
           // Standard Colors
    { "Blue", Brushes.Blue },
    { "Red", Brushes.Red },
    { "ForestGreen", new SolidColorBrush(Color.FromRgb(34, 139, 34)) },
    { "Sepia", new SolidColorBrush(Color.FromRgb(112, 66, 20)) },
    { "Green", Brushes.Green },
    { "Yellow", Brushes.Yellow },
    { "Orange", Brushes.Orange },
    { "Purple", Brushes.Purple },
    { "Pink", Brushes.Pink },
    { "Cyan", Brushes.Cyan },
    { "Magenta", Brushes.Magenta },
    { "Black", Brushes.Black },
    { "White", Brushes.White },

    // Neon Varieties
    { "NeonGreen", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#39FF14")) }, // Bright neon green
    { "NeonBlue", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4D4DFF")) },  // Vibrant neon blue
    { "NeonPink", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6EC7")) },  // Vivid neon pink
    { "NeonOrange", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5F1F")) },// Bold neon orange
    { "NeonYellow", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF33")) },// Bright neon yellow
    { "NeonPurple", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B026FF")) }, // Intense neon purple

    // Pastel Varieties
    { "PastelBlue", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AEC6CF")) },   // Light pastel blue
    { "PastelPink", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1DC")) },   // Soft pastel pink
    { "PastelGreen", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#77DD77")) },  // Pastel green
    { "PastelPurple", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C3B1E1")) }, // Pastel purple
    { "PastelYellow", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDFD96")) }, // Pastel yellow
    { "PastelOrange", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB347")) }  // Pastel orange

            // Add more color mappings if desired.
        };

        private Dictionary<string, Dictionary<string, List<Message>>> categoryData = new();
        private string currentCategory = "Notes";
        private string currentChannel = "General"; // Default channel for every category
        // Use this for user images for now, and then change it later.
        public BitmapImage? _defaultUserProfileImage; // Store the default profile image
        public UserProfile? _currentProfile;
        public List<UserProfile> ValidProfiles = new List<UserProfile>();

        public struct MediaContainer  // Corrected MediaContainer struct
        {
            public string OriginalFilename { get; set; }
            public string LocalCachePath { get; set; }
            public string OriginalUri { get; set; } // Store the original URI (ADDED!)
            public bool IsImage { get; set; }

            public MediaContainer(string filename, string cachePath, string originalUri = null, bool isImage = true) // Updated constructor
            {
                OriginalFilename = filename;
                LocalCachePath = cachePath;
                OriginalUri = originalUri; // Initialize OriginalUri
                IsImage = isImage;
            }
        }

        public MainWindow()
        {
            // Load the settings icon.  Adjust path as needed.
            try
            {
                // Option 1: From project resources (recommended)
                Application.Current.Resources["SettingsIcon"] = new BitmapImage(new Uri("pack://application:,,,/RichMediaJournal;component/Resources/settings_icon.png")); // Replace with your path

                // Option 2: From a file path (less portable)
                // Application.Current.Resources["SettingsIcon"] = new BitmapImage(new Uri(@"C:\path\to\your\icon\settings_icon.png")); // Replace with your path

            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error loading settings icon: " + ex.Message);
            }

            InitializeComponent();

            //Call loading
            string profilesFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles/profiles.txt");
            this.ValidProfiles = Deserialize_Profiles(profilesFilePath);

            // Initialize default categories and a default channel inside each
            categoryData["Notes"] = new Dictionary<string, List<Message>> { { "General", new List<Message>() } };
            categoryData["Local"] = new Dictionary<string, List<Message>> { { "General", new List<Message>() } };

            if (ChannelList == null)
            {
                MessageBox.Show("ChannelList is not recognized in XAML.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            LoadCategories();
            LoadChannels();
        }

        // ---------------------- CATEGORY MANAGEMENT ---------------------- //

        private void LoadCategories()
        {
            BranchCategoryList.Items.Clear();
            foreach (var category in categoryData.Keys)
            {
                Button categoryButton = CreateCategoryButton(category);
                BranchCategoryList.Items.Add(categoryButton);
            }
        }

        private Button CreateCategoryButton(string category)
        {
            Button button = new Button
            {
                Content = category,
                Background = System.Windows.Media.Brushes.Gray,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            button.Click += (s, args) => SwitchCategory(category);
            return button;
        }

        private void AddCategory_Click(object sender, RoutedEventArgs e)
        {
            string newCategory = NewCategoryInput.Text.Trim();
            if (!string.IsNullOrEmpty(newCategory) && !categoryData.ContainsKey(newCategory))
            {
                categoryData[newCategory] = new Dictionary<string, List<Message>> { { "General", new List<Message>() } };

                Button categoryButton = CreateCategoryButton(newCategory);
                BranchCategoryList.Items.Add(categoryButton);

                SwitchCategory(newCategory);
                NewCategoryInput.Clear();
            }
        }

        private void SwitchCategory(string category)
        {
            if (categoryData.ContainsKey(category))
            {
                currentCategory = category;
                LoadChannels();
                SwitchChannel("General"); // Always switch to "General" when changing categories
                UpdateChatTitle();
            }
        }
        // ---------------------- CHANNEL MANAGEMENT ---------------------- //

        private void LoadChannels()
        {
            ChannelList.Items.Clear();
            if (categoryData.ContainsKey(currentCategory))
            {
                foreach (var channel in categoryData[currentCategory].Keys)
                {
                    Button channelButton = CreateChannelButton(channel);
                    ChannelList.Items.Add(channelButton);
                }
            }
        }

        private void ClearPlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Text == "Enter category name")
            {
                textBox.Text = "";
                textBox.Foreground = System.Windows.Media.Brushes.White;
            }
        }

        private void RestorePlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Enter category name";
                textBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        private void DeleteMessage(Message messageToDelete)
        {
            if (categoryData.ContainsKey(currentCategory) && categoryData[currentCategory].ContainsKey(currentChannel))
            {
                var messages = categoryData[currentCategory][currentChannel];

                // Remove the message from the list
                messages.Remove(messageToDelete);

                // Refresh the chat UI
                RefreshChatHistory();
            }
        }

        private Button CreateChannelButton(string channel)
        {
            Button button = new Button
            {
                Content = channel,
                Background = System.Windows.Media.Brushes.DarkSlateGray,
                Foreground = System.Windows.Media.Brushes.White,
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch
            };
            button.Click += (s, args) => SwitchChannel(channel);
            return button;
        }

        private void AddChannel_Click(object sender, RoutedEventArgs e)
        {
            string newChannel = NewChannelInput.Text.Trim();
            if (!string.IsNullOrEmpty(newChannel) && categoryData[currentCategory] != null &&
                !categoryData[currentCategory].ContainsKey(newChannel))
            {
                categoryData[currentCategory][newChannel] = new List<Message>();

                Button channelButton = CreateChannelButton(newChannel);
                ChannelList.Items.Add(channelButton);

                SwitchChannel(newChannel);
                NewChannelInput.Clear();
            }
        }

        private void SwitchToNotes_Click(object sender, RoutedEventArgs e)
        {
            SwitchCategory("Notes");
        }

        private void SwitchToLocal_Click(object sender, RoutedEventArgs e)
        {
            SwitchCategory("Local");
        }

        private void SwitchChannel(string channel)
        {
            if (categoryData[currentCategory].ContainsKey(channel))
            {
                currentChannel = channel;
                RefreshChatHistory();
                UpdateChatTitle();
            }
        }

        private void OpenImagePopup(string imagePath)
        {
            Window popupWindow = new Window
            {
                Title = "Image Viewer",
                Background = System.Windows.Media.Brushes.Black,
                WindowStyle = WindowStyle.SingleBorderWindow,
                ResizeMode = ResizeMode.CanResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            try
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.EndInit();

                Image img = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform // Maintain aspect ratio
                };

                img.Loaded += (s, e) =>
                {
                    double screenWidth = SystemParameters.WorkArea.Width;
                    double screenHeight = SystemParameters.WorkArea.Height;

                    double imageWidth = img.ActualWidth;
                    double imageHeight = img.ActualHeight;

                    double maxWidth = screenWidth * 0.9;  // 90% of screen width
                    double maxHeight = screenHeight * 0.9; // 90% of screen height

                    if (imageWidth <= maxWidth && imageHeight <= maxHeight)
                    {
                        // Image is smaller than or equal to screen, show at actual size (up to max)
                        popupWindow.Width = Math.Min(imageWidth + 40, screenWidth); // Respect screen bounds
                        popupWindow.Height = Math.Min(imageHeight + 60, screenHeight);
                    }
                    else
                    {
                        // Image is larger than screen, scale down
                        popupWindow.MaxWidth = maxWidth;
                        popupWindow.MaxHeight = maxHeight;

                        popupWindow.Width = maxWidth; // Scale to max width/height
                        popupWindow.Height = maxHeight;
                    }
                };

                Button closeButton = new Button
                {
                    Content = "Close",
                    Width = 80,
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Bottom
                };

                closeButton.Click += (s, e) => popupWindow.Close();

                Grid grid = new Grid();
                grid.RowDefinitions.Add(new RowDefinition());
                grid.RowDefinitions.Add(new RowDefinition() { Height = GridLength.Auto });

                Grid.SetRow(img, 0);
                Grid.SetRow(closeButton, 1);

                grid.Children.Add(img);
                grid.Children.Add(closeButton);

                popupWindow.Content = grid;
                popupWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image in popup: {ex.Message} Path: {imagePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                popupWindow.Close();
            }
        }

        private void UpdateChatTitle()
        {
            ChatTitleBar.Text = $"{currentCategory} #{currentChannel}";
        }

        // ---------------------- CHAT HISTORY MANAGEMENT ---------------------- //

        private void MessageInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.OriginalSource is TextBox textBox)
                {
                    if (Keyboard.Modifiers == ModifierKeys.None) // Regular Enter
                    {
                        string messageText = textBox.Text;

                        if (textBox.LineCount == 1 || textBox.LineCount > 1) // Send if single or multi-line
                        {
                            if (!string.IsNullOrWhiteSpace(messageText) ||
                                (categoryData[currentCategory][currentChannel].Count > 0 &&
                                 categoryData[currentCategory][currentChannel].Last().Attachments.Count > 0 &&
                                 string.IsNullOrWhiteSpace(categoryData[currentCategory][currentChannel].Last().MessageString)))
                            {
                                SendMessage(messageText);
                                Dispatcher.BeginInvoke((Action)(() => textBox.Clear()));
                                e.Handled = true;
                            }
                            else
                            {
                                Dispatcher.BeginInvoke((Action)(() => textBox.Clear()));
                                e.Handled = true;
                            }
                        }
                    }
                    else if (Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control) // Shift+Enter or Ctrl+Enter
                    {
                        ScrollViewer scrollViewer = FindVisualParent<ScrollViewer>(textBox);

                        textBox.AppendText(Environment.NewLine);
                        textBox.CaretIndex = textBox.Text.Length;

                        if (scrollViewer != null)
                        {
                            scrollViewer.ScrollToVerticalOffset(scrollViewer.ExtentHeight);
                        }

                        e.Handled = true;
                    }
                }
            }
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string messageText = MessageInput.Text;
            if (!string.IsNullOrWhiteSpace(messageText) ||
                (categoryData[currentCategory][currentChannel].Count > 0 &&
                 categoryData[currentCategory][currentChannel].Last().Attachments.Count > 0 &&
                 string.IsNullOrWhiteSpace(categoryData[currentCategory][currentChannel].Last().MessageString)))
            {
                SendMessage(messageText);
                Dispatcher.BeginInvoke((Action)(() => MessageInput.Clear()));
            }
            else
            {
                Dispatcher.BeginInvoke((Action)(() => MessageInput.Clear()));
            }
        }

        private void SendMessage(string messageText)
        {
            string username_ = "User"; // Default username
            BitmapImage img_instance = _defaultUserProfileImage; // Default image

            if (_currentProfile != null)
            {
                if (!string.IsNullOrWhiteSpace(_currentProfile.Value.Username))
                {
                    username_ = _currentProfile.Value.Username;

                    UserProfile foundProfile = ValidProfiles.Where(x => x.Username == username_).FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(foundProfile.Username))
                    {
                        img_instance = foundProfile.userProfileImage ?? _defaultUserProfileImage;
                    }
                }
            }

            Message newMessage = new Message(username_, messageText, img_instance); // Create a *new* message *every time*

            bool urlEmbedded = false;
            List<string> embeddedUrls = new List<string>();

            string[] words = messageText.Split(' ');

            foreach (string word in words)
            {
                if (Uri.TryCreate(word, UriKind.Absolute, out Uri uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
                    (word.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                     word.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                     word.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                     word.EndsWith(".webp", StringComparison.OrdinalIgnoreCase)))
                {
                    embeddedUrls.Add(word);
                    urlEmbedded = true;
                    newMessage.MessageString = newMessage.MessageString.Replace(word, "").Trim();
                }
            }

            if (embeddedUrls.Count > 0)
            {
                int downloadsCompleted = 0;
                foreach (string url in embeddedUrls)
                {
                    Uri uri = new Uri(url);
                    string fileName = Path.GetFileName(uri.LocalPath);
                    string categoryPath = Path.Combine("Cache", currentCategory, currentChannel);
                    Directory.CreateDirectory(categoryPath);
                    string localCachePath = Path.Combine(categoryPath, fileName);

                    WebClient client = new WebClient();

                    client.DownloadFileCompleted += (s, e) =>
                    {
                        if (e.Error == null)
                        {
                            MediaContainer media = new MediaContainer(fileName, localCachePath, uri.ToString());
                            newMessage.Attachments.Add(media);
                        }
                        else
                        {
                            MessageBox.Show($"Failed to download image: {e.Error.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }

                        client.Dispose();
                        downloadsCompleted++;

                        if (downloadsCompleted == embeddedUrls.Count)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                categoryData[currentCategory][currentChannel].Add(newMessage);
                                RefreshChatHistory();
                                MessageInput.Clear();
                            });
                        }
                    };

                    try
                    {
                        client.DownloadFileAsync(uri, localCachePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to handle media: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        client.Dispose();
                    }
                }
            }
            else
            {
                categoryData[currentCategory][currentChannel].Add(newMessage);
                RefreshChatHistory();
                MessageInput.Clear();
            }
        }

        // Helper function to find a visual parent of a specific type
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                {
                    return typedParent;
                }
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // ---------------------- UI EVENT HELPERS ---------------------- //

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            //Sidebar.Visibility = Sidebar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ProfileSettings settingswindow = new ProfileSettings(this);
            settingswindow.Show();
        }

        private void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Select an image to attach",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.webp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string selectedFilePath = openFileDialog.FileName;

                if (_defaultUserProfileImage == null)
                {
                    try
                    {
                        _defaultUserProfileImage = new BitmapImage(new Uri(selectedFilePath));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading profile image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                Message newMessage = new Message("User", "", _defaultUserProfileImage);
                HandleMedia(newMessage, selectedFilePath, false);
                categoryData[currentCategory][currentChannel].Add(newMessage);
                RefreshChatHistory();
            }
        }

        // ------------------ UPDATED: RefreshChatHistory ------------------
        private void RefreshChatHistory()
        {
            ChatHistory.Children.Clear();

            if (categoryData.ContainsKey(currentCategory) && categoryData[currentCategory].ContainsKey(currentChannel))
            {
                var messages = categoryData[currentCategory][currentChannel];

                foreach (var msg in messages.ToList())
                {
                    // Main Message Container (Grid for better layout)
                    Grid messageGrid = new Grid { Margin = new Thickness(5) };
                    messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title bar row
                    messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Profile/Message row
                    messageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Profile picture column
                    messageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Message content column

                    // 1. Title Bar (Discord-like styling)
                    Grid titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(54, 57, 63)) };
                    TextBlock userLabel = new TextBlock
                    {
                        Text = msg.MessageSender,
                        FontWeight = FontWeights.Bold,
                        FontSize = 16,
                        Foreground = Brushes.White,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    titleBar.Children.Add(userLabel);

                    TextBlock timeLabel = new TextBlock
                    {
                        Text = $"({msg.MessageTimeStamp:yyyy-MM-dd HH:mm})",
                        Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)),
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    titleBar.Children.Add(timeLabel);

                    Grid.SetRow(titleBar, 0);
                    Grid.SetColumnSpan(titleBar, 2);
                    messageGrid.Children.Add(titleBar);

                    // 2. Profile Picture
                    BitmapImage profileImageToUse = _defaultUserProfileImage ?? msg.SenderProfileImage;
                    if (_currentProfile != null)
                    {
                        if (ValidProfiles.Count > 0)
                        {
                            UserProfile _prof = ValidProfiles.Where(x => x.Username == _currentProfile.Value.Username).FirstOrDefault();
                            if (_prof.Username == _currentProfile.Value.Username && _prof.userProfileImage != null)
                            {
                                profileImageToUse = msg.SenderProfileImage;
                            }
                        }
                    }
                    Image profileImage = new Image
                    {
                        Source = profileImageToUse,
                        Width = 100,
                        Height = 100,
                        VerticalAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 5, 0, 0)
                    };
                    Grid.SetRow(profileImage, 1);
                    Grid.SetColumn(profileImage, 0);
                    messageGrid.Children.Add(profileImage);

                    // 3. Message Content with formatted text
                    TextBlock messageText = new TextBlock
                    {
                        Margin = new Thickness(5),
                        Width = 450,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    // Instead of plain text, add inlines produced by our parser.
                    messageText.Inlines.Add(ParseFormattedText(msg.MessageString));
                    Grid.SetRow(messageText, 1);
                    Grid.SetColumn(messageText, 1);
                    messageGrid.Children.Add(messageText);

                    // 4. Embedded Images
                    if (msg.Attachments.Count > 0)
                    {
                        foreach (var attachment in msg.Attachments)
                        {
                            try
                            {
                                string imagePathToUse = attachment.OriginalUri ?? attachment.LocalCachePath;
                                BitmapImage embeddedImage = null;

                                if (Uri.TryCreate(imagePathToUse, UriKind.Absolute, out Uri uri) &&
                                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                                {
                                    embeddedImage = new BitmapImage(uri);
                                }
                                else
                                {
                                    embeddedImage = new BitmapImage(new Uri(imagePathToUse));
                                }

                                Image imageControl = new Image
                                {
                                    Source = embeddedImage,
                                    MaxWidth = 300,
                                    MaxHeight = 300,
                                    Margin = new Thickness(5),
                                    Cursor = Cursors.Hand
                                };
                                imageControl.MouseDown += (sender, e) => OpenImagePopup(imagePathToUse);

                                Grid.SetRow(imageControl, 2);
                                Grid.SetColumn(imageControl, 1);
                                messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                                messageGrid.Children.Add(imageControl);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error loading embedded image: {ex.Message} Path: {attachment.OriginalUri ?? attachment.LocalCachePath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }

                    messageGrid.Background = new SolidColorBrush(Color.FromRgb(47, 49, 54));
                    ChatHistory.Children.Add(messageGrid);
                }
            }
        }

        private void HandleMedia(Message message, string mediaPath, bool isRemote)
        {
            string fileName = Path.GetFileName(mediaPath);
            string categoryPath = Path.Combine("Cache", currentCategory, currentChannel);
            Directory.CreateDirectory(categoryPath);
            string localCachePath = Path.Combine(categoryPath, fileName);

            string originalUri = null;

            if (isRemote)
            {
                File.Copy(mediaPath, localCachePath, true);
                originalUri = mediaPath;
            }
            else
            {
                localCachePath = mediaPath;
                originalUri = "file://" + mediaPath;
            }

            MediaContainer media = new MediaContainer(fileName, localCachePath, originalUri);
            message.Attachments.Add(media);
        }

        private void AddMediaToCurrentMessage(MediaContainer media)
        {
            if (categoryData.ContainsKey(currentCategory) && categoryData[currentCategory].ContainsKey(currentChannel))
            {
                if (categoryData[currentCategory][currentChannel].Count == 0 ||
                    !string.IsNullOrWhiteSpace(MessageInput.Text))
                {
                    categoryData[currentCategory][currentChannel].Last().Attachments.Add(media);
                }
                else
                {
                    Message newMessage = new Message("User", media.LocalCachePath);
                    newMessage.Attachments.Add(media);
                    categoryData[currentCategory][currentChannel].Add(newMessage);
                }

                RefreshChatHistory();
            }
        }

        //==Serialize For Profiles Helpers
        public string Serialize_Profiles(List<UserProfile> profiles_in)
        {
            string profiles_string = System.Text.Json.JsonSerializer.Serialize(profiles_in);
            Console.WriteLine("Test");
            return profiles_string;
        }

        public List<UserProfile> Deserialize_Profiles(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new List<UserProfile>();
            }

            try
            {
                string jsonString = File.ReadAllText(filePath);
                List<UserProfile> profiles = JsonSerializer.Deserialize<List<UserProfile>>(jsonString);

                if (profiles != null && profiles.Count > 0)
                {
                    for (int i = 0; i < profiles.Count; i++)
                    {
                        UserProfile profile = profiles[i];

                        if (profile.userProfileImage == null && !string.IsNullOrEmpty(profile.ProfileImagePath))
                        {
                            try
                            {
                                profile.userProfileImage = new BitmapImage(new Uri(profile.ProfileImagePath, UriKind.RelativeOrAbsolute));
                                profiles[i] = profile;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Error loading image for {profile.Username}: {ex.Message}");
                            }
                        }
                    }
                }

                return profiles;
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"Error deserializing JSON: {ex.Message}");
                return new List<UserProfile>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading profiles: {ex.Message}");
                return new List<UserProfile>();
            }
        }

        // ------------------ NEW: Inline Formatting Parser ------------------
        // This function scans the input text and produces a Span (an Inline container)
        // with Runs that have formatting applied according to our simple inline tags.
        // Supported markers:
        //   **text** => bold
        //   *text*   => italic
        //   __text__ => underline
        //   --text-- => strikethrough (crossed out)
        //   [ColorTag] => change foreground color (e.g., [Blue], [Red], etc.)
        // Also, URLs (starting with http:// or https://) are turned into clickable links.
        private Inline ParseFormattedText(string text)
        {
            Span span = new Span();

            // formatting state flags
            bool bold = false, italic = false, underline = false, crossout = false;
            // default color (Discord-like light gray)
            Brush currentBrush = new SolidColorBrush(Color.FromRgb(220, 221, 225));

            StringBuilder currentRunText = new StringBuilder();
            int i = 0;
            int len = text.Length;

            // Helper local function: flush the current text into a run with current formatting.
            void FlushCurrentText()
            {
                if (currentRunText.Length > 0)
                {
                    string runText = currentRunText.ToString();
                    // Prepare combined text decorations if needed.
                    TextDecorationCollection decorations = new TextDecorationCollection();
                    if (underline) decorations.Add(TextDecorations.Underline[0]);
                    if (crossout) decorations.Add(TextDecorations.Strikethrough[0]);

                    // Create a run with current formatting.
                    // Instead of adding the run directly, process it for hyperlinks.
                    foreach (var inline in ParseHyperlinks(runText, currentBrush,
                        bold ? FontWeights.Bold : FontWeights.Normal,
                        italic ? FontStyles.Italic : FontStyles.Normal,
                        decorations))
                    {
                        span.Inlines.Add(inline);
                    }
                    currentRunText.Clear();
                }
            }

            // Main loop: scan each character and detect markers.
            while (i < len)
            {
                // Check for bold marker (**)
                if (i <= len - 2 && text.Substring(i, 2) == "**")
                {
                    FlushCurrentText();
                    bold = !bold;
                    i += 2;
                    continue;
                }
                // Check for underline marker (__)
                else if (i <= len - 2 && text.Substring(i, 2) == "__")
                {
                    FlushCurrentText();
                    underline = !underline;
                    i += 2;
                    continue;
                }
                // Check for crossout marker (--)
                else if (i <= len - 2 && text.Substring(i, 2) == "--")
                {
                    FlushCurrentText();
                    crossout = !crossout;
                    i += 2;
                    continue;
                }
                // Check for color tag marker: e.g., [Blue] or [Red]
                else if (text[i] == '[')
                {
                    int closeIndex = text.IndexOf(']', i);
                    if (closeIndex != -1)
                    {
                        string tagContent = text.Substring(i + 1, closeIndex - i - 1);
                        if (colorTags.TryGetValue(tagContent, out Brush newBrush))
                        {
                            FlushCurrentText();
                            currentBrush = newBrush;
                            i = closeIndex + 1;
                            continue;
                        }
                    }
                    // If not a valid tag, treat '[' as normal character.
                    currentRunText.Append(text[i]);
                    i++;
                    continue;
                }
                // Check for italic marker (*)
                else if (text[i] == '*')
                {
                    FlushCurrentText();
                    italic = !italic;
                    i++;
                    continue;
                }
                else
                {
                    currentRunText.Append(text[i]);
                    i++;
                }
            }
            FlushCurrentText();
            return span;
        }

        // This helper function looks for URLs in the given plain text and splits the text
        // into Runs and Hyperlinks accordingly.
        private IEnumerable<Inline> ParseHyperlinks(string text, Brush currentBrush, FontWeight fontWeight, FontStyle fontStyle, TextDecorationCollection decorations)
        {
            // Regular expression for URLs.
            Regex regex = new Regex(@"(https?://[^\s]+)");
            int lastIndex = 0;
            foreach (Match m in regex.Matches(text))
            {
                if (m.Index > lastIndex)
                {
                    yield return new Run(text.Substring(lastIndex, m.Index - lastIndex))
                    {
                        Foreground = currentBrush,
                        FontWeight = fontWeight,
                        FontStyle = fontStyle,
                        TextDecorations = decorations
                    };
                }
                Hyperlink link = new Hyperlink(new Run(m.Value))
                {
                    NavigateUri = new Uri(m.Value),
                    Foreground = Brushes.LightBlue, // link color
                    FontWeight = fontWeight,
                    FontStyle = fontStyle,
                    TextDecorations = decorations
                };
                // Open URL in default browser.
                link.RequestNavigate += (sender, e) =>
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
                };
                yield return link;
                lastIndex = m.Index + m.Length;
            }
            if (lastIndex < text.Length)
            {
                yield return new Run(text.Substring(lastIndex))
                {
                    Foreground = currentBrush,
                    FontWeight = fontWeight,
                    FontStyle = fontStyle,
                    TextDecorations = decorations
                };
            }
        }
    }
}
