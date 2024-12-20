using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader.Config;

namespace TerrarAI.Common.Configs
{
    internal class TerrarAIModConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [Header("$TerrarAIModConfig.ConfigHeader")]

        [LabelKey("$TerrarAIModConfig.APIKey.Label")]
        [TooltipKey("$TerrarAIModConfig.APIKey.Tooltip")]
        public string ApiKey { get; set; }

        [LabelKey("$TerrarAIModConfig.Model.Label")]
        [TooltipKey("$TerrarAIModConfig.Model.Tooltip")]
        [DefaultValue("llama-3.3-70b-versatile")]
        public string Model { get; set; }

        [LabelKey("$TerrarAIModConfig.Temperature.Label")]
        [DefaultValue(0.5)]
        [Range(0, 100)]
        [TooltipKey("$TerrarAIModConfig.Temperature.Tooltip")]
        public int Temperature { get; set; }
         
        [LabelKey("$TerrarAIModConfig.MaxTokens.Label")]
        [TooltipKey("$TerrarAIModConfig.MaxTokens.Tooltip")]
        [DefaultValue(1000)]
        public int MaxTokens { get; set; }
    }
}
