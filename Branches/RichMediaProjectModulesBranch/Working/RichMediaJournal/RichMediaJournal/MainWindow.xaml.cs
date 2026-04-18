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
using System.Net;
using System.Text.Json.Serialization;
using System.Text.Json;
using static RichMediaJournal.MainWindow;
using ModuleContract;
using System.Reflection;
using HelloBotExample;

namespace RichMediaJournal
{
    // ------------------ Message Struct ------------------
    public struct Message
    {
        public string MessageString { get; set; }
        public DateTime MessageTimeStamp { get; set; }
        public string MessageSender { get; set; } // legacy username
        public Guid MessageSenderUUID { get; set; } // new UUID for sender

        [JsonIgnore]
        public BitmapImage SenderProfileImage { get; set; }

        public List<MediaContainer> Attachments { get; set; }

        [JsonIgnore]
        public UserProfile MessageSenderProfile { get; set; }

        public string originalusermessage { get; set; }

        // Constructor that accepts a sender UUID (defaulting to Guid.Empty if needed)
        public Message(string sender, Guid senderUUID, string message, BitmapImage profileImage = null)
        {
            MessageSender = sender;
            MessageSenderUUID = senderUUID;
            MessageString = message;
            MessageTimeStamp = DateTime.Now;
            Attachments = new List<MediaContainer>();
            SenderProfileImage = profileImage;
            MessageSenderProfile = new UserProfile();
            originalusermessage = "empty";
        }
    }

    // ------------------ UserProfile Struct ------------------
    public struct UserProfile
    {
        public string Username { get; set; }
        public string ProfileImagePath { get; set; }
        public bool IsDefault { get; set; }
        [JsonIgnore]
        public BitmapImage userProfileImage { get; set; }
        public Guid UserUUID { get; set; } // new UUID property

        // Constructor now accepts an optional UUID (defaults to Guid.Empty)
        public UserProfile(string username, string profileImagePath, bool isDefault, BitmapImage userImage, Guid userUUID = default)
        {
            Username = username;
            ProfileImagePath = profileImagePath;
            IsDefault = isDefault;
            userProfileImage = userImage;
            UserUUID = userUUID;
        }
    }

    // ------------------ MainWindow Class ------------------
    public partial class MainWindow : Window
    {
        // Color tag dictionary for inline formatting.
        private static readonly Dictionary<string, Brush> colorTags = new Dictionary<string, Brush>(StringComparer.OrdinalIgnoreCase)
        {
            { "Blue", Brushes.Blue },
            { "Red", Brushes.Red },
            { "ForestGreen", new SolidColorBrush(Color.FromRgb(34,139,34)) },
            { "Sepia", new SolidColorBrush(Color.FromRgb(112,66,20)) },
            { "Green", Brushes.Green },
            { "Yellow", Brushes.Yellow },
            { "Orange", Brushes.Orange },
            { "Purple", Brushes.Purple },
            { "Pink", Brushes.Pink },
            { "Cyan", Brushes.Cyan },
            { "Magenta", Brushes.Magenta },
            { "Black", Brushes.Black },
            { "White", Brushes.White },
            { "NeonGreen", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#39FF14")) },
            { "NeonBlue", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4D4DFF")) },
            { "NeonPink", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6EC7")) },
            { "NeonOrange", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5F1F")) },
            { "NeonYellow", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF33")) },
            { "NeonPurple", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B026FF")) },
            { "PastelBlue", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#AEC6CF")) },
            { "PastelPink", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFD1DC")) },
            { "PastelGreen", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#77DD77")) },
            { "PastelPurple", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C3B1E1")) },
            { "PastelYellow", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FDFD96")) },
            { "PastelOrange", new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFB347")) }
        };

        // Dictionary to store valid modules keyed by their GUID.
        public Dictionary<Guid, IModule> ValidModules { get; set; } = new Dictionary<Guid, IModule>();


        // Chat data: Category -> (Channel -> List of Messages)
        private Dictionary<string, Dictionary<string, List<Message>>> categoryData = new();
        private string currentCategory = "Notes";
        private string currentChannel = "General";

        // Profile and image management.
        public BitmapImage? _defaultUserProfileImage; // default profile image
        public UserProfile? _currentProfile;
        public List<UserProfile> ValidProfiles = new List<UserProfile>();

        // ------------------ MediaContainer Struct ------------------
        public struct MediaContainer
        {
            public string OriginalFilename { get; set; }
            public string LocalCachePath { get; set; }
            public string OriginalUri { get; set; }
            public bool IsImage { get; set; }

            public MediaContainer(string filename, string cachePath, string originalUri = null, bool isImage = true)
            {
                OriginalFilename = filename;
                LocalCachePath = cachePath;
                OriginalUri = originalUri;
                IsImage = isImage;
            }
        }

        // ------------------ Constructor ------------------
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

            // Load the settings icon.  Adjust path as needed.
            try
            {
                // Option 1: From project resources (recommended)
                Application.Current.Resources["SearchIcon"] = new BitmapImage(new Uri("pack://application:,,,/RichMediaJournal;component/Resources/search_icon.png")); // Replace with your path

                // Option 2: From a file path (less portable)
                // Application.Current.Resources["SettingsIcon"] = new BitmapImage(new Uri(@"C:\path\to\your\icon\settings_icon.png")); // Replace with your path

            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Error loading search icon " + ex.Message);
            }

            InitializeComponent();

            // Load profiles, channels, messages, etc.
            LoadModules();



            // Load profiles.
            string profilesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles/profiles.txt");
            this.ValidProfiles = Deserialize_Profiles(profilesFilePath);

            // Initialize default categories and channels.
            categoryData["Notes"] = new Dictionary<string, List<Message>> { { "General", new List<Message>() } };
            categoryData["Local"] = new Dictionary<string, List<Message>> { { "General", new List<Message>() } };

            // Load any existing categories/channels from disk.
            LoadCategoriesFromDisk();

            // Load UI elements (from XAML) with the current categories/channels.
            LoadCategories();
            LoadChannels();
            // Load messages for the default channel if a JSON file exists.
            LoadChannelData(currentCategory, currentChannel);
            RefreshChatHistory();
        }

        //==Modules
        private void LoadModules()
        {
            // Define the folder where your module assemblies reside.
            string moduleFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Modules");

            if (!Directory.Exists(moduleFolder))
                return; // No modules to load

            // Loop through each DLL in the Modules folder.
            foreach (var dll in Directory.GetFiles(moduleFolder, "*.dll"))
            {
                try
                {
                    // Load the assembly from the DLL.
                    Assembly assembly = Assembly.LoadFrom(dll);

                    // Find all types in the assembly that implement IModule.
                    var moduleTypes = assembly.GetTypes()
                        .Where(t => typeof(IModule).IsAssignableFrom(t) && !t.IsAbstract);

                    foreach (var moduleType in moduleTypes)
                    {
                        // Create an instance of the module.
                        IModule moduleInstance = (IModule)Activator.CreateInstance(moduleType);

                        // Register the module using its GUID.
                        ValidModules[moduleInstance.ModuleGuid] = moduleInstance;

                        //Make sure modules run their on loaded code.
                        moduleInstance.OnLoaded();
                    }
                }
                catch (Exception ex)
                {
                    // Log or handle any errors loading a module.
                    Console.WriteLine($"Error loading module from {dll}: {ex.Message}");
                }
            }
        }

        // ------------------ CATEGORY MANAGEMENT ------------------
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
                Background = Brushes.Gray,
                Foreground = Brushes.White,
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
                // Create a folder for the category if needed.
                string categoryFolder = Path.Combine("NotesCache", newCategory);
                Directory.CreateDirectory(categoryFolder);

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
                SwitchChannel("General"); // always default to "General" channel
                UpdateChatTitle();
            }
        }

        // ------------------ CHANNEL MANAGEMENT ------------------
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

        private Button CreateChannelButton(string channel)
        {
            Button button = new Button
            {
                Content = channel,
                Background = Brushes.DarkSlateGray,
                Foreground = Brushes.White,
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
                // Create a new channel and its JSON file.
                categoryData[currentCategory][newChannel] = new List<Message>();
                string channelFileDir = Path.Combine("NotesCache", currentCategory);
                Directory.CreateDirectory(channelFileDir);
                string channelFile = Path.Combine(channelFileDir, newChannel + ".json");
                File.WriteAllText(channelFile, "[]");

                Button channelButton = CreateChannelButton(newChannel);
                ChannelList.Items.Add(channelButton);

                SwitchChannel(newChannel);
                NewChannelInput.Clear();
            }
        }

        private void SwitchToNotes_Click(object sender, RoutedEventArgs e)
        {
            SwitchCategory("Notes");
            RefreshChatHistory();
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
                // Reload channel data from JSON file.
                LoadChannelData(currentCategory, currentChannel);
                RefreshChatHistory();
                UpdateChatTitle();
            }
        }

        private void UpdateChatTitle()
        {
            ChatTitleBar.Text = $"{currentCategory} #{currentChannel}";
        }

        // ------------------ JSON PERSISTENCE HELPERS ------------------
        private void SaveChannelData(string category, string channel)
        {
            if (categoryData.ContainsKey(category) && categoryData[category].ContainsKey(channel))
            {
                var messages = categoryData[category][channel];
                string dir = Path.Combine("NotesCache", category);
                Directory.CreateDirectory(dir);
                string filePath = Path.Combine(dir, channel + ".json");
                string json = JsonSerializer.Serialize(messages, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);
            }
        }

        private void LoadChannelData(string category, string channel)
        {
            string dir = Path.Combine("NotesCache", category);
            string filePath = Path.Combine(dir, channel + ".json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var messages = JsonSerializer.Deserialize<List<Message>>(json);
                if (messages != null)
                {
                    categoryData[category][channel] = messages;
                }
            }
        }

        // Scan the NotesCache folder for existing categories/channels and load them.
        private void LoadCategoriesFromDisk()
        {
            string baseDir = "NotesCache";
            if (Directory.Exists(baseDir))
            {
                foreach (var categoryDir in Directory.GetDirectories(baseDir))
                {
                    string categoryName = Path.GetFileName(categoryDir);
                    if (!categoryData.ContainsKey(categoryName))
                    {
                        categoryData[categoryName] = new Dictionary<string, List<Message>>();
                    }
                    foreach (var file in Directory.GetFiles(categoryDir, "*.json"))
                    {
                        string channelName = Path.GetFileNameWithoutExtension(file);
                        string json = File.ReadAllText(file);
                        List<Message> messages = JsonSerializer.Deserialize<List<Message>>(json) ?? new List<Message>();
                        if (!categoryData[categoryName].ContainsKey(channelName))
                        {
                            categoryData[categoryName][channelName] = messages;
                        }
                    }
                }
            }
        }

        // ------------------ CHAT HISTORY & MESSAGE HANDLING ------------------
        private void MessageInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (e.OriginalSource is TextBox textBox)
                {
                    if (Keyboard.Modifiers == ModifierKeys.None)
                    {
                        string messageText = textBox.Text;
                        if (textBox.LineCount >= 1)
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
                    else if (Keyboard.Modifiers == ModifierKeys.Shift || Keyboard.Modifiers == ModifierKeys.Control)
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
            System.Diagnostics.Debug.WriteLine("SendMessage called with text: [" + messageText + "]");
            messageText = messageText.Trim();

            // Check if the message starts with any module slash commands.
            Guid matchedModuleGuid = Guid.Empty;
            foreach (var module in ValidModules.Values)
            {
                List<string> commands = module.FetchCommands();
                foreach (string cmd in commands)
                {
                    if (messageText.StartsWith(cmd, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedModuleGuid = module.ModuleGuid;
                        // Rewrite the message to the standardized module command format.
                        //Try to inject the user's message for future terms.
                        messageText = $"UseModule=true;GUID={matchedModuleGuid};contentGUID=null;userMessage={messageText}";
                        System.Diagnostics.Debug.WriteLine("Slash command recognized. Rewritten message: " + messageText);
                        break;
                    }
                }
                if (matchedModuleGuid != Guid.Empty)
                    break;
            }

            // Use current profile if available; otherwise, use defaults.
            string legacyUsername = "User";
            BitmapImage img_instance = _defaultUserProfileImage;
            Guid senderUUID = Guid.Empty;

            if (_currentProfile != null)
            {
                if (!string.IsNullOrWhiteSpace(_currentProfile.Value.Username))
                {
                    legacyUsername = _currentProfile.Value.Username;
                    senderUUID = _currentProfile.Value.UserUUID;
                    UserProfile foundProfile = ValidProfiles.FirstOrDefault(x => x.Username == legacyUsername);
                    if (!string.IsNullOrWhiteSpace(foundProfile.Username))
                    {
                        img_instance = foundProfile.userProfileImage ?? _defaultUserProfileImage;
                    }
                }
            }

            // Create a new message with the (possibly rewritten) text.
            Message newMessage = new Message(legacyUsername, senderUUID, messageText, img_instance);

            // Process any embedded media URLs.
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
                    // Remove the URL from the message text.
                    newMessage.MessageString = newMessage.MessageString.Replace(word, "").Trim();
                }
            }

            if (embeddedUrls.Count > 0)
            {
                int downloadsCompleted = 0;
                foreach (string url in embeddedUrls)
                {
                    Uri uri = new Uri(url);
                    string fileName = System.IO.Path.GetFileName(uri.LocalPath);
                    string categoryPath = System.IO.Path.Combine("Cache", currentCategory, currentChannel);
                    System.IO.Directory.CreateDirectory(categoryPath);
                    string localCachePath = System.IO.Path.Combine(categoryPath, fileName);

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
                                SaveChannelData(currentCategory, currentChannel);
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
                SaveChannelData(currentCategory, currentChannel);
                RefreshChatHistory();
                MessageInput.Clear();

            }

            // Allow modules to check for interactions after sending.
            ScrollViewer chatScrollViewer = FindVisualParent<ScrollViewer>(ChatHistory);
            if (chatScrollViewer != null)
            {
                foreach (var module in ValidModules.Values)
                {
                    //module.CheckInteraction(messageText, chatScrollViewer);
                }
            }
        }



        // Helper to locate a visual parent.
        private static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        // ------------------ UI EVENT HELPERS ------------------
        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            // Uncomment and modify if you wish to toggle the sidebar.
            // Sidebar.Visibility = Sidebar.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
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

                // Use default username "User" and default UUID if no profile is set.
                Message newMessage = new Message("User", Guid.Empty, "", _defaultUserProfileImage);
                HandleMedia(newMessage, selectedFilePath, false);
                categoryData[currentCategory][currentChannel].Add(newMessage);
                RefreshChatHistory();
                SaveChannelData(currentCategory, currentChannel);
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
                if (categoryData[currentCategory][currentChannel].Count == 0 || !string.IsNullOrWhiteSpace(MessageInput.Text))
                {
                    categoryData[currentCategory][currentChannel].Last().Attachments.Add(media);
                }
                else
                {
                    Message newMessage = new Message("User", Guid.Empty, media.LocalCachePath, _defaultUserProfileImage);
                    newMessage.Attachments.Add(media);
                    categoryData[currentCategory][currentChannel].Add(newMessage);
                }
                RefreshChatHistory();
            }
        }

        // When the search icon is clicked, show the search panel and hide the search button.
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchButton.Visibility = Visibility.Collapsed;
            SearchPanel.Visibility = Visibility.Visible;
            SearchTextBox.Focus();
        }

        // When the cancel ("X") button is clicked, clear the search, hide the search panel, restore the search button, and refresh chat.
        private void CancelSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = string.Empty;
            SearchPanel.Visibility = Visibility.Collapsed;
            SearchButton.Visibility = Visibility.Visible;
            RefreshChatHistory(); // Show all messages again.
        }

        // When the user presses Enter in the search box, trigger the search.
        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RefreshChatHistory(); // This will now filter messages if there's search text.
            }
        }
        

        public void RefreshChatHistory()
        {
            ChatHistory.Children.Clear();

            if (categoryData.ContainsKey(currentCategory) && categoryData[currentCategory].ContainsKey(currentChannel))
            {
                List<Message> messages = categoryData[currentCategory][currentChannel];

                // If search is active, filter messages.
                if (SearchPanel.Visibility == Visibility.Visible && !string.IsNullOrWhiteSpace(SearchTextBox.Text))
                {
                    string searchTerm = SearchTextBox.Text;
                    messages = messages.Where(m => m.MessageString.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
                }

                for (int i = 0; i < messages.Count; i++)
                {
                    Message msg = messages[i];

                    // Detect module command messages.
                    if (msg.MessageString.StartsWith("UseModule=true;GUID="))
                    {
                        // Expected format:
                        // UseModule=true;GUID=<moduleGUID>;contentGUID=<contentGUID>
                        string[] parts = msg.MessageString.Split(';');
                        if (parts.Length >= 3)
                        {
                            string modulePart = parts[1];    // e.g., "GUID=A1111111-1111-1111-1111-111111111111"
                            string contentPart = parts[2];   // e.g., "contentGUID=null" or "contentGUID=<someGUID>"
                            string moduleGuidStr = modulePart.Substring("GUID=".Length);
                            string contentGuidStr = contentPart.Substring("contentGUID=".Length);
                            string userMessagePart = "null";

                            //Assign it locally so I can use it here.
                            Guid newContentGuid = Guid.Empty;

                            //Try to extract the user message.
                            if (parts.Length >= 4)
                            {
                                userMessagePart = parts[3];
                            }

                            if (Guid.TryParse(moduleGuidStr, out Guid moduleGuid))
                            {
                                if (ValidModules.TryGetValue(moduleGuid, out IModule module))
                                {
                                    ScrollViewer chatScrollViewer = FindVisualParent<ScrollViewer>(ChatHistory);
                                    if (chatScrollViewer != null)
                                    {
                                        if (contentGuidStr.Equals("null", StringComparison.OrdinalIgnoreCase))
                                        {
                                            // No content created yet. Create new content.

                                            if (userMessagePart == "null")
                                            {
                                               
                                                newContentGuid = module.InjectContent(chatScrollViewer, Guid.Empty);
                                            }
                                            else
                                            {
                                                //If the userMessage part is anything but "null" assuming 
                                                //That "null" is the initialized state always without the message part
                                                //being greater than 3 then well we do this
                                                //and this should IN THEORY... allow the llm to run MAYBE?
                                                newContentGuid = module.InjectContent(chatScrollViewer, Guid.Empty, userMessagePart);

                                                if (!string.IsNullOrWhiteSpace(ChatCommand.lastpromptresult))
                                                {
                                                    msg.MessageString =
                                                        $"UseModule=true;GUID={moduleGuid};contentGUID={newContentGuid}\n\n" +
                                                        $"[BOT]\n{ChatCommand.lastpromptresult}";
                                                }
                                                else
                                                {
                                                    msg.MessageString =
                                                        $"UseModule=true;GUID={moduleGuid};contentGUID={newContentGuid}";
                                                }
                                            }

                                               
                                            // Update the message with the new content GUID.
                                            //msg.MessageString = $"UseModule=true;GUID={moduleGuid};contentGUID={newContentGuid}";
                                            messages[i] = msg; // Update in-memory.
                                            System.Diagnostics.Debug.WriteLine("Updated message with new content GUID: " + msg.MessageString);
                                            SaveChannelData(currentCategory, currentChannel);
                                        }
                                        else
                                        {
                                            // Re-inject existing content.
                                            if (Guid.TryParse(contentGuidStr, out Guid existingContentGuid))
                                            {
                                                module.InjectContent(chatScrollViewer, existingContentGuid);
                                                System.Diagnostics.Debug.WriteLine("Re-injected content with existing GUID: " + existingContentGuid);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        // Skip normal rendering for module command messages.
                        continue;
                    }

                    // Otherwise, render the message normally.
                    Grid messageGrid = new Grid { Margin = new Thickness(5) };
                    messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    messageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    messageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // Title bar.
                    Grid titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(54, 57, 63)) };
                    var matchingProfile = ValidProfiles.FirstOrDefault(p => p.UserUUID == msg.MessageSenderUUID);
                    string displayUsername = msg.MessageSender;
                    if (!string.IsNullOrWhiteSpace(matchingProfile.Username))
                    {
                        displayUsername = matchingProfile.Username;
                    }
                    TextBlock userLabel = new TextBlock
                    {
                        Text = displayUsername,
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

                    // Profile picture.
                    BitmapImage profileImageToUse = _defaultUserProfileImage;
                    if (msg.SenderProfileImage != null)
                    {
                        profileImageToUse = msg.SenderProfileImage;
                    }
                    else if (!string.IsNullOrWhiteSpace(matchingProfile.Username) && matchingProfile.userProfileImage != null)
                    {
                        profileImageToUse = matchingProfile.userProfileImage;
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

                    // Message content.
                    TextBlock messageText = new TextBlock
                    {
                        Margin = new Thickness(5),
                        Width = 450,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Left
                    };
                    messageText.Inlines.Add(ParseFormattedText(msg.MessageString));
                    Grid.SetRow(messageText, 1);
                    Grid.SetColumn(messageText, 1);
                    messageGrid.Children.Add(messageText);

                    // Embedded images.
                    if (msg.Attachments.Count > 0)
                    {
                        foreach (var attachment in msg.Attachments)
                        {
                            try
                            {
                                string imagePathToUse = attachment.OriginalUri ?? attachment.LocalCachePath;
                                BitmapImage embeddedImage;
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
                                MessageBox.Show($"Error loading embedded image: {ex.Message} Path: {attachment.OriginalUri ?? attachment.LocalCachePath}",
                                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }

                    messageGrid.Background = new SolidColorBrush(Color.FromRgb(47, 49, 54));
                    ChatHistory.Children.Add(messageGrid);
                }
            }

            Dispatcher.BeginInvoke((Action)(() =>
            {
                ScrollViewer sv = FindVisualParent<ScrollViewer>(ChatHistory);
                if (sv != null)
                    sv.ScrollToEnd();
            }));
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

        // ------------------ INLINE FORMATTING PARSER ------------------
        private Inline ParseFormattedText(string text)
        {
            Span span = new Span();
            bool bold = false, italic = false, underline = false, crossout = false;
            Brush currentBrush = new SolidColorBrush(Color.FromRgb(220, 221, 225));
            StringBuilder currentRunText = new StringBuilder();
            int i = 0, len = text.Length;

            void FlushCurrentText()
            {
                if (currentRunText.Length > 0)
                {
                    string runText = currentRunText.ToString();
                    TextDecorationCollection decorations = new TextDecorationCollection();
                    if (underline) decorations.Add(TextDecorations.Underline[0]);
                    if (crossout) decorations.Add(TextDecorations.Strikethrough[0]);
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

            while (i < len)
            {
                if (i <= len - 2 && text.Substring(i, 2) == "**")
                {
                    FlushCurrentText();
                    bold = !bold;
                    i += 2;
                    continue;
                }
                else if (i <= len - 2 && text.Substring(i, 2) == "__")
                {
                    FlushCurrentText();
                    underline = !underline;
                    i += 2;
                    continue;
                }
                else if (i <= len - 2 && text.Substring(i, 2) == "--")
                {
                    FlushCurrentText();
                    crossout = !crossout;
                    i += 2;
                    continue;
                }
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
                    currentRunText.Append(text[i]);
                    i++;
                    continue;
                }
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

        private IEnumerable<Inline> ParseHyperlinks(string text, Brush currentBrush, FontWeight fontWeight, FontStyle fontStyle, TextDecorationCollection decorations)
        {
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
                    Foreground = Brushes.LightBlue,
                    FontWeight = fontWeight,
                    FontStyle = fontStyle,
                    TextDecorations = decorations
                };
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

        // ------------------ MISSING FUNCTIONS FROM ORIGINAL CODE ------------------


        private void ClearPlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Text == "Enter category name")
            {
                textBox.Text = "";
                textBox.Foreground = Brushes.White;
            }
        }

        private void RestorePlaceholderText(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = "Enter category name";
                textBox.Foreground = Brushes.Gray;
            }
        }

        public string Serialize_Profiles(List<UserProfile> profiles_in)
        {
            string profiles_string = JsonSerializer.Serialize(profiles_in);
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
                        // If the profile image is not loaded and we have a valid path, load the BitmapImage.
                        if (profile.userProfileImage == null && !string.IsNullOrEmpty(profile.ProfileImagePath))
                        {
                            try
                            {
                                BitmapImage bmp = new BitmapImage();
                                bmp.BeginInit();
                                bmp.UriSource = new Uri(profile.ProfileImagePath, UriKind.RelativeOrAbsolute);
                                bmp.CacheOption = BitmapCacheOption.OnLoad; // load synchronously
                                bmp.EndInit();
                                bmp.Freeze(); // Optional: Freeze for cross-thread usage
                                profile.userProfileImage = bmp;
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

    }
}
