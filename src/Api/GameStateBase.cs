using System;
using System.Collections.Generic;

namespace Service.LogicCommon
{
    [Serializable]
    public class GameStateBase {
        public long bet;
        public bool buyBonus = false;
        public decimal buyBonusCost;
        public bool gambleForBonusWon = false;
        public int gambleForBonusChance;
        public long totalWinInCash = 0;
        public bool willDoFreeSpins;
        public string ApiVersion = Service.LogicCommon.ApiVersion.GetVersion();
        public List<string> nextCommands;
    }
}