using System;
using System.Collections.Generic;
using Service.LogicCommon;

namespace Service.Logic
{
    public class GameClientConfig
    {
        public Dictionary<int, int[]> awards;
        public List<List<List<int>>> reelSets;
        public List<List<int>> payLines;
        public List<Symbol> symbols;
        public decimal buyBonusCost;
        public Dictionary<int, decimal> gambleForBonusCosts;
        public List<string> featureTypes = FeatureTypes.GetTypes();
        public List<string> featureIds = FeatureIds.GetIds();
       
    }
}