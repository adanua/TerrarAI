using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using TerrarAI.Common.Configs;
using TerrarAI.Libs;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.ModLoader;

namespace TerrarAI.Common.Commands
{
    internal class CalamityCommand : ModCommand
    {
        public override string Command => "calamity";
        public override string Description =>
            "Get a quick response based off the Calamity Wiki on a query using AI.";
        public override string Usage => "/calamity <query>";

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

                var MediaWikiClient = new CalamityMediaWiki();

                if (!hasApiKey)
                {
                    caller.Reply(
                        "[TerrarAI] No API key found. Please set an API key in the mod configuration.",
                        Color.Red
                    );
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
                    var queryRequest = Prompts
                        .buildGroqRequest(query, Prompts.SearchMediaWikiPrompt, config)
                        .Result;
                    caller.Reply("[TerrarAI] Querying AI...", Color.Yellow);
                    var queriesResult = await groqClient.CreateChatCompletionAsync(queryRequest);

                    // caller.Reply($"TerrarAI:query | {query}");

                    var items = queriesResult
                        ?["choices"]?[0]?["message"]?["content"]?.ToString()
                        .Split(",");

                    // caller.Reply($"TerrarAI:items | {queriesResult?["choices"]?[0]?["message"]?["content"]?.ToString()}");

                    caller.Reply(
                        "[TerrarAI] Found the following items to search on the wiki: "
                            + queriesResult?["choices"]?[0]?["message"]?["content"]?.ToString()
                    );
                    var collectedDictionary = new Dictionary<int, string>();
                    var totalCollectedItems = "";
                    var finalAIContent = "";
                    var finalContentPages = 0;

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
                        caller.Reply(
                            "[TerrarAI] Search results for " + item + ": " + collectedResults,
                            Color.Yellow
                        );
                        totalCollectedItems += collectedResults;
                    }
                    caller.Reply("[TerrarAI] Picking the most relevant page(s)", Color.Yellow);

                    var rerankResult = Prompts
                        .buildStructuredGroqRequest(
                            Prompts.PagesRerankPrompt,
                            "User Query: '" + query + "'\nPages found: " + totalCollectedItems,
                            @"{
                            ""pageIDs"": [
                                {
                                    ""string | The name of the page"": ""string | The integer ID of the page""
                                }
                            ]
                        }",
                            config,
                            true
                        )
                        .Result;

                    var rerankResultJson = JsonNode.Parse(rerankResult.ToString());

                    JsonArray rerankedArray = (JsonArray)rerankResultJson?["pageIDs"];
                    List<string> pageNames = [];

                    foreach (JsonNode pageIDNode in rerankedArray)
                    {
                        if (pageIDNode is JsonObject jsonObject)
                        {
                            // Loop over key-value pairs in each object
                            foreach (var kvp in jsonObject)
                            {
                                string pageName = kvp.Key; // e.g., "moon lord"
                                pageNames.Add(pageName);
                            }
                        }
                    }

                    caller.Reply(
                        $"[TerrarAI] Picked the most relevant page(s): {string.Join(", ", pageNames)}",
                        Color.YellowGreen
                    );
                    foreach (JsonNode pageIDNode in rerankedArray)
                    {
                        if (pageIDNode is JsonObject jsonObject)
                        {
                            // Loop over key-value pairs in each object
                            foreach (var kvp in jsonObject)
                            {
                                string pageName = kvp.Key; // e.g., "moon lord"
                                var pageID = kvp.Value!.GetValue<string>();
                                caller.Reply(
                                    $"[TerrarAI] Reading page with ID: {pageID}",
                                    Color.Yellow
                                );

                                var sections = await MediaWikiClient.GetSections(pageID);
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

                                caller.Reply(
                                    $"[TerrarAI] Found {sectEnt.Count} section(s)",
                                    Color.Yellow
                                );

                                var sectionRerankRequest = Prompts
                                    .buildDeterministicGroqRequest(
                                        Prompts.PageSectionsRerankPrompt,
                                        $"User Query: '{query}'\nPage Sections found: {string.Join(", ", lines)}",
                                        config,
                                        true
                                    )
                                    .Result;
                                var sectionRerankResponse =
                                    await groqClient.CreateChatCompletionAsync(
                                        sectionRerankRequest
                                    );

                                // caller.Reply($"TerrarAI:sectionRerank | {sectionRerankResponse?["choices"]?[0]?["message"]?["content"]?.ToString()}");

                                var rerankedSections = sectionRerankResponse
                                    ?["choices"]?[0]?["message"]?["content"]?.ToString()
                                    .Split(",");

                                caller.Reply(
                                    $"[TerrarAI] Picked {rerankedSections.Length} section(s): {string.Join(", ", rerankedSections)}",
                                    Color.Yellow
                                );
                                // caller.Reply(sectionRerankResponse?["choices"]?[0]?["message"]?["content"]?.ToString());
                                foreach (var section in rerankedSections)
                                {
                                    if (
                                        sectionDictionary.TryGetValue(
                                            section,
                                            out string sectionIndex
                                        )
                                    )
                                    {
                                        string sectionText =
                                            await MediaWikiClient.GetWikitextForSection(
                                                pageID,
                                                sectionIndex.Trim()
                                            );

                                        if (finalContentPages <= 3)
                                        {
                                            finalContentPages++;
                                            finalAIContent +=
                                                $"Title: {pageName} | Section: {sectionIndex}\n\n";
                                            // caller.Reply($"TerrarAI:sectionText | {sectionText}");
                                            finalAIContent += sectionText;
                                        }
                                        else
                                        {
                                            caller.Reply(
                                                $"[TerrarAI] Skipping section '{section}' as to make sure you don't hit the token limit (max 3 sections at a time)",
                                                Color.YellowGreen
                                            );
                                        }
                                    }
                                    else
                                    {
                                        caller.Reply(
                                            $"[TerrarAI] Skipping section '{section}' as it wasn't found",
                                            Color.YellowGreen
                                        );
                                    }
                                }
                            }
                        }
                    }

                    finalAIContent += $"\n\nUser Query: {query}";

                    var finalRequest = Prompts
                        .buildGroqRequest(Prompts.FinalPrompt, finalAIContent, config)
                        .Result;
                    var finalResponse = await groqClient.CreateChatCompletionAsync(finalRequest);

                    // caller.Reply($"TerrarAI:finalResp | {finalResponse?["choices"]?[0]?["message"]?["content"]?.ToString()}");

                    var finalResponseArray = finalResponse
                        ?["choices"]?[0]?["message"]?["content"]?.ToString()
                        .Split("\n");
                    var finalResponseString = "";
                    foreach (var text in finalResponseArray)
                    {
                        finalResponseString += $"\n[TerrarAI] {text}";
                    }

                    caller.Reply(finalResponseString, Color.BlueViolet);
                    caller.Reply(
                        "[TerrarAI] If the response is bigger than the chat window, you can use the arrow keys to scroll",
                        Color.BlueViolet
                    );
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    //send the command to the server
                    ChatHelper.SendChatMessageFromClient(new ChatMessage(input));
                }
            }
            catch (Exception ex)
            {
                caller.Reply($"[TerrarAI] Error: {ex.Message}");
            }
        }
    }
}
