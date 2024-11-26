using System;
using System.Collections.Generic;
using Service.LogicCommon;

using static Service.Logic.GameDefs;
namespace Service.Logic
{

    public class Force
    {
        public List<int> cheat;
        public PersistentData persistentData;

        internal List<int> usedNumbers = new List<int>();
    }

    public class PersistentData
    {
        // progression of 2 scatter count
        public Int32 twoScatterProgress;

        // persistent bigHit stake multiple target
        public Int32 bigHitMinimumStakeMultipleIndex;

        public Int32 activeFrameCount;

        //spins since last WILD tracking
        public Int32 spinsSinceLastWild;
        public Int32 guaranteedWildSpinCount;

        // persistent tracking for "first x spins" elements
        public Int32 playedSpins;
        public Int32 nearMissScattersOnSpin;                        // 2 scatter near miss demonstration in first 10 spins
        public Int32 landingSilverWildOnSpin = -1;                   // single wild that will land in first 10 spins 
        public Int32 nearMissGoldWildOnSpin;                        // gold wild that will spin through in first 20 spins

        // as above, but preserved before a game plays out
        public Int32 onStartPlayedSpins;
        public Int32 onStartNearMissScattersOnSpin;
        public Int32 onStartLandingSilverWildOnSpin;
        public Int32 onStartNearMissGoldWildOnSpin;

        // persistent
        public Int32[,] frameWindow = new Int32[NUMBER_REELS, REEL_WINDOW];
    }


    public class GameState
    {
        public List<Feature> features = new List<Feature>();
        public long bet;
        public long cost;
        public long win;
        public Dictionary<long, PersistentData> persistentDataMap = new Dictionary<long, PersistentData>();
        public Force force;
        internal long coinWin; // Only for matching original logic
    }

}
