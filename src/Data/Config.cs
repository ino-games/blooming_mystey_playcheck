
using System.Collections.Generic;
using System.Linq;
using Service.LogicCommon;
using Service.LogicCommon.Utils;

using static Service.Logic.GameDefs;

namespace Service.Logic
{
    public class Config : GameConfig
    {

        public static Config Setup()
        {
            var config = new Config
            {
                nColumns = GameDefs.NUMBER_REELS,
                nRows = GameDefs.REEL_WINDOW,
            };

            config.symbols = ArrayUtils.ToList(GameDefs.AWARDS).Select((symbolData, i) => new Symbol { name = symbolNames[i], id = symbolData.Take(1).Single(), awards = symbolData.Skip(1).ToArray() }).ToList();
            config.reelSets = config.GetReelSets(new BandSet());
            config.payLines = ArrayUtils.ToList(GameDefs.PAYLINES);
            config.Initialize();
            return config;
        }

        private List<List<List<int>>> GetReelSets(BandSet bandsetData)
        {
            var reelSets = new List<List<List<int>>>();
            for (var reelSetId = 0; reelSetId < LOGIC_BANDSET_MAX; reelSetId++)
            {
                var reelSet = new List<List<int>>();
                for (var reelIndex = 0; reelIndex < NUMBER_REELS; reelIndex++)
                {
                    var reel = new List<int>();
                    for (var symbolIndex = 0; symbolIndex < bandsetData.GetBandSetReelLength(reelSetId, reelIndex); symbolIndex++)
                    {
                        reel.Add(bandsetData.GetSymbol(reelSetId, reelIndex, symbolIndex));
                    }
                    reelSet.Add(reel);
                }
                reelSets.Add(reelSet);
            }

            return reelSets;
        }

        private static string[] symbolNames = new string[] {
            "scatter",
            "collect",
            "wild",
            "high1",
            "high2",
            "high3",
            "high4",
            "low1",
            "low2",
            "low3",
            "low4",
            "low5"
        };
    }

}
