using System.Collections.Generic;
namespace Service.Logic
{
    public class FeatureIds
    {
        public const string BaseGame = "baseGame";
        public const string FreeSpins = "freeSpins";
        public const string FreeSpin = "freeSpin";
        public const string Pick = "pick";
        public const string Respin = "respin";
        public const string Anticipation = "anticipation";
        public const string Trail = "trail";
        public const string Scatters = "scatters";
        public const string Multiplier = "multiplier";
        public const string BigHit = "bigHit";
        public const string ExplodingWild = "explodingWild";
        public const string ExplodingWildEnd = "explodingWildEnd";

        public static List<string> GetIds()
        {
            List<string> constansts = new List<string>();
            foreach (var constant in typeof(FeatureIds).GetFields())
            {
                if (constant.IsLiteral && !constant.IsInitOnly)
                {
                    constansts.Add((string)constant.GetValue(null));
                }
            }
            return constansts;

        }

    }

}
