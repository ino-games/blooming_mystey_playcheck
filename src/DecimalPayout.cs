using System.Collections.Generic;
using System.Linq;
using Service.Logic;
using Service.LogicCommon;
using Service.LogicCommon.Win;

namespace Service.PlayCheckCommon
{
    internal class DecimalWaysPayout : DecimalPayout
    {

        public DecimalWinDetails<DecimalWaysWinItem> winDetails;
        
        public DecimalWaysPayout(WaysPayout waysPayout) : base(waysPayout)
        {
            winDetails = new DecimalWinDetails<DecimalWaysWinItem>(waysPayout.winDetails.cashWin, waysPayout.winDetails.winList.Select(winItem => new DecimalWaysWinItem(winItem)));
        }
    }

    internal class DecimalPayLinePayout : DecimalPayout
    {

        public DecimalWinDetails<DecimalPayLineWinItem> winDetails;
        

        public DecimalPayLinePayout(PayLinePayout payout) : base(payout)
        {
            winDetails = new DecimalWinDetails<DecimalPayLineWinItem>(payout.winDetails.cashWin, payout.winDetails.winList.Select(winItem => new DecimalPayLineWinItem(winItem)));
        }
    }

    internal class DecimalPayout : Feature
    {

        public override string type { get => FeatureTypes.Payout; }
        public decimal win;
        public DecimalPayout(Payout payout)
        {
            id = payout.id;
            win = (decimal)payout.win / 100;
        }
    }

    internal class DecimalWinDetails<T> where T : DecimalWinItem
    {
        /// <summary>Total win of all WinItems in cash.</summary>
        public decimal cashWin;

        /// <summary>All WinItems.</summary>
        public IEnumerable<T> winList;

        /// <summary> Spin that generated the win. </summary>
        public MappedSpin spin;


        public DecimalWinDetails(long cashWin, IEnumerable<T> winList)
        {
            this.cashWin = (decimal)cashWin / 100;
            this.winList = winList;
        }
    }

    internal class DecimalWinItem
    {
        /// <summary>Total win in cash.</summary>
        public decimal winInCash = 0;

        /// <summary>Unmultiplied win in cash.</summary>
        public decimal baseWinInCash = 0;

        /// <summary>SymbolId for the symbol in the win.</summary>
        public int symbolId = 0;
        public string symbolImage;

        /// <summary>Number of symbols in the win.</summary>
        public int symbolCount = 0;

        /// <summary>Multipier in effect.</summary>
        public int multiplier = 1;

        /// <summary>[Reel][Row] All the SymbolIds in the win in their respective positions.</summary>
        public List<List<int>> symbolsInWin;

        /// <summary>[Reel][Row] All the SymbolData in the win, null = no win symbol at that position</summary>
        public List<List<SymbolData>> symbolsData;

        public DecimalWinItem(WinItem winItem)
        {
            winInCash = (decimal)winItem.winInCash / 100;
            baseWinInCash = (decimal)winItem.baseWinInCash / 100;
            symbolId = winItem.symbolId;
            symbolCount = winItem.symbolCount;
            multiplier = winItem.multiplier;

            symbolsInWin = new List<List<int>>();
            for(var i = 0; i< winItem.symbolsInWin.GetLength(0); i++)
            {
                symbolsInWin.Add(new List<int>());
                for(var j = 0; j< winItem.symbolsInWin.GetLength(1); j++)
                {
                    symbolsInWin[i].Add(winItem.symbolsInWin[i,j]);
                }
            }
        }

        // /// <summary>Total win in coins.</summary>
        // internal long winInCoins = 0;
    }

    internal class DecimalPayLineWinItem : DecimalWinItem
    {
        public int payLineId;
        public bool isPayLinePayout = true;
        
        public DecimalPayLineWinItem(PayLineWinItem winItem): base(winItem)
        {
            payLineId = winItem.payLineId;
        }

    }

    internal class DecimalWaysWinItem : DecimalWinItem
    {
        public int ways;
        public bool isWaysPayout = true;
        public DecimalWaysWinItem(WaysWinItem winItem): base(winItem)
        {
            ways = winItem.ways;
        }
    }
}