
using System.Windows.Controls;
using System.Windows;
using System.Net;
using ModuleContract;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Net.Mail;
using System.Text.Json.Serialization;
using System.IO;
using System.Text.Json;
using System.Text;
using System.Windows.Documents;
using System.Text.RegularExpressions;

namespace HelloBotExample
{

    public class HelloBot : IModule
    {
        // Fixed GUID for this module.
        public Guid ModuleGuid { get; } = new Guid("A1111111-1111-1111-1111-111111111222");

        // Dictionary to hold module-generated content (each content is a Panel, for example).
        private Dictionary<Guid, Panel> ContentDictionary = new Dictionary<Guid, Panel>();

        public UserProfile? _currentProfile;
        public List<UserProfile> ValidProfiles = new List<UserProfile>();
        private UserProfile currentProfile;
        private BitmapImage defaultUserProfileImage;


        public void OnLoaded()
        {
            CreateBotProfile();
        }

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

        /// <summary>
        /// Injects content into the provided ScrollViewer.
        /// If contentGuid is Guid.Empty, new content is created; otherwise, the existing content is re-injected.
        /// Returns the GUID for the content.
        /// </summary>
        /// 

        public struct Message
        {
            public string MessageString { get; set; }
            public DateTime MessageTimeStamp { get; set; }
            public string MessageSender { get; set; } // legacy username
            public Guid MessageSenderUUID { get; set; } // new UUID for sender

            [JsonIgnore]
            public BitmapImage SenderProfileImage { get; set; }

            [JsonIgnore]
            public UserProfile MessageSenderProfile { get; set; }

            public Message(string sender, Guid senderUUID, string message, BitmapImage profileImage = null)
            {
                MessageSender = sender;
                MessageSenderUUID = senderUUID;
                MessageString = message;
                MessageTimeStamp = DateTime.Now;
                SenderProfileImage = profileImage;
                MessageSenderProfile = new UserProfile();
            }
        }

        public struct UserProfile
        {
            public string Username { get; set; }
            public string ProfileImagePath { get; set; }
            public BitmapImage userProfileImage { get; set; }
            public Guid UserUUID { get; set; } // new UUID property

            // Constructor now accepts an optional UUID (defaults to Guid.Empty)
            public UserProfile(string username, string profileImagePath, bool isDefault, BitmapImage userImage, Guid userUUID = default)
            {
                Username = username;
                ProfileImagePath = profileImagePath;
                userProfileImage = userImage;
                UserUUID = userUUID;
            }
        }




        /*
        private void CreateBotProfile()
        {
            if (!string.IsNullOrEmpty("ModuleResources/BotProfiles")) // Check if the path is valid
            {
                try
                {
                    // 1. Create the Profiles directory if it doesn't exist
                    string profilesDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModuleResources/BotProfiles");
                    if (!Directory.Exists(profilesDirectory))
                    {
                        Directory.CreateDirectory(profilesDirectory);
                    }

                    // 2. Create a copy of the image in the Profiles directory
                    string defaultFilePath = System.IO.Path.Combine(profilesDirectory, "DefaultProfile.jpg");

                    // 3. Load the BitmapImage from the default file path
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(defaultFilePath); // Use the new file path!
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();

                    // Store the relative path
                    string relativePath = GetRelativePath(defaultFilePath); // Helper function (see below)

                    // 4. Create the UserProfile, using the default file path
                    UserProfile userProfile = new UserProfile("HelloBot", defaultFilePath, false, bitmapImage, Guid.NewGuid());

                    userProfile.ProfileImagePath = relativePath; // Store the new file path
                    userProfile.userProfileImage = bitmapImage;

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
        */

        private void CreateBotProfile()
        {
            try
            {
                // Create the Profiles directory if it doesn't exist.
                string profilesDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ModuleResources/BotProfiles");
                if (!Directory.Exists(profilesDirectory))
                {
                    Directory.CreateDirectory(profilesDirectory);
                }

                // Construct the full file path for the default profile image.
                string defaultFilePath = System.IO.Path.Combine(profilesDirectory, "DefaultProfile.jpg");

                // Check if the file exists.
                if (!File.Exists(defaultFilePath))
                {
                    MessageBox.Show($"Default profile image not found at: {defaultFilePath}");
                    return;
                }

                // Load the BitmapImage from the file.
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = new Uri(defaultFilePath, UriKind.Absolute);
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                // Optionally, get a relative path if needed.
                string relativePath = GetRelativePath(defaultFilePath);

                // Create the UserProfile.
                UserProfile userProfile = new UserProfile("HelloBot", relativePath, false, bitmapImage, Guid.NewGuid());

                // Now assign the loaded image to your fields.
                defaultUserProfileImage = bitmapImage;
                _currentProfile = userProfile;

                // (Optionally, add this profile to your ValidProfiles list.)
                ValidProfiles.Add(userProfile);

                System.Diagnostics.Debug.WriteLine("Bot profile image loaded successfully from: " + defaultFilePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading image: {ex.Message}");
            }
        }
        private void LoadExistingProfiles()
        {
            for (int i = 0; i < ValidProfiles.Count; i++)
            {
                UserProfile profile = ValidProfiles[i];

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
                    ValidProfiles[i] = profile;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image for {profile.Username}: {ex.Message}");
                    ValidProfiles[i] = profile;
                }

                // Set the current profile if not already set.
                if (!currentProfile.Equals(null))
                {
                    currentProfile = profile;
                    defaultUserProfileImage = profile.userProfileImage;

                }
            }
        }

        private void SetProfile(UserProfile profile)
        {
            // Set the current profile if not already set.
            if (!currentProfile.Equals(null))
            {
                currentProfile = profile;
                defaultUserProfileImage = profile.userProfileImage;

            }
        }

        private string GetRelativePath(string fullPath)
        {
            string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            return System.IO.Path.GetRelativePath(currentDirectory, fullPath);
        }

        public Guid InjectContent(ScrollViewer scrollView, Guid contentGuid)
        {
            StackPanel container = new StackPanel();
            //Message msg = new Message();
            //msg.MessageString = "[Red] **I'm really cool!**";
            Message msg = new Message("HelloBot", Guid.NewGuid(), "[Red] **I'm really cool!**", defaultUserProfileImage);


            if (contentGuid == Guid.Empty)
            {
                Grid messageGrid = new Grid { Margin = new Thickness(5) };
                messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                messageGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                messageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                messageGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // Title bar.
                Grid titleBar = new Grid { Background = new SolidColorBrush(Color.FromRgb(54, 57, 63)) };

                string displayUsername = "Hello Bot";

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
                BitmapImage profileImageToUse = defaultUserProfileImage;

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

                Guid newguid = Guid.NewGuid();
                
                ContentDictionary.Add(newguid, messageGrid);

                if (scrollView.Content is Panel panel)
                {
                    panel.Children.Add(container);
                    container.Children.Add(messageGrid);
                    System.Diagnostics.Debug.WriteLine("Injected new HelloBot content with GUID: " + newguid);
                }
                
                
            }
            else
            {

                // Reuse existing content.
                if (ContentDictionary.TryGetValue(contentGuid, out Panel existingContent))
                {
                    if (scrollView.Content is Panel panel)
                    {
                        panel.Children.Add(existingContent);
                        System.Diagnostics.Debug.WriteLine("Re-injected existing content with GUID: " + contentGuid);

                    }
                    else
                    {
                        MessageBox.Show("ScrollViewer's content is not a Panel!");
                    }
                    return contentGuid;
                }
                /*
                else
                {
                    //BAD recursive call that chat gpt created.. we ALREADY create new content at the top if GUID is empty.
                        //So even if the dictionary doesn't return an entry we don't need to create a second time...
                    // If not found, create new content.
                    //return InjectContent(scrollView, Guid.Empty);
                }
                */
            }
            return Guid.Empty; //Period just don't re inject

        }

        public void CheckInteraction(string messageContent, ScrollViewer scrollView)
        {
            // If the message contains "/inject", we create new content.
            if (messageContent.Contains("/hello"))
            {
                Guid newContentGuid = InjectContent(scrollView, Guid.Empty);
                System.Diagnostics.Debug.WriteLine("CheckInteraction injected content with GUID: " + newContentGuid);
            }
        }

        public bool SearchSelf(string input)
        {
            //return input.IndexOf("button", StringComparison.OrdinalIgnoreCase) >= 0;
            return false;
        }

        public List<string> FetchCommands()
        {
            return new List<string> { "/hello" };
        }

        /*
         * 
         *  // Otherwise, render the message normally.
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
        */


        public string Serialize_Profiles(List<UserProfile> profiles_in)
        {
            string profiles_string = JsonSerializer.Serialize(profiles_in);
            //Console.WriteLine("Test");
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
    }
}
