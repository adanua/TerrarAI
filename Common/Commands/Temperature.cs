using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrarAI.Common.Configs;
using Terraria.ModLoader;

namespace TerrarAI.Common.Commands
{
    internal class Temperature : ModCommand
    {
        public override string Command => "temp";
        public override string Description =>
            "Check what your temp is";
        public override string Usage => "/temp <query>";

        public override CommandType Type => CommandType.Chat;

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var config = ModContent.GetInstance<TerrarAIModConfig>();
            var temp = config.Temperature;
            caller.Reply($"Your temp is: {temp}\nYour dived temp is: ${temp / 100}");
        }
    }
}
