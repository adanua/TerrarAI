using System;
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace TerrarAI.Common.Configs
{
    internal class TerrarAIModConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ServerSide;

        [LabelKey("$Mods.TerrarAI.TerrarAIModConfig.APIKey.Label")]
        [TooltipKey("$Mods.TerrarAI.TerrarAIModConfig.APIKey.Tooltip")]
        public string ApiKey { get; set; }

        [LabelKey("$Mods.TerrarAI.TerrarAIModConfig.Model.Label")]
        [TooltipKey("$Mods.TerrarAI.TerrarAIModConfig.Model.Tooltip")]
        [DefaultValue("llama-3.3-70b-versatile")]
        public string Model { get; set; }

        [LabelKey("$Mods.TerrarAI.TerrarAIModConfig.SecondaryModel.Label")]
        [TooltipKey("$Mods.TerrarAI.TerrarAIModConfig.SecondaryModel.Tooltip")]
        [DefaultValue("llama-3.3-70b-specdec")]
        public string SecondaryModel { get; set; }

        [LabelKey("$Mods.TerrarAI.TerrarAIModConfig.Temperature.Label")]
        [DefaultValue(50)]
        [Range(0, 100)]
        [TooltipKey("$Mods.TerrarAI.TerrarAIModConfig.Temperature.Tooltip")]
        public int Temperature { get; set; }

        [LabelKey("$Mods.TerrarAI.TerrarAIModConfig.MaxTokens.Label")]
        [TooltipKey("$Mods.TerrarAI.TerrarAIModConfig.MaxTokens.Tooltip")]
        [DefaultValue(1000)]
        public int MaxTokens { get; set; }
    }
}
