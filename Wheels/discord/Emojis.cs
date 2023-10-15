using System.Collections.Generic;

namespace WheelsGodot.discord {
    public static class Emojis {
        public const string Blank = "<:Blank:1153452767785013248>";

        public const string LockTop = "<:Wheels_Lock_Top:1153450728229183498>";
        public const string LockBottom = "<:Wheels_Lock_Bot:1153450729751707739>";

        public static readonly string[] HeroIcons = new string[] { "<:Wheels_HeroA:1162817257957773352>", "<:Wheels_HeroB:1162817259136364626>" };

        public static readonly IReadOnlyDictionary<string, string> SymbolToEmoji = new Dictionary<string, string>() {
            { "Empty,1", "<:WheelsEmpty:1153436297961160788>" },
            { "Energy_A,1", "<:WheelsEnergyA1:1153436752070062101>" },
            { "Energy_A,2", "<:WheelsEnergyA2:1153436526961758268>" },
            { "Energy_A,3", "<:WheelsEnergyA3:1153436983780188241>" },
            { "Energy_B,1", "<:WheelsEnergyB1:1153436873386098830>" },
            { "Energy_B,2", "<:WheelsEnergyB2:1153436657253621832>" },
            { "Energy_B,3", "<:WheelsEnergyB3:1153436925101867120>" },
            { "XP_A,1", "<:WheelsXPA1:1153436458003218533>" },
            { "XP_A,2", "<:WheelsXPA2:1153436803173466243>" },
            { "XP_A,3", "<:WheelsXPA3:1153436597082140723>" },
            { "XP_B,1", "<:WheelsXPB1:1153437108866908261>" },
            { "XP_B,2", "<:WheelsXPB2:1153437035957329971>" },
            { "XP_B,3", "<:WheelsXPB3:1153437173635358892>" },
            { "Hammer,1", "<:WheelsHammer1:1153437224411607090>" },
            { "Hammer,2", "<:WheelsHammer2:1153436714841415680>" },
            { "Hammer,3", "<:WheelsHammer3:1153436406421659751>" },
        };

        public const string CrownIcon = "<:Wheels_Crown:1162837374582140960>";
        public const string BulwarkIcon = "<:Wheels_Bulwark:1162837376125648896>";
    }
}
