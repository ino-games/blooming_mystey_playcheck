using System;
using System.Collections.Generic;
using System.Text;

namespace Service.Logic
{
    public static class GameDefs
    {
        // *******************************************************************************
        public enum RTP
        {
            Target86,
            Target92,
            Target94,
            Target96
        }

        public const Int32 BONUS_BUY_COST_86 = 427;
        public const Int32 BONUS_BUY_COST_92 = 403;
        public const Int32 BONUS_BUY_COST_94 = 394;
        public const Int32 BONUS_BUY_COST_96 = 386;

        // *******************************************************************************
        // misc...
        public const Int32 NUMBER_REELS = 5;
        public const Int32 REEL_WINDOW = 4;
        public const Int32 NUMBER_WIN_LINES = 20;
        public const Int32 REELS_BIAS_TABLE_SIZE = NUMBER_REELS * REEL_WINDOW;
        public const Int32 MAX_SCATTERS = 5;

        public const Int32 GAME_MODE_BASEGAME = 0;
        public const Int32 GAME_MODE_FREE_SPINS = 1;



        // exploding wilds
        public const Int32 MAX_ALLOWABLE_FRAMES_BASEGAME = 8;
        public const Int32 MAX_EXPLODING_WILDS_STAGES = 3;
        public const Int32 MAX_EXPLODING_WILDS_PER_STAGE = 4;
        public const Int32 EW_FROM_SHIFT = 16;

        public const Int32 MAX_EXPLODING_WILDS_ATTEMPTS = 10;
        public const Int32 MAX_EX_STAKE_MULTIPLIE_WIN_UNCONTESTED = 20;
        public const Int32 MAX_EW_STAKE_MULTIPLE_WIN = 40;


        public const Int32 MAX_FRAME_MULTIPLIER = 5;

        // free spins
        public const Int32 FULL_SCATTER_PROGRESS_BAR_COUNT = 50;
        public const Int32 FREESPINS_TRIGGER_MAX_ACTIVE_FRAMES = 6;
        public const Int32 MAX_TOO_LARGE_RETRIES = 10;
        public const Int32 FB_BIG_HIT = 1;
        public const Int32 FB_EXP_WILDS = 2;
        public const Int32 FB_FORCED_WIN = 4;
        public const Int32 LOGIC_MAX_FREESPINS = 40;      // 20 spins from a 5 scatter trigger + up to 4 * +5 extra spins
        public const Int32 MAX_FREE_SPINS_TRAIL_LENGTH = 12;
        public const Int32 FS_SPIN_COUNT_INCREMENT = 5;

        public const Int32 FS_MOOD_1 = 1;

        // trail types
        public const Int32 FSTT_BIG_HIT = 0;
        public const Int32 FSTT_5_SPINS_X2_MULTIPLIER = 1;
        public const Int32 FSTT_5_SPINS_X3_MULTIPLIER = 2;
        public const Int32 FSTT_5_SPINS_X5_MULTIPLIER = 3;
        public const Int32 FSTT_5_SPINS_X10_MULTIPLIER = 4;

        public const Int32 COINS_PER_BET = 10;



        // symbols...
        public const Int32 LOGIC_SYMBOL_SCATTER = 0;
        public const Int32 LOGIC_SYMBOL_BIGHIT = 1;

        public const Int32 LOGIC_SYMBOL_WILD = 2;

        public const Int32 LOGIC_SYMBOL_HIGH1 = 3;
        public const Int32 LOGIC_SYMBOL_HIGH2 = 4;
        public const Int32 LOGIC_SYMBOL_HIGH3 = 5;
        public const Int32 LOGIC_SYMBOL_HIGH4 = 6;

        public const Int32 LOGIC_SYMBOL_LOW1 = 7;
        public const Int32 LOGIC_SYMBOL_LOW2 = 8;
        public const Int32 LOGIC_SYMBOL_LOW3 = 9;
        public const Int32 LOGIC_SYMBOL_LOW4 = 10;
        public const Int32 LOGIC_SYMBOL_LOW5 = 11;
        public const Int32 LOGIC_SYMBOL_MAX = 12;


        // bandsets...
        public const Int32 LOGIC_BANDSET_LOSING = 0;
        public const Int32 LOGIC_BANDSET_NORMAL = 1;
        public const Int32 LOGIC_BANDSET_JUST_LOW = 2;
        public const Int32 LOGIC_BANDSET_MAX = 3;

        public static Int32[,] AWARDS = new Int32[LOGIC_SYMBOL_MAX, 6]
        {
            { LOGIC_SYMBOL_SCATTER, 0, 0, 0, 0, 0 },
            { LOGIC_SYMBOL_BIGHIT, 0, 0, 0, 0, 0 },
            { LOGIC_SYMBOL_WILD, 0, 0, 20, 50, 200 },
            { LOGIC_SYMBOL_HIGH1, 0, 0, 10, 25, 100 },
            { LOGIC_SYMBOL_HIGH2, 0, 0, 5, 10, 50 },
            { LOGIC_SYMBOL_HIGH3, 0, 0, 2, 5, 30 },
            { LOGIC_SYMBOL_HIGH4, 0, 0, 1, 3, 20 },
            { LOGIC_SYMBOL_LOW1, 0, 0, 1, 3, 10 },
            { LOGIC_SYMBOL_LOW2, 0, 0, 1, 3, 10 },
            { LOGIC_SYMBOL_LOW3, 0, 0, 1, 3, 10 },
            { LOGIC_SYMBOL_LOW4, 0, 0, 1, 3, 10 },
            { LOGIC_SYMBOL_LOW5, 0, 0, 1, 3, 10 },
        };

        // win line patterns
        public static Int32[,] PAYLINES =
        {
            { 0,0,0,0,0 },
            { 1,1,1,1,1 },
            { 2,2,2,2,2 },
            { 3,3,3,3,3 },
            { 0,1,2,1,0 },
            { 1,2,3,2,1 },
            { 2,1,0,1,2 },
            { 3,2,1,2,3 },
            { 0,1,1,1,0 },
            { 1,2,2,2,1 },
            { 2,3,3,3,2 },
            { 3,2,2,2,3 },
            { 2,1,1,1,2 },
            { 1,0,0,0,1 },
            { 0,1,0,1,0 },
            { 1,2,1,2,1 },
            { 2,3,2,3,2 },
            { 3,2,3,2,3 },
            { 2,1,2,1,2 },
            { 1,0,1,0,1 }
        };

        public static int NUMBER_LINES = PAYLINES.GetLength(0);

    }
}

