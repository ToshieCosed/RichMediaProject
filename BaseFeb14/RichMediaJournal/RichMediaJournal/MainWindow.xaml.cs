using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using static RichMediaJournal.MainWindow;
using System.Net;
using static RichMediaJournal.MainWindow;

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

        public UserProfile(string username, string profileImagePath, bool isDefault = false)
        {
            Username = username;
            ProfileImagePath = profileImagePath;
            IsDefault = isDefault;
        }
    }

    public partial class MainWindow : Window
    {
        private Dictionary<string, Dictionary<string, List<Message>>> categoryData = new();
        private string currentCategory = "Notes";
        private string currentChannel = "General"; // Default channel for every category

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
            InitializeComponent();

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
                    double screenWidth = System.Windows.SystemParameters.WorkArea.Width;
                    double screenHeight = System.Windows.SystemParameters.WorkArea.Height;

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




        private void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string messageText = MessageInput.Text.Trim();

            if (!string.IsNullOrWhiteSpace(messageText) ||  // Check for text OR attachments
                (categoryData[currentCategory][currentChannel].Count > 0 && // Check if there is already a message
                 categoryData[currentCategory][currentChannel].Last().Attachments.Count > 0 && // Check if the last message has an attachment
                 string.IsNullOrWhiteSpace(categoryData[currentCategory][currentChannel].Last().MessageString))) // Check if the last message's text is empty

            {
                if (categoryData[currentCategory][currentChannel].Count > 0)
                {
                    for (int i = categoryData[currentCategory][currentChannel].Count - 1; i >= 0; i--)
                    {
                        if (categoryData[currentCategory][currentChannel][i].Attachments.Count > 0 &&
                            string.IsNullOrWhiteSpace(categoryData[currentCategory][currentChannel][i].MessageString) &&
                            !string.IsNullOrWhiteSpace(messageText))
                        {
                            // Correctly update the message:
                            Message updatedMessage = categoryData[currentCategory][currentChannel][i]; // Get a COPY
                            updatedMessage.MessageString = messageText; // Modify the COPY
                            categoryData[currentCategory][currentChannel][i] = updatedMessage; // Set the COPY back in the list

                            break;
                        }
                    }
                }
                else
                {
                    Message newMessage = new Message("User", messageText); // Create a new message
                    categoryData[currentCategory][currentChannel].Add(newMessage);
                }

                RefreshChatHistory();
                MessageInput.Clear();
            }
        }



        private void SendMessage()
        {
            string messageText = MessageInput.Text.Trim();

            if (!string.IsNullOrWhiteSpace(messageText))
            {
                Message newMessage = new Message("User", messageText, _defaultUserProfileImage);
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
                                // *Crucial Change:* Add the message *here*, after all downloads are complete.
                                Dispatcher.Invoke(() =>
                                {
                                    categoryData[currentCategory][currentChannel].Add(newMessage); // Add the message
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
                    // If no URLs, add message immediately
                    categoryData[currentCategory][currentChannel].Add(newMessage);
                    RefreshChatHistory();
                    MessageInput.Clear();
                }
            }
        }









        private void MessageInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage(); // Call SendMessage on Enter
            }
        }

        // ---------------------- UI EVENT HELPERS ---------------------- //

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            Sidebar.Visibility = Sidebar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }



        private BitmapImage _defaultUserProfileImage; // Store the default profile image

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

                // Load the image for the profile picture
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

                Message newMessage = new Message("User", "", _defaultUserProfileImage); // Include profile image
                HandleMedia(newMessage, selectedFilePath, false);
                categoryData[currentCategory][currentChannel].Add(newMessage);
                RefreshChatHistory();
            }
        }


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
                    Grid titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(54, 57, 63)) }; // Discord background color
                    TextBlock userLabel = new TextBlock
                    {
                        Text = msg.MessageSender,
                        FontWeight = FontWeights.Bold,
                        FontSize = 16,
                        Foreground = Brushes.White, // White text
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(5, 0, 0, 0)
                    };
                    titleBar.Children.Add(userLabel);

                    TextBlock timeLabel = new TextBlock
                    {
                        Text = $"({msg.MessageTimeStamp.ToString("yyyy-MM-dd HH:mm")})",
                        Foreground = new SolidColorBrush(Color.FromRgb(153, 153, 153)), // Lighter gray for timestamp
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    titleBar.Children.Add(timeLabel);

                    Grid.SetRow(titleBar, 0);
                    Grid.SetColumnSpan(titleBar, 2);
                    messageGrid.Children.Add(titleBar);


                    // 2. Profile Picture
                    BitmapImage profileImageToUse = _defaultUserProfileImage ?? msg.SenderProfileImage; // Use either default or message-specific

                    if (profileImageToUse != null)
                    {
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
                    }

                    // 3. Message Content
                    TextBlock messageText = new TextBlock
                    {
                        Text = msg.MessageString,
                        Foreground = new SolidColorBrush(Color.FromRgb(220, 221, 225)), // Discord text color
                        Margin = new Thickness(5),
                        Width = 450,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Top
                    };

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
                                string imagePathToUse = attachment.OriginalUri ?? attachment.LocalCachePath; // Use OriginalUri if available, otherwise LocalCachePath

                                BitmapImage embeddedImage = null;

                                if (Uri.TryCreate(imagePathToUse, UriKind.Absolute, out Uri uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                                {
                                    embeddedImage = new BitmapImage(uri); // It's a URI (remote)
                                }
                                else
                                {
                                    // It's a local file path
                                    embeddedImage = new BitmapImage(new Uri(imagePathToUse)); // Create BitmapImage from local file path.
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

                    // Message Background (Discord-like styling)
                    messageGrid.Background = new SolidColorBrush(Color.FromRgb(47, 49, 54)); // Discord message background
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

            string originalUri = null; // Initialize to null

            if (isRemote)
            {
                File.Copy(mediaPath, localCachePath, true);
                originalUri = mediaPath;  // Set originalUri to the remote URL
            }
            else
            {
                localCachePath = mediaPath; // If it's local, localCachePath is the original path.
                originalUri = "file://" + mediaPath; // Convert local path to URI
            }


            MediaContainer media = new MediaContainer(fileName, localCachePath, originalUri); // Corrected MediaContainer creation
            message.Attachments.Add(media);
        }





        private void AddMediaToCurrentMessage(MediaContainer media)
        {
            if (categoryData.ContainsKey(currentCategory) && categoryData[currentCategory].ContainsKey(currentChannel))
            {
                if (categoryData[currentCategory][currentChannel].Count == 0 ||
                    !string.IsNullOrWhiteSpace(MessageInput.Text))
                {
                    // If message has text, attach image to the latest message
                    categoryData[currentCategory][currentChannel].Last().Attachments.Add(media);
                }
                else
                {
                    // If the message has no text, create a new one with image URI
                    Message newMessage = new Message("User", media.LocalCachePath);
                    newMessage.Attachments.Add(media);
                    categoryData[currentCategory][currentChannel].Add(newMessage);
                }

                RefreshChatHistory();
            }
        }
    }
}
