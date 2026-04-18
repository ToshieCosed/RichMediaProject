
using OllamaConsoleClient;
using System;
using System.Threading.Tasks;

namespace HelloBotExample
{
    public static class ChatCommand
    {
        public static string char_;
        public static string lastpromptresult = "none initiated";
        public static int turncount = 0;
        public static string loadedcharacter = "";
        public static string CurrentTopic = "";
        public static bool firstpass = false;

        public static void WriteOut(string inp)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine(inp);
            });
        }
        public static void dochat(string userstring)
        {

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine("💥 dochat entered on thread " + System.Threading.Thread.CurrentThread.ManagedThreadId);
            });

            WriteOut("dochat called with message: " + userstring);

            WriteOut("✅ 1. Command started");
            WriteOut("The first pass variable's value is " + firstpass); //first pass should be printed


            if (!firstpass)
            {
                //First pass should be in here
                WriteOut("✅ 2. Inside firstpass block");
                CharacterStructure.ReadCharacter("char.txt"); //Don't forget to copy her persona
                WriteOut("✅ 3. Character file read");

                char_ = CharacterStructure.getchardef();

                //defailt
                loadedcharacter = char_;
                WriteOut("✅ 4. Got char def");

                WriteOut("✅ 5. About to call Ollama...");

                //Ollama prompt hook
                //Figure this part out next
                lastpromptresult = Utility.SendPromptToOllama(char_);


                WriteOut("✅ 6. Ollama returned!");
                if (lastpromptresult != "")
                {
                    WriteOut("✅ 7. Prompt result not empty");
                    WriteOut(lastpromptresult + "\n");

                    CharacterStructure.chatcontext.Add("<Character>" + lastpromptresult);

                    WriteOut("✅ 8. Added to context");

                    CharacterStructure.SaveMemory();
                    WriteOut("✅ 9. Memory saved");
                }

                WriteOut("✅ 10. About to set firstpass to false");

               
                firstpass = false;

            }
                WriteOut("✅ 11. First pass = " + firstpass); //todo inject firstpass value



            //First pass set to true or skipped
            WriteOut("✅ 12. Past the firstpass block");
            string input = userstring; // ?? Correct

                try
                {
                string send = "";

                    //New routine which injects only a few pieces of chat history
                    int historyDepth = Math.Min(6, CharacterStructure.chatcontext.Count);
                    for (int i = CharacterStructure.chatcontext.Count - historyDepth; i < CharacterStructure.chatcontext.Count; i++)
                    {
                        send += CharacterStructure.chatcontext[i] + "\n";
                    }

                    //Check memory relevency
                    if (CharacterStructure.tagged_memories.Count != 0)
                    {
                        Utility.releventMemories.Clear();   // <-- critical fix
                        Utility.releventMemories = Utility.MemoryCheckAsync("<User>" + input, CharacterStructure.tagged_memories); //Added user to the input by memory check
                    }

                    string memoryBlock = "";

                    if (Utility.releventMemories.Count > 0)
                    {
                        //Refactored into Utility class call
                        memoryBlock = Utility.BuildRelevantMemoryBlock(CharacterStructure.tagged_memories, Utility.releventMemories);
                    }

                    WriteOut("Made it to prompt step for character and memory");
                    lastpromptresult = Utility.SendPromptToOllama("<System>" + loadedcharacter + "<History>" + send + "<HistoryEnd>" + memoryBlock + "<User>" + input);
                    CharacterStructure.chatcontext.Add("<User>" + input);

                    WriteOut(lastpromptresult);
                    CharacterStructure.chatcontext.Add("<Character>" + lastpromptresult);


                    //Every five turns memory is summarized from the last five entries (I hope)
                    turncount++;
                    CharacterStructure.SaveMemory();
                    Utility.releventMemories.Clear();


                }
                catch (Exception ex)
                {
                    WriteOut($"\nError: {ex.Message}\n");
                }


                if (turncount > 2)
                {

                    //Try to get the current topic using a simple prompt
                    try
                    {

                        string send = "";

                        int count = CharacterStructure.chatcontext.Count;

                    send = "Return a 3 sentence summaries of the context of this content. Do not think about it. WHEN a nametag prefixes the USER's input such as 'Phoenix:' or 'Rune:' you are to refer to user in the summary as 'The User Phoenix' or 'The User Rune' when mentioning them. Explicitly always mention a user if they are prefixed. If multiple users exist in the history you are shown, expand the summary beyond 3 sentences if needed to accurately and factually represent their statments and inputs." +
                            "Do not add narrative. Keep it to the facts plain and simple. You are a memory processing engine, and your" +
                            "task is to simply ensure that the content is summarized efficiently enough" +
                            "for an LLM with very low memory to be able to process if asked about their memories. Do not include " +
                            "this instruction as part of the memory summary. Include a KEYWORD=&keyword& result in the prompt output at the" +
                            "START of the summary as the first line. Make sure the keyword is relevent to the summary because it will be" +
                            "used as a keyvaluepair dictionary entry for a list of related summaries in the same category." +
                            "A rule to keep in mind, in the code i'm normalizing the capitalization of all keywords to lower case so upper and lower cased words will be the same." + "\n";


                    send += "HARD RULES For topics:" +
                        "Prioritize things said by <User> above <Character> for topic classification.\n" +
                        "Example: <Character> Oh Pokemon is cool do you like rainbow girl flowers colors \n" +
                        "<User> oh, Charizard is cool. I think it can be very rainbowy yeah.\n " +
                        "This should result in the topic logically being: Pokemon \n" +
                        "BIGGEST OVER RIDE YET, IF the user is asking for two things to be associated together, ie the word tomato, and a url \n" +
                        "DO INCLUDE THEM!!! For example Tomato=https://youtu.be/dQw4w9WgXcQ?list=RDdQw4w9WgXcQ so if the user asks hey what's tomato\n" +
                        "The character this is injected into will later be able to say 'oh yeah i remember tomato was '<url>' explicitly and to the facts\n";

                        //We should get way less repeat keys now
                        send = send + "You will also be given a list of keywords that are already defined, if no relevent ones exist in this list create a new one. For example fastrace and race_event are basically the same so choose only one. \n";
                        send += "Tagged memories: " + string.Join(", ", CharacterStructure.tagged_memories.Keys) + "\n";


                        for (int i = count - 5; i < count; i++)
                        {
                            send = send + CharacterStructure.chatcontext[i] + "\n";
                        }

                        //Moved to Utility :)
                        CurrentTopic = Utility.GetTopicAsync(send);

                        //await ctx.Channel.SendMessageAsync("<System> Current Topic Summarized So Far");
                        //await ctx.Channel.SendMessageAsync(Program.CurrentTopic);

                        Console.WriteLine("\n");

                        //Reset Count Memory to 0
                        turncount = 0;

                        //Inject the new summarized memory into the structure
                        //Refactored into Utility :)
                        CharacterStructure.tagged_memories = Utility.InjectNewMemory(CurrentTopic, CharacterStructure.tagged_memories);


                    }
                    catch (Exception ex)
                    {
                        WriteOut($"\nError: {ex.Message}\n");
                    }

                }

                //await ctx.Channel.SendMessageAsync(Program.lastpromptresult);
        }
    }
}