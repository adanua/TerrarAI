using GroqSharp;
using GroqSharp.Models;
using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using TerrarAI.Common.Configs;

namespace TerrarAI.Common
{
    internal class Prompts
    {
        public static string SearchMediaWikiPrompt = "You are a helpful terraria expert. Your job is to extract key search query items for a function that can search on the terraria wiki. Make sure to return your response as just the queries and commas. For example if the user sent: 'I want to craft the boss spawner for the moon lord', you would return: 'Moon Lord' capitalised and in the correct format. If there are multiple items in one, just append using commas as a delimiter, e.g: 'Moon Lord, Moon Phase' etc.. but only use what is referenced in the message. Be sure to use the official Terarria spelling." +
            "Take note that the above examples are only an example and not to be taken as fact.";

        public static string PagesRerankPrompt = "You are a helpful search engine reranker. Your job is to take the users question, along with a set of page names and their respective ID and then decide which out of the pages are the best to find the content from. You should return usually only one, which is the best match given the query. However if multiple pages are present, just add more to the JSON array specified in the JSON structure.";

        public static string PageSectionsRerankPrompt = "You are a helpful search engine reranker. Your job is to take the users question, along with a set of sections of a web page that may be of interest, and return the pages that are worthy of reading. You may return one or more sections, if you are returning more sections then delimit your response with commas." +
            "" +
            "User Query: I want to know how to craft the spawn item for the Moon Lord" +
            "Page Sections found: Summoning, Behaviour, Attacks, Parts, Aftermath, Notes" +
            "" +
            "Given the above query, you would have returned only 'Summoning' as that is the only relevant section." +
            "ONLY return the section name as a comma seperated list. Do NOT return anything other than that" +
            "Also do not add any spaces between commas, just put the section name followed by a comma then the next section. No need for spaces." +
            "Take note that the above example is an example and not to be taken as fact.";

        public static string FinalPrompt = "You are an expert in the game Terraria. You have been given a set of data relevant to the users query, along with the query itself. Use the information provided to answer the users query.";

        public static Task<JsonObject> buildGroqRequest(string prompt, string message, TerrarAIModConfig config)
        {
            var request = new JsonObject
            {
                ["model"] = config.Model,
                ["temperature"] = (double)config.Temperature / 100.0,
                ["max_tokens"] = config.MaxTokens,
                ["messages"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["role"] = "system",
                            ["content"] = prompt
                        },
                        new JsonObject
                        {
                            ["role"] = "user",
                            ["content"] = message
                        }
                    }
            };

            return Task.FromResult(request);
        }

        public static Task<JsonObject> buildDeterministicGroqRequest(string prompt, string message, TerrarAIModConfig config, bool useSubmodel = false)
        {
            var request = new JsonObject
            {
                ["model"] = (useSubmodel ? config.SecondaryModel : config.Model),
                ["temperature"] = 0.4f,
                ["max_tokens"] = config.MaxTokens,
                ["messages"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["role"] = "system",
                            ["content"] = prompt
                        },
                        new JsonObject
                        {
                            ["role"] = "user",
                            ["content"] = message
                        }
                    }
            };

            return Task.FromResult(request);
        }

        public async static Task<JsonNode> buildStructuredGroqRequest(string prompt, string message, string jsonStructure, TerrarAIModConfig config, bool useSubmodel = false)
        {

            IGroqClient groqClient = new GroqClient(config.ApiKey, (useSubmodel ? config.SecondaryModel : config.Model))
                .SetTemperature((double)config.Temperature / 100.0)
                .SetMaxTokens(config.MaxTokens)
                .SetStructuredRetryPolicy(2);

            try
            {
                var response = await groqClient.GetStructuredChatCompletionAsync(jsonStructure,
                    new Message { Role = MessageRoleType.System, Content = prompt },
                    new Message { Role = MessageRoleType.User, Content = message }
                );

                var jsonNode = JsonNode.Parse(response);

                return jsonNode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                throw;
            }
        }
    }
}
