using System;
using Service.Logic;
using Service.LogicCommon.Win;

namespace Service.LogicCommon.Api.Spin
{
    /// <summary>Base SpinResult</summary>
    [Serializable]
    public class SpinResult<T> where T:WinItem
    {
        /// <summary>ReelSetId for the spin</summary>
        public int reelSetId;

        /// <summary>The window of symbols visible at end of spin.</summary>
        public int[,] reelWindow = new int[GameDefs.NUMBER_REELS, GameDefs.REEL_WINDOW];
        public int[,] overlayWindow = new int[GameDefs.NUMBER_REELS, GameDefs.REEL_WINDOW];

        /// <summary>The stop positions of all the reels</summary>
        public int[] reelStops = new int[GameDefs.NUMBER_REELS];

        /// <summary>The WinDetails of the spin</summary>
        public WinDetails<T> winDetails = new WinDetails<T>();
    }

    /// <summary>Ways SpinResult</summary>
    [Serializable]
    public class WaysSpinResult: SpinResult<WaysWinItem> {
        public int numberOfWays;
    }

    /// <summary>UltraWays SpinResult</summary>
    [Serializable]
    public class UWSpinResult : WaysSpinResult
    {
        public enum UWStatus
        {
            NORMAL,
            DOUBLE,
            QUAD
        }

        /// <summary>The UltraWays statuses for all the symbols at the end of the spin.</summary>
        public UWStatus[,] uwStatusWindow;
    }
}