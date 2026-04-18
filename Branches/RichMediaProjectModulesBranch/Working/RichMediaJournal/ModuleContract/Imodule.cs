using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ModuleContract
{

    public interface IModule
    {
        Guid ModuleGuid { get; }
        /// <summary>
        /// Injects content into the given ScrollViewer.
        /// If the passed contentGuid is Guid.Empty (i.e. “null”), the module creates new content and returns its GUID.
        /// </summary>
        /// <param name="scrollView">The ScrollViewer that holds the UI panel.</param>
        /// <param name="contentGuid">Existing content GUID or Guid.Empty if none.</param>
        /// <returns>The GUID of the created content.</returns>
        Guid InjectContent(ScrollViewer scrollView, Guid contentGuid);
        Guid InjectContent(ScrollViewer scrollView, Guid contentGuid, string usercontent);

        void CheckInteractionAsync(string messageContent, ScrollViewer scrollView);
        bool SearchSelf(string input);

        /// <summary>
        /// Returns the list of slash commands (e.g. "/inject") that this module provides.
        /// </summary>
        List<string> FetchCommands();

        void OnLoaded() { }

        void CheckInteraction(string messageText, ScrollViewer chatScrollViewer);
    }

}

