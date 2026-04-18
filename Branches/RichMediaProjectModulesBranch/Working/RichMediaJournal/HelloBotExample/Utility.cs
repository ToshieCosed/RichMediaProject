using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace OllamaConsoleClient
{
    public static class Utility {

        public static List<string> releventMemories = new List<string>();
        // Ollama default URL and port
        private static readonly string OllamaUrl = "http://localhost:11434";

        private static readonly string modelstring  = "gemma3:12b";

        /*
        public static string ExtractKeyword(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return null;

            // Get first line only
            var firstLine = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];

            const string prefix = "KEYWORD=";
            if (!firstLine.StartsWith(prefix))
                return null;

            return firstLine.Substring(prefix.Length).Trim();
        }
        */

        //Temporary pasted debug version of above function
        public static string ExtractKeyword(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
            {
                Console.WriteLine("DEBUG: response was null or empty");
                return null;
            }

            var lines = response.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"DEBUG: Total lines = {lines.Length}");
            Console.WriteLine($"DEBUG: First line = '{lines[0]}'");
            Console.WriteLine($"DEBUG: First line starts with 'KEYWORD='? {lines[0].StartsWith("KEYWORD=")}");

            var firstLine = lines[0].Trim(); // <-- ADD .Trim() HERE

            const string prefix = "KEYWORD=";
            if (!firstLine.StartsWith(prefix))
            {
                Console.WriteLine($"DEBUG: First line doesn't start with '{prefix}'");
                return null;
            }

            string result = firstLine.Substring(prefix.Length).Trim();
            Console.WriteLine($"DEBUG: Extracted keyword = '{result}'");
            return result;
        }

        public static List<string> ExtractKeywordsFromMemoryCheck(string response)
        {
            if (string.IsNullOrWhiteSpace(response))
                return new List<string>();

            var firstLine = response
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0]
                .Trim();

            if (!firstLine.ToLowerInvariant().StartsWith("keywords="))
                return new List<string>();

            string keywordPart = firstLine.Substring("keywords=".Length);

            if (keywordPart.ToLowerInvariant() == "none")
                return new List<string>();

            return keywordPart
                .Split(',')
                .Select(k => k.Trim().ToLowerInvariant())
                .Where(k => k.Length > 0)
                .ToList();
        }

        public static string BuildRelevantMemoryBlock(Dictionary<string, List<string>> tagged_memories, List<string> releventMemories)
        {
            if (releventMemories == null || releventMemories.Count == 0)
                return "";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("\n[Previous memories on this topic]");

            foreach (string tag in releventMemories)
            {
                if (tagged_memories.ContainsKey(tag))
                {
                    foreach (string mem in tagged_memories[tag])
                    {
                        sb.AppendLine(mem.Trim());
                    }
                }
            }

            sb.AppendLine("[End of previous memories]\n");

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("[InjectedMemoryBlock]\n" + sb.ToString());

            return sb.ToString();
        }

        public static List<string> SplitByNewlines(string text)
        {
            return text
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .ToList();
        }

        public static Dictionary<string, List<string>> InjectNewMemory(string topic_summary, Dictionary<string, List<string>> tagged_memories)
        {
            string Keyword = Utility.ExtractKeyword(topic_summary);

            //Keyword = Keyword.ToLower(); -- Corrected for working across multiple systems?

            // ADD THIS CHECK
            if (string.IsNullOrWhiteSpace(Keyword))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Could not extract keyword from summary:");
                Console.WriteLine(topic_summary);
                return tagged_memories; // Skip this memory injection
            }

            Keyword = Keyword.Trim().ToLowerInvariant();

            List<string> lines = Utility.SplitByNewlines(topic_summary);
            string currentmemory = "";

            int strcount = 0;
            foreach (string s in lines)
            {
                //Wasn't actually ignoring keyword, this simple patch fixes that by ensuring only strings after the 1st pass are added.
                if (strcount > 0)
                {
                    currentmemory += s + "\n"; //re add the new line ignore the keyword line of the memory;
                }
                strcount++;
            }

            //This is temporary because we need to check for 
            // other tagged memories in the same keytag first
            List<string> processed_memories = new List<string>();
            processed_memories.Add(currentmemory);

            //Keyword is for now, not collidable.
            if (!tagged_memories.ContainsKey(Keyword))
            {
                tagged_memories.Add(Keyword, processed_memories);
            }
            else
            {
                //handle collision
                List<string> keyword_memories = tagged_memories[Keyword];
                keyword_memories.Add(currentmemory);
                //Remove entry and re-add new instance of the memories with the appended memory
                tagged_memories.Remove(Keyword);
                tagged_memories.Add(Keyword, keyword_memories);
            }

            Console.ForegroundColor = ConsoleColor.DarkGreen;

            //Every single time that I ask it to tag a memory with a keyword, I need to also provide it
            //With a list of tags that it has previously created so it can be tag-aware.

            Console.WriteLine("...memory injected");
            //Return the tagged memories structure
            CharacterStructure.SaveMemory();
            return tagged_memories;

        }

        public static List<string> MemoryCheckAsync(string inp, Dictionary<string, List<string>> tagged_memories)
        {
            // Build deterministic memory classification instructions
            inp = inp + "| #Instruction |\n" +
                  "You are a deterministic memory classification system.\n" +
                  "You do NOT infer, speculate, or associate concepts.\n\n" +
                  "Only match a keyword if the user's input very closely matches the concept, or branching concepts the keyword could refer to. Ie 'magic_painting', 'magical paintbrush'\n" +
                  "Do NOT match based on:\n" +
                  "- Aesthetic description\n" +
                  "- Adjectives or color words\n" +
                  "- Vibes, tone, or symbolism\n\n" +
                  "IMPORTANT DISTINCTION:\n" +
                  "- 'rainbow' as a color or appearance ≠ rainbow_magic\n" +
                  "- 'rainbow magic' must involve explicit magical effects, spells, powers, or mechanics\n\n" +
                  "If no keywords clearly apply, respond with:\n" +
                  "KEYWORDS=none\n\n" +
                  "On the FIRST LINE ONLY, respond in the format:\n" +
                  "KEYWORDS=tag1,tag2\n" +
                  "rainbow_magic: Explicit magical systems, spells, supernatural abilities, or mechanics involving rainbows\r\n" +
                  "rainbow: Visual coloration or aesthetic only\r\n" +
                  "fox: Literal foxes or fox traits\r\n" +
                  "rainbow_fox: Fox characters whose identity is defined by rainbow coloration, not magic\r\n" +
                  "If both a general and a specific keyword could apply, ONLY return the most specific one.\r\n" +
                  "ALWAYS use the same formatting, identify things like 'magical_paintbrush' and 'magic paintbrush' as being in the same tag, if one exists use that instead of creating a new one.\r\n" +
                  "Do not use spaces, use single word tags or always use an underscore, keep formatting persistent. If you see a tag such as 'magical_tool' and the topic is 'magical_screwdriver', then logically put 'magical_screwdriver' into the 'magical_tool' tag and don't create a new 'magical_screwdriver' tag!\n" +
                  "Below is a list of tags, you are to evaluate the user's input above this instruction that begins before the instruction hashtag, and see if it could possibly at all in any reality, match any potential tags. A bit of fuzzy matching is ok but try to keep things logically categorical. \n" +
                  "Lastly, if the user's input includes or is about a more generic group of topics, return every tag that could possibly relate ie pokemon cards, charizard, shiny pokemon, and so on \r\n" +
                  "And one final RULE!! If the user asks what you remember about the conversation, include EVERY passed in tag in the result, so it can scan all memories even if the tags DONT match. This is the one exception\n";

            // Add potential tags to the instruction
            inp += "Potential Tags: " + string.Join(", ", tagged_memories.Keys) + "\n";

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5); // Increase timeout

                var payload = new
                {
                    model = modelstring,
                    prompt = inp,
                    stream = false
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");

                // Synchronous HTTP call
                HttpResponseMessage response = client.PostAsync($"{OllamaUrl}/api/generate", content).Result;
                response.EnsureSuccessStatusCode();

                // Synchronously read response
                string result = response.Content.ReadAsStringAsync().Result;
                var jsonResponse = JObject.Parse(result);

                string responseText = jsonResponse["response"]?.ToString();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[SystemMemoryConfirmation] " + responseText);

                // Extract keywords into global list
                releventMemories = Utility.ExtractKeywordsFromMemoryCheck(responseText);
                return releventMemories;
            }
        }


        public static string GetTopicAsync(string inp)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5); // Increase timeout for longer responses

                // Build the request body for Ollama API
                var payload = new
                {
                    model = modelstring, // Your installed model
                    prompt = inp,
                    stream = false
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");

                // Synchronous HTTP call
                HttpResponseMessage response = client.PostAsync($"{OllamaUrl}/api/generate", content).Result;
                response.EnsureSuccessStatusCode();

                // Synchronously read response
                string result = response.Content.ReadAsStringAsync().Result;
                var jsonResponse = JObject.Parse(result);

                string responseText = jsonResponse["response"]?.ToString();
                return responseText; // Return exactly the same as before
            }
        }


        public static string SendPromptToOllama(string prompt)
        {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5); // Increase timeout

                var payload = new
                {
                    model = modelstring,
                    prompt = prompt,
                    stream = false
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");

                HttpResponseMessage response = client.PostAsync($"{OllamaUrl}/api/generate", content).Result;
                response.EnsureSuccessStatusCode();

                string result = response.Content.ReadAsStringAsync().Result;
                var jsonResponse = JObject.Parse(result);

                string responseText = jsonResponse["response"]?.ToString();
                return responseText;
            }
        }


    }
}
