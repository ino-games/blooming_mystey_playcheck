using System;
using Service.LogicCommon;
using Service.LogicCommon.Win;
using static Service.Logic.GameDefs;

namespace Service.Logic
{
    class PayWays
    {
        // *******************************************************************************
        public const Int32 MAX_WIN_LENGTH = 20;

        // *******************************************************************************
        public Int32 awardCount = 0;
        private Int32 wildMask = 0;

        Int32[,] award_table = ObjectUtils.Clone(GameDefs.AWARDS);



        // *******************************************************************************
        private void ConvertAwardTable()
        {
            awardCount = GameDefs.LOGIC_SYMBOL_MAX;

            for (Int32 a = 0; a < awardCount; a++)
            {
                award_table[a, 0] = (1 << award_table[a, 0]); // Convert 1st award_table entry to bitwise symbol
            }

            Console.WriteLine("Award Count is " + awardCount);
        }

        // ***************************************************************************************
        public long ConvertCoinValueToCash(long coins, long coinValue)
        {
            return coins * coinValue;
        }


        // *******************************************************************************
        public PayWays()
        {
            Console.WriteLine("Initialising PayWays");
            ConvertAwardTable();

            Maths.SetAllBits(ref wildMask);
            Maths.ClearSingleBit(ref wildMask, LOGIC_SYMBOL_SCATTER);
        }





        public Int32 CalculateMultiplier(Int32 payLine, Int32 winLength, Int32[,] multiplierWindow)
        {
            Int32 multiplier = 1;


            for (Int32 reel = 0; reel < winLength; reel++)
            {
                Int32 row = GameDefs.PAYLINES[payLine, reel];
                Int32 value = multiplierWindow[reel, row];

                if (value > 1)
                    multiplier *= value;
            }


            return multiplier;
        }


        // this version takes a passed multiplier window
        public WinDetails<PayLineWinItem> CalculateTotal(InternalState internalState, Int32[,] rw, Int32[,] mw, Int32 paylineCount, int multiplier = 1)
        {
            // Int32 totalWin = 0;
            Int32 thisWin = 0;
            Int32 award = 0;
            Int32 winLength = 0;
            Int32 mp;

            var reelWindow = rw;
            var multiplierWindow = new Int32[NUMBER_REELS, REEL_WINDOW];

            var winDetails = new WinDetails<PayLineWinItem>();

            // prepare the multiplier window, since the passed in version is the symbol status, so 0==x1 multiplier
            for (Int32 row = 0; row < REEL_WINDOW; row++)
            {
                for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                {
                    multiplierWindow[reel, row] = mw[reel, row] + 1;
                }
            }

            for (Int32 a = 0; a < paylineCount; a++)
            {
                mp = 1;

                thisWin = FindSymbolWin(
                    reelWindow[0, (PAYLINES[a, 0])],
                    reelWindow[1, (PAYLINES[a, 1])],
                    reelWindow[2, (PAYLINES[a, 2])],
                    reelWindow[3, (PAYLINES[a, 3])],
                    reelWindow[4, (PAYLINES[a, 4])],
                    ref award, ref winLength, a, multiplierWindow);

                if (thisWin > 0)
                {
                    mp = CalculateMultiplier(a, winLength, multiplierWindow);

                    AddPayWays(internalState, winDetails, award, a, winLength, thisWin, mp * multiplier);

                    // winDetails.coinWin = totalWin;
                }
            }


            return winDetails;
        }

        public void AddPayWays(InternalState internalState, WinDetails<PayLineWinItem> dest, Int32 symbolIndex, Int32 winLine, Int32 winLength, Int32 totalWonInCoins, Int32 multiplier)
        {
            PayLineWinItem item = new PayLineWinItem();

            item.winInCoins = totalWonInCoins * multiplier;
            item.baseWinInCash = ConvertCoinValueToCash(totalWonInCoins, internalState.coinValue);
            item.winInCash = item.baseWinInCash * multiplier;
            // item.count = 1;
            item.symbolId = symbolIndex;
            item.symbolCount = winLength;
            item.payLineId = winLine;
            item.multiplier = multiplier;

            dest.winList.Add(item);

            dest.coinWin += item.winInCoins;
            dest.cashWin += item.winInCash;
        }


        Int32 GetCumulativeMultiplier(int[,] multiplierWindow, Int32 winLineIndex, Int32 length)
        {
            Int32 multiplier = 1;

            for (Int32 n = 0; n < length; n++)
                multiplier *= multiplierWindow[n, PAYLINES[winLineIndex, n]];

            return multiplier;
        }


        // *******************************************************************************
        public Int32 FindSymbolWin(Int32 posa, Int32 posb, Int32 posc, Int32 posd, Int32 pose, ref Int32 award, ref Int32 length, Int32 winLineIndex, int[,] multiplierWindow)
        {
            Int32 a;
            UInt32 sym_a, sym_b, sym_c, sym_d, sym_e, sym_win;
            Int32 max_win = 0;
            Int32 maxWinMultiplied = 0;
            Int32 multiplier;




            sym_a = ((posa == LOGIC_SYMBOL_WILD) ? (UInt32)(wildMask) : (UInt32)(1 << posa));
            sym_b = ((posb == LOGIC_SYMBOL_WILD) ? (UInt32)(wildMask) : (UInt32)(1 << posb));
            sym_c = ((posc == LOGIC_SYMBOL_WILD) ? (UInt32)(wildMask) : (UInt32)(1 << posc));
            sym_d = ((posd == LOGIC_SYMBOL_WILD) ? (UInt32)(wildMask) : (UInt32)(1 << posd));
            sym_e = ((pose == LOGIC_SYMBOL_WILD) ? (UInt32)(wildMask) : (UInt32)(1 << pose));

            length = 0;

            // 3 of a kind wins only
            sym_win = (sym_a & sym_b & sym_c);
            if (sym_win != 0)
            {
                for (a = 0; a < LOGIC_SYMBOL_MAX; a++)
                {
                    if ((award_table[a, 0] & sym_win) != 0)
                    {
                        multiplier = GetCumulativeMultiplier(multiplierWindow, winLineIndex, 3);
                        if (award_table[a, 3] * multiplier >= maxWinMultiplied)
                        {
                            maxWinMultiplied = award_table[a, 3] * multiplier;
                            max_win = award_table[a, 3];
                            award = a;
                            length = 3;
                        }
                    }
                }
            }
            else
                return (max_win);

            // 4 of a kind wins next
            sym_win = (sym_a & sym_b & sym_c & sym_d);
            if (sym_win != 0)
            {
                for (a = 0; a < LOGIC_SYMBOL_MAX; a++)
                {
                    if ((award_table[a, 0] & sym_win) != 0)
                    {
                        multiplier = GetCumulativeMultiplier(multiplierWindow, winLineIndex, 4);
                        if (award_table[a, 4] * multiplier >= maxWinMultiplied)
                        {
                            maxWinMultiplied = award_table[a, 4] * multiplier;
                            max_win = award_table[a, 4];
                            award = a;
                            length = 4;
                        }
                    }
                }
            }
            else
                return (max_win);

            // 5 of a kind wins 
            sym_win = (sym_a & sym_b & sym_c & sym_d & sym_e);
            if (sym_win != 0)
            {
                for (a = 0; a < LOGIC_SYMBOL_MAX; a++)
                {
                    if ((award_table[a, 0] & sym_win) != 0)
                    {
                        multiplier = GetCumulativeMultiplier(multiplierWindow, winLineIndex, 5);
                        if (award_table[a, 5] * multiplier >= maxWinMultiplied)
                        {
                            maxWinMultiplied = award_table[a, 5] * multiplier;
                            max_win = award_table[a, 5];
                            award = a;
                            length = 5;
                        }
                    }
                }
            }

            return max_win;
        }



    }
}
