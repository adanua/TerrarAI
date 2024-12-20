using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.Chat.Commands;
using Terraria.Chat;
using Terraria.UI.Chat;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using System.Text.Json.Nodes;
using GroqApiLibrary;
using TerrarAI.Common.Configs;
using TerrarAI.Common;
using Newtonsoft.Json;

namespace TerrarAI.Common.Commands
{
    internal class TestAICommand : ModCommand
    {
        public override string Command => "test";
        public override string Description => "Get a quick response based off the Terraria wikis on a query using AI.";
        public override string Usage => "/test <query>";

        public override CommandType Type => CommandType.Chat;

        public override async void Action(CommandCaller caller, string input, string[] args)
        {
            try
            {
                var config = ModContent.GetInstance<TerrarAIModConfig>();
                bool hasApiKey = !string.IsNullOrEmpty(config.ApiKey);
                float temperature = config.Temperature;
                int maxTokens = config.MaxTokens;
                string model = config.Model;

                var MediaWikiClient = new TerrariaMediaWiki();

                if (!hasApiKey)
                {
                    caller.Reply("[TerrarAI] No API key found. Please set an API key in the mod configuration.", Color.Red);
                    return;
                }

                var groqClient = new GroqApiClient(config.ApiKey);

                if (args.Length <= 0)
                {
                    caller.Reply(String.Concat("[TerrarAI] ", Usage), Color.MediumVioletRed);
                    if (!string.IsNullOrEmpty(Description))
                        caller.Reply(String.Concat("[TerrarAI] ", Description), Color.Red);

                    return;
                }
                else
                {
                    String query = String.Join(" ", args);
                    var queryRequest = Prompts.buildGroqRequest(query, Prompts.SearchMediaWikiPrompt, config).Result;
                    caller.Reply("[TerrarAI] Querying AI...", Color.Yellow);
                    var queriesResult = await groqClient.CreateChatCompletionAsync(queryRequest);

                    // caller.Reply($"TerrarAI:query | {query}");

                    var items = queriesResult?["choices"]?[0]?["message"]?["content"]?.ToString().Split(",");

                    // caller.Reply($"TerrarAI:items | {queriesResult?["choices"]?[0]?["message"]?["content"]?.ToString()}");

                    caller.Reply("[TerrarAI] Found the following items to search on the wiki: " + queriesResult?["choices"]?[0]?["message"]?["content"]?.ToString());
                    var collectedDictionary = new Dictionary<int, string>();
                    var totalCollectedItems = "";
                    for (var itemIndex = 0; itemIndex < items.Length; itemIndex++)
                    {
                        string item = items[itemIndex].Trim();
                        caller.Reply("[TerrarAI] Searching for: " + item, Color.Yellow);
                        var searchResults = await MediaWikiClient.SearchWiki(item);
                        var collectedResults = "";
                        foreach (var result in searchResults.EnumerateArray())
                        {
                            string title = result.GetProperty("title").GetString();
                            int pageID = result.GetProperty("pageid").GetInt32();
                            collectedResults += $"{title} (ID: {pageID}), ";
                            if (collectedDictionary.ContainsKey(pageID))
                            {
                                collectedDictionary[pageID] = title;
                            }
                            else
                            {
                                collectedDictionary.TryAdd(pageID, title);
                            }
                        }
                        caller.Reply("[TerrarAI] Search results for " + item + ": " + collectedResults, Color.Yellow);
                        totalCollectedItems += collectedResults;
                    }
                    caller.Reply("[TerrarAI] Picking the most relevant page(s)", Color.Yellow);

                    var rerankResult = Prompts.buildStructuredGroqRequest(Prompts.PagesRerankPrompt, 
                        "User Query: '" + query + "'\nPages found: " + totalCollectedItems, 
                        @"{
                            ""pageIDs"": [
                                ""string | The page ID of the page.""
                            ]
                        }",
                        config).Result;

                    //var rerankResult = await groqClient.CreateChatCompletionAsync(rerankRequest);

                    caller.Reply($"TerrarAI:reranked | {rerankResult.ToString()}");

                    var rerankedItems = rerankResult?["choices"]?[0]?["message"]?["content"]?.ToString().Split(",");
                    caller.Reply($"[TerrarAI] Picked the most relevant page(s): {rerankResult?["choices"]?[0]?["message"]?["content"]?.ToString()}");

                    var finalAIContent = "";

                    for (var rerankerIndex = 0; rerankerIndex < rerankedItems.Length; rerankerIndex++)
                    {
                        string pageIDStr = rerankedItems[rerankerIndex].Trim();
                        int pageID = Int32.Parse(pageIDStr);
                        string pageName = collectedDictionary[pageID];
                        if (collectedDictionary.ContainsKey(pageID))
                        {
                            caller.Reply($"[TerrarAI] Reading page with ID: {pageIDStr}", Color.Yellow);
                            var sections = await MediaWikiClient.GetSections(Int32.Parse(pageIDStr));
                            var sectEnt = sections.AsArray();
                            var lines = string.Join(", ", sectEnt);
                            var sectionDictionary = new Dictionary<string, string>();

                            foreach (JsonNode? item in sectEnt)
                            {
                                string? line = item?["line"]?.ToString();
                                string? index = item?["index"]?.ToString();

                                if (line != null && index != null)
                                {
                                    sectionDictionary[line] = index;
                                }
                            }

                            caller.Reply($"[TerrarAI] Found {sectEnt.Count} section(s)", Color.Yellow);

                            var sectionRerankRequest = Prompts.buildDeterministicGroqRequest(Prompts.PageSectionsRerankPrompt, 
                                $"User Query: '{query}'\nPage Sections found: {string.Join(", ", lines)}", 
                                config).Result;
                            var sectionRerankResponse = await groqClient.CreateChatCompletionAsync(sectionRerankRequest);

                            // caller.Reply($"TerrarAI:sectionRerank | {sectionRerankResponse?["choices"]?[0]?["message"]?["content"]?.ToString()}");

                            var rerankedSections = sectionRerankResponse?["choices"]?[0]?["message"]?["content"]?.ToString().Split(",");

                            caller.Reply($"[TerrarAI] Picked {rerankedSections.Length} section(s): {string.Join(", ", rerankedSections)}", Color.Yellow);
                            // caller.Reply(sectionRerankResponse?["choices"]?[0]?["message"]?["content"]?.ToString());
                            foreach (var section in rerankedSections)
                            {
                                if (sectionDictionary.TryGetValue(section, out string sectionIndex))
                                {
                                    string sectionText = await MediaWikiClient.GetWikitextForSection(pageID, sectionIndex.Trim());
                                    finalAIContent += $"Title: {pageName} | Section: {sectionIndex}\n\n";
                                    // caller.Reply($"TerrarAI:sectionText | {sectionText}");
                                    finalAIContent += sectionText;
                                }
                                else
                                {
                                    caller.Reply($"[TerrarAI] Couldn't find section ${section}", Color.Red);
                                }
                            }

                        }
                        else
                        {
                            caller.Reply("[TerrarAI] Could not find the page ID for: " + pageName, Color.Red);
                            continue;
                        }

                    }

                    finalAIContent += $"\n\nUser Query: {query}";

                    var finalRequest = Prompts.buildGroqRequest(Prompts.FinalPrompt, finalAIContent, config).Result;
                    var finalResponse = await groqClient.CreateChatCompletionAsync(finalRequest);

                    // caller.Reply($"TerrarAI:finalResp | {finalResponse?["choices"]?[0]?["message"]?["content"]?.ToString()}");

                    var finalResponseArray = finalResponse?["choices"]?[0]?["message"]?["content"]?.ToString().Split("\n");
                    var finalResponseString = "";
                    foreach (var text in finalResponseArray)
                    {
                        finalResponseString += $"[TerrarAI] {text}\n";
                    }

                    caller.Reply(finalResponseString, Color.Azure);
                    caller.Reply("[TerrarAI] If the response is bigger than the chat window, you can use the arrow keys to scroll", Color.BlueViolet);
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    //send the command to the server
                    ChatHelper.SendChatMessageFromClient(new ChatMessage(input));
                } 
            } catch (Exception ex)
            {
                caller.Reply($"[TerrarAI] Error: {ex.Message}");
            }
        }
    }
}
