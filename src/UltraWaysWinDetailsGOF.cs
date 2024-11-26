using System;
using System.Collections.Generic;

namespace Service.LogicCommon
{
    [Serializable]
    public class UltraWaysWinDetailsGOF
    {
        public List<UltraWaysWinItemGOF> winList = new List<UltraWaysWinItemGOF>();
        public int totalAmount = 0;
        internal int[,] symbolsInWin;

         public void Add(int coinValue, int symbolIndex, int winLength, int currentWinCount,
            int totalWonInCoins)
        {
            var item = new UltraWaysWinItemGOF
            {
                award = totalWonInCoins,
                amount = totalWonInCoins * coinValue,
                ways = currentWinCount,
                symbol = symbolIndex,
                count = winLength
            };


            winList.Add(item);
        }
    }
}