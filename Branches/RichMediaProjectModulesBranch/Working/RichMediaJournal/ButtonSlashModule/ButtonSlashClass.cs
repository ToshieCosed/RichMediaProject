using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using ModuleContract;

namespace ButtonSlashModule
{
    public class ButtonSlashClass : IModule
    {
        // Fixed GUID for this module.
        public Guid ModuleGuid { get; } = new Guid("A1111111-1111-1111-1111-111111111111");

        // A counter for created buttons.
        private int buttonCount = 0;

        // Dictionary to hold module-generated content (each content is a Panel, for example).
        private Dictionary<Guid, Panel> ContentDictionary = new Dictionary<Guid, Panel>();

        /// <summary>
        /// Injects content into the provided ScrollViewer.
        /// If contentGuid is Guid.Empty, new content is created; otherwise, the existing content is re-injected.
        /// Returns the GUID for the content.
        /// </summary>
        public Guid InjectContent(ScrollViewer scrollView, Guid contentGuid)
        {

            if (contentGuid == Guid.Empty)
            {
                // Create a new content container.

                Guid newContentGuid = Guid.NewGuid();
                StackPanel container = new StackPanel();

                // Create a new button.
                buttonCount++;
                Button btn = new Button
                {
                    Content = $"Module Button #{buttonCount}",
                    Margin = new Thickness(5),
                    Tag = buttonCount
                };

                // Attach an event handler.
                btn.Click += (sender, e) =>
                {
                    MessageBox.Show($"This is button number {buttonCount}");
                };

                // Add the button to the container.
                container.Children.Add(btn);

                // Save the container in our dictionary.
                ContentDictionary[newContentGuid] = container;

                // Inject the container into the provided ScrollViewer.
                if (scrollView.Content is Panel panel)
                {
                    panel.Children.Add(container);
                    System.Diagnostics.Debug.WriteLine("Injected new content with GUID: " + newContentGuid);
                }
                else
                {
                    MessageBox.Show("ScrollViewer's content is not a Panel!");
                }

                return newContentGuid;

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

        public void CheckInteractionAsync(string messageContent, ScrollViewer scrollView)
        {
            // If the message contains "/inject", we create new content.
            if (messageContent.Contains("/inject"))
            {
                Guid newContentGuid = InjectContent(scrollView, Guid.Empty);
                System.Diagnostics.Debug.WriteLine("CheckInteraction injected content with GUID: " + newContentGuid);
            }
        }

        public bool SearchSelf(string input)
        {
            return input.IndexOf("button", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public List<string> FetchCommands()
        {
            return new List<string> { "/inject" };
        }
    }
}
