using System;
using Service.Logic;

namespace Service.LogicCommon.Win
{

    /// <summary>Base WinItem.</summary>
    public class WinItem
    {
        /// <summary>Total win in cash.</summary>
        public long winInCash = 0;

        /// <summary>Unmultiplied win in cash.</summary>
        public long baseWinInCash = 0;

        /// <summary>SymbolId for the symbol in the win.</summary>
        public int symbolId = 0;

        /// <summary>Number of symbols in the win.</summary>
        public int symbolCount = 0;

        /// <summary>Multipier in effect.</summary>
        public int multiplier = 1;

        /// <summary>All the SymbolIds in the win in their respective positions.</summary>
        public int[,] symbolsInWin = new int[GameDefs.NUMBER_REELS, GameDefs.REEL_WINDOW];

        /// <summary>Total win in coins.</summary>
        internal long winInCoins = 0;
    }

    /// <summary>WinItem for payLine games.</summary>
    public class PayLineWinItem : WinItem
    {
        /// <summary>PayLineId for the win.</summary>
        public int payLineId;
    }

    /// <summary>WinItem for way games.</summary>
    public class WaysWinItem : WinItem
    {
        /// <summary>Number of ways for the win.</summary>
        public int ways;
    }
}