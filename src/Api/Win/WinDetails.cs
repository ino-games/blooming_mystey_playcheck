using System;
using System.Collections.Generic;
using Service.Logic;

namespace Service.LogicCommon.Win {

    /// <summary>Base WinDetails.</summary>
    [Serializable]
    public class WinDetails<T> where T: WinItem {
        /// <summary>Total win of all WinItems in cash.</summary>
        public long cashWin;

        /// <summary>Symbols in all WinItems.</summary>
        public int[,] symbolsInWin = new int[GameDefs.NUMBER_REELS, GameDefs.REEL_WINDOW];
        
        /// <summary>All WinItems.</summary>
        public List<T> winList = new List<T>();

        /// <summary>Total win of all WinItems in coins.</summary>        
        internal long coinWin = 0;
    }

}