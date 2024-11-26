using System;
using System.Collections.Generic;
using static Service.Logic.GameDefs;

namespace Service.Logic
{
    // *******************************************************************************
    // this is like your GameResults structure.  Anything in these will be reported
    // back to the client
    // *******************************************************************************

    // *******************************************************************************
    // Basegame Result
    // *******************************************************************************
    class BaseGameResult
    {
        public int clientRNGSeed;
        public GameSpinState baseGameSpin = new GameSpinState();
        public bool willDoFreeSpins;
        public int freeSpinsMode;
        public bool willDoSmallWin;
    }

    // *******************************************************************************
    // FreeSpins Result
    // *******************************************************************************
    class FreeSpinsResult
    {
        public Int32 initialSpinCount;
        public List<GameSpinState> freeSpinList = new List<GameSpinState>();

        public Int32 spinCount;
        public Int32 type;

        public long totalWinInCoins;
        public long totalWinInCash;
        public bool buyBonusResult;        // true if player bought FS
        public bool fastStart;
    }

    // *******************************************************************************
    class GameResults
    {
        public BaseGameResult baseGameResult = new BaseGameResult();
        public FreeSpinsResult freeSpinsResult = new FreeSpinsResult();
        public long winsFromBaseGameInCoins;
        public long winsFromExplodingWildsInCoins;
        public long winsFromBigHitInCoins;
        public long winsFromFreeSpinsInCoins;

        // *******************************************************************************
        public GameResults()
        {
            winsFromBaseGameInCoins = 0;
            winsFromExplodingWildsInCoins = 0;
            winsFromBigHitInCoins = 0;
            winsFromFreeSpinsInCoins = 0;
        }
    }
}
