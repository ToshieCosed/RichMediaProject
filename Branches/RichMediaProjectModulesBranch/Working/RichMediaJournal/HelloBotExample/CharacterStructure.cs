using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OllamaConsoleClient
{
    public static class CharacterStructure
    {
        public static Dictionary<string, List<string>> tagged_memories = new Dictionary<string, List<string>>();
        public static List<string> chatcontext = new List<string>();

        private static string char_;
        private static string memoryPath;

        public static void ReadCharacter(string filename)
        {
            char_ = File.ReadAllText(filename);

            string nameOnly = Path.GetFileNameWithoutExtension(filename);
            memoryPath = "characters/" + nameOnly + "_memory.json";

            LoadMemory();
        }

        public static string getchardef()
        {
            return char_;
        }

        private static void LoadMemory()
        {
            if (!File.Exists(memoryPath))
            {
                tagged_memories = new Dictionary<string, List<string>>();
                chatcontext = new List<string>();
                return;
            }

            try
            {
                string json = File.ReadAllText(memoryPath);
                CharacterMemoryFile data = JsonSerializer.Deserialize<CharacterMemoryFile>(json);

                tagged_memories = data != null ? data.TaggedMemories : new Dictionary<string, List<string>>();
                chatcontext = data != null ? data.ChatContext : new List<string>();
            }
            catch
            {
                tagged_memories = new Dictionary<string, List<string>>();
                chatcontext = new List<string>();
            }
        }

        public static void SaveMemory()
        {
            CharacterMemoryFile data = new CharacterMemoryFile();
            data.TaggedMemories = tagged_memories;
            data.ChatContext = chatcontext;

            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            Directory.CreateDirectory("characters");
            File.WriteAllText(memoryPath, json);
        }
    }
}
