using System;
using System.Collections.Generic;

namespace Service.LogicCommon
{
    [Serializable]
    public class UltraWaysWinDetails
    {
        public List<UltraWaysWinItem> winList = new List<UltraWaysWinItem>();
        public int totalAmount = 0;
    }
}