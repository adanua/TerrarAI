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

namespace TerrarAI.Common.Commands
{
    internal class AICommand : ModCommand
    {
        public override string Command => "ai";
        public override string Description => "Get a quick response based off the Terraria wikis on a query using AI.";
        public override string Usage => "/ai <query>";

        public override CommandType Type => CommandType.Chat;

        public override async void Action(CommandCaller caller, string input, string[] args)
        {
            var config = ModContent.GetInstance<TerrarAIModConfig>();
            bool hasApiKey = !string.IsNullOrEmpty(config.ApiKey);
            float temperature = config.Temperature;
            int maxTokens = config.MaxTokens;
            string model = config.Model;

            if(!hasApiKey)
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
            } else
            {
                String query = String.Join(" ", args);
                var request = new JsonObject
                {
                    ["model"] = model,
                    ["temperature"] = temperature,
                    ["max_tokens"] = maxTokens,
                    ["messages"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["role"] = "system",
                            ["content"] = "You are a helpful terraria expert."
                        },
                        new JsonObject
                        {
                            ["role"] = "user",
                            ["content"] = query
                        }
                    }
                };
                caller.Reply("Querying AI...", Color.Yellow);
                var result = await groqClient.CreateChatCompletionAsync(request);
                caller.Reply("[TerrarAI] " + result?["choices"]?[0]?["message"]?["content"]?.ToString(), Color.AliceBlue);
                caller.Reply("[TerrarAI] If the response is bigger than the chat window, you can use the arrow keys to scroll", Color.BlueViolet);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                //send the command to the server
                ChatHelper.SendChatMessageFromClient(new ChatMessage(input));
            }
        }
    }
}
