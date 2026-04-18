using System.Collections.Generic;

namespace OllamaConsoleClient
{
    public class CharacterMemoryFile
    {
        public Dictionary<string, List<string>> TaggedMemories { get; set; }
        public List<string> ChatContext { get; set; }

        public CharacterMemoryFile()
        {
            TaggedMemories = new Dictionary<string, List<string>>();
            ChatContext = new List<string>();
        }
    }
}
