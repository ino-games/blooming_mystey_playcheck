using System.Collections.Generic;
using Service.LogicCommon;

namespace Service.PlayCheckCommon
{
    public class PersistedState
    {

        public long bet = 0;
        public List<Feature> processedStages = new List<Feature>();
        public List<Feature> currentStage = new List<Feature>();
        public List<int> optionsPicked = new List<int>();
    }
}