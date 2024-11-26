
using System.Linq;

namespace Service.LogicCommon.Evaluation
{

    public class UltraWaysEvaluatorGOF
    {
        public GameConfig config;

        public UltraWaysEvaluatorGOF(GameConfig _config)
        {
            this.config = _config;
        }

        public UltraWaysWinDetailsGOF EvaluateResult(int coinValue, int[][] reelWindow, int[][] uwStatusWindow)
        {
            // dest.Clear();
            var dest = new UltraWaysWinDetailsGOF { symbolsInWin = new int[reelWindow.Length, reelWindow[0].Length] };



            var wildHits = new AnyWaysSymbolHitGroup();
            var symbolHits = new AnyWaysSymbolHitGroup[config.symbols.Count];

            for (var i = 0; i < symbolHits.Length; i++) symbolHits[i] = new AnyWaysSymbolHitGroup();

            CalculateForSymbol(coinValue, reelWindow, uwStatusWindow, ref wildHits, GetWild().id, ref dest);

            for (var s = GetWild().id + 1; s < config.symbols.Count; s++)
                CalculateForSymbol(coinValue, reelWindow, uwStatusWindow, ref symbolHits[s], s, ref dest);

            for (var i = 0; i < dest.winList.Count; i++)
            {
                var winItem = dest.winList[i];
                winItem.amount = winItem.award * winItem.ways * coinValue;
                dest.totalAmount += winItem.amount;
                // dest.wonOnHowManyWays += dest.winList[i].count;
                // dest.totalWinInCoins += dest.winList[i].totalWinInCoins;
                // dest.totalWinInCash += dest.winList[i].totalWinInCash;
            }

            dest.winList = dest.winList.Where(item => item.ways > 0).OrderByDescending(item => item.count).OrderByDescending(item => item.amount).ToList();


            // state.wins = dest.totalWinInCash;

            return dest;
        }

        private Symbol _wild;



        private Symbol GetWild()
        {
            if (_wild == null)
            {
                _wild = config.GetSymbolByName("Wild");
            }
            return _wild;
        }

        private void CalculateForSymbol(int coinValue, int[][] reelWindow, int[][] uwStatusWindow,
            ref AnyWaysSymbolHitGroup targetGroup, int symbolIndex, ref UltraWaysWinDetailsGOF destWinList)
        {
            ResetSymbolHits(ref targetGroup);

            var symbolCountLookup = CreateSymbolLookUp(reelWindow, uwStatusWindow, symbolIndex);

            targetGroup.symbolIndex = symbolIndex;
            targetGroup.count = 0;

            if (symbolIndex == GetWild().id)
                DoRecursive(coinValue, reelWindow, uwStatusWindow, symbolCountLookup, ref targetGroup, 0, 0, 1, 1, false,
                    ref destWinList);
            else
                DoRecursive(coinValue, reelWindow, uwStatusWindow, symbolCountLookup, ref targetGroup, 0, 0, 1, 1, true,
                    ref destWinList);

            if (targetGroup.count > 0)
                FindSymbolsThatArePartOfWin(reelWindow, uwStatusWindow, ref targetGroup, symbolIndex, ref destWinList);
        }

        private void DoRecursive(int coinValue, int[][] reelWindow, int[][] uwStatusWindow, int[,,] symbolCountLookup,
           ref AnyWaysSymbolHitGroup group, int currentReel, int winLengthBefore, int currentWinCount,
           int currentPureWildWinCount, bool subtractPureWildWins, ref UltraWaysWinDetailsGOF destWinList)
        {
            var hasFinished = false;


            if (currentReel >= reelWindow.Length)
            {
                hasFinished = true;
            }
            else
            {
                if (symbolCountLookup[currentReel, 0, 0] == 0 && symbolCountLookup[currentReel, 1, 0] == 0)
                    hasFinished = true;
            }


            if (hasFinished)
            {
                // gone through all of the symbols
                if (subtractPureWildWins)
                    currentWinCount -= currentPureWildWinCount;

                // wins[winLengthBefore] += currentWinCount;

                if (winLengthBefore >= 3)
                {
                    var award = config.GetSymbolById(group.symbolIndex).awards[winLengthBefore - 1];
                    if (group.symbolIndex == GetWild().id)
                        award = config.GetSymbolByName("High1").awards[winLengthBefore - 1];//_awardTable[group.symbolIndex, winLengthBefore];
                    var totalWonInCoins = award; // anyWaysGlobs.wins[winLengthBefore];
                    var totalWonInCash = totalWonInCoins * coinValue;

                    group.count += currentWinCount; // anyWaysGlobs.wins[winLengthBefore];
                    group.totalWonInCoins = totalWonInCoins;
                    group.totalWonInCash = totalWonInCash;
                    group.length = winLengthBefore;

                    //GameLog("Win %s, length %d, count %d, amount £%d.%02d", GameDefs_GetSymbolNameNoSpace(group->symbolIndex), winLengthBefore, anyWaysGlobs.wins[winLengthBefore], group->totalWonInCash / 100, group->totalWonInCash % 100);
                    // Console.WriteLine("Win {0}, length {1}, count {2}, amount ${3}.{4}", BandSet.GetSymbolName(group.symbolIndex, 0), winLengthBefore, currentWinCount, group.totalWonInCash / 100, group.totalWonInCash % 100);

                    //WinList_AddAnyWaysWinToWinList(anyWaysGlobs.targetWinList, group->symbolIndex, winLengthBefore, anyWaysGlobs.wins[winLengthBefore], totalWonInCoins);
                    //WinList_AddAnyWaysWinToWinList(anyWaysGlobs.targetWinList, group.symbolIndex, winLengthBefore, currentWinCount, totalWonInCoins);
                    destWinList.Add(coinValue, group.symbolIndex, winLengthBefore, currentWinCount,
                        totalWonInCoins);
                }

                return;
            }

            if (symbolCountLookup[currentReel, 0, 0] > 0)
                // normal reels (regular symbol or horizontal split)
                DoRecursive(coinValue, reelWindow, uwStatusWindow, symbolCountLookup, ref group, currentReel + 1, winLengthBefore + 1,
                    currentWinCount * symbolCountLookup[currentReel, 0, 0],
                    currentPureWildWinCount * symbolCountLookup[currentReel, 0, 1],
                    subtractPureWildWins, ref destWinList);

            if (symbolCountLookup[currentReel, 1, 0] > 0)
                // normal reels (regular symbol or vertical split)
                DoRecursive(coinValue, reelWindow, uwStatusWindow, symbolCountLookup, ref group, currentReel + 1, winLengthBefore + 2,
                    currentWinCount * symbolCountLookup[currentReel, 1, 0] * symbolCountLookup[currentReel, 1, 0],
                    currentPureWildWinCount * symbolCountLookup[currentReel, 1, 1] *
                    symbolCountLookup[currentReel, 1, 1],
                    subtractPureWildWins, ref destWinList);
        }

        private void FindSymbolsThatArePartOfWin(int[][] reelWindow, int[][] uwStatusWindow, ref AnyWaysSymbolHitGroup dest,
            int targetSymbol,
            ref UltraWaysWinDetailsGOF destWinList)
        {
            bool doCheck = false;

            for (int x = 0; x < reelWindow.Length; x++)
            {
                int count = 0;

                for (int y = 0; y < reelWindow[0].Length; y++)
                {
                    int symbolIndex = reelWindow[x][y];

                    if ((symbolIndex == targetSymbol) || (symbolIndex == GetWild().id))
                    {
                        count++;
                        dest.symbolHits[x, y] = 1;
                        destWinList.symbolsInWin[x, y] = 1;
                    }
                }

                if (count == 0)
                {
                    if (x == 2)
                        doCheck = true;             // we only want to check for our special case if there's no winning symbols on reel 3
                    else
                        doCheck = false;

                    break;      // leave the for loop
                }
            }


            if (doCheck)
            {
                CheckForSpecialCase1(reelWindow, uwStatusWindow, ref dest, targetSymbol, ref destWinList);
                CheckForSpecialCase2(reelWindow, uwStatusWindow, ref dest, targetSymbol, ref destWinList);
            }
        }

        private void CheckForSpecialCase2(int[][] reelWindow, int[][] uwStatusWindow, ref AnyWaysSymbolHitGroup dest, int targetSymbol, ref UltraWaysWinDetailsGOF destWinList)
        {
            // we only do this check if we stopped on Reel C, this means we must have a quad on reel A or B
            // as that's the only way to not have any winning symbols on Reel 3
            for (int y = 0; y < 3; y++)
            {
                int symbolIndex = reelWindow[1][y];

                if ((symbolIndex == targetSymbol) || (symbolIndex == GetWild().id))
                {
                    if (IsSymbolSplitVertically(uwStatusWindow, 1, y))                  // everything is ok if we've got a quad on reel B so we can leave
                        return;
                }
            }

            // we've not got a quad on reel B so we must have one on reel 1 (as we only do this check if we stopped at reel C)
            // so look for any single or doubles (or, 'not quads'), and take them out of the symbols in win list
            for (int y = 0; y < 3; y++)
            {
                int symbolIndex = reelWindow[0][y];

                if ((symbolIndex == targetSymbol) || (symbolIndex == GetWild().id))
                {
                    if (IsSymbolSplitVertically(uwStatusWindow, 0, y) == false)                 // we not have a quad on reel A
                    {
                        dest.symbolHits[0, y] = 0;
                        destWinList.symbolsInWin[0, y] = 0;
                    }
                }
            }
        }

        private void CheckForSpecialCase1(int[][] reelWindow, int[][] uwStatusWindow, ref AnyWaysSymbolHitGroup dest, int targetSymbol, ref UltraWaysWinDetailsGOF destWinList)
        {
            // we only do this check if we stopped on Reel 3, this means we must have a quad on reel 1 or 2
            // as that's the only way to not have any winning symbols on Reel 3
            for (int y = 0; y < 3; y++)
            {
                int symbolIndex = reelWindow[0][y];

                if ((symbolIndex == targetSymbol) || (symbolIndex == GetWild().id))
                {
                    if (IsSymbolSplitVertically(uwStatusWindow, 0, y))                  // everything is ok if we've got a quad on reel 1 so we can leave
                        return;
                }
            }

            // we've not got a quad on reel 1 so we must have one on reel 2 (as we only do this check if we stopped at reel 3)
            // so look for any single or doubles (or, 'not quads'), and take them out of the symbols in win list
            for (int y = 0; y < 3; y++)
            {
                int symbolIndex = reelWindow[1][y];

                if ((symbolIndex == targetSymbol) || (symbolIndex == GetWild().id))
                {
                    if (IsSymbolSplitVertically(uwStatusWindow, 1, y) == false)                 // we have not a quad on reel 2
                    {
                        dest.symbolHits[1, y] = 0;
                        destWinList.symbolsInWin[1, y] = 0;
                    }
                }
            }
        }

        private int[,,] CreateSymbolLookUp(int[][] reelWindow, int[][] uwStatusWindow, int targetSymbol)
        {
            var symbolCountLookup = new int[reelWindow.Length, 2, 2];
            for (var x = 0; x < reelWindow.Length; x++)
            {
                symbolCountLookup[x, 0, 0] = 0;
                symbolCountLookup[x, 0, 1] = 0;
                symbolCountLookup[x, 1, 0] = 0;
                symbolCountLookup[x, 1, 1] = 0;
            }


            for (var x = 0; x < reelWindow.Length; x++)
                for (var y = 0; y < reelWindow[0].Length; y++)
                {
                    var symbolIndex = reelWindow[x][y];

                    if (!DoesSymbolMatch(targetSymbol, symbolIndex)) continue;

                    var symbolCount = 1;
                    if (IsSymbolSplitHorizontally(uwStatusWindow, x, y)) symbolCount = 2; // add 2 symbols for a horizontal split


                    if (IsSymbolSplitVertically(uwStatusWindow, x, y))
                        symbolCountLookup[x, 1, 0] += symbolCount;
                    else
                        symbolCountLookup[x, 0, 0] += symbolCount;


                    if (symbolIndex != GetWild().id) continue;

                    if (IsSymbolSplitVertically(uwStatusWindow, x, y))
                        symbolCountLookup[x, 1, 1] += symbolCount;
                    else
                        symbolCountLookup[x, 0, 1] += symbolCount;
                }

            return symbolCountLookup;
        }

        private static bool IsSymbolSplitVertically(int[][] uwStatusWindow, int x, int y)
        {
            var retVal = false;

            switch ((UltraWaysStatus)uwStatusWindow[x][y])
            {
                case UltraWaysStatus.SINGLE:
                    retVal = false;
                    break;
                case UltraWaysStatus.DOUBLE:
                    retVal = false;
                    break;
                case UltraWaysStatus.QUAD:
                    retVal = true;
                    break;
            }

            return retVal;
        }

        private static bool IsSymbolSplitHorizontally(int[][] uwStatusWindow, int x, int y)
        {
            var retVal = false;

            switch ((UltraWaysStatus)uwStatusWindow[x][y])
            {
                case UltraWaysStatus.SINGLE:
                    retVal = false;
                    break;
                case UltraWaysStatus.DOUBLE:
                    retVal = true;
                    break;
                case UltraWaysStatus.QUAD:
                    retVal = true;
                    break;
            }

            return retVal;
        }

        private bool DoesSymbolMatch(int symbolA, int symbolB)
        {
            if (symbolA == symbolB)
                return true;

            Symbol wild = GetWild();
            if (symbolB == wild.id)
                if (Maths.IsSingleBitSet((int)wild.mask, symbolA))
                    return true;

            return false;
        }

        private void ResetSymbolHits(ref AnyWaysSymbolHitGroup targetGroup)
        {
            for (var y = 0; y < targetGroup.symbolHits.GetLength(1); y++)
                for (var x = 0; x < targetGroup.symbolHits.GetLength(0); x++)
                    targetGroup.symbolHits[x, y] = 0;

            targetGroup.count = 0;
            targetGroup.length = 0;
            targetGroup.symbolIndex = -1;
        }
    }

    public class AnyWaysSymbolHitGroup
    {
        public int count;
        public int length;
        public int[,] symbolHits = new int[5, 3];

        public int symbolIndex;
        public int totalWonInCash;
        public int totalWonInCoins;
    }
    public static class Maths
    {
        // *******************************************************************************
        public const int LOGICMATHS_BIT_WIDTH = 5;
        public const int LOGICMATHS_BIT_HEIGHT = 3;


        // *******************************************************************************
        public static void ConvertMaskBitToXY(int pos, ref int x, ref int y)
        {
            var nx = pos % LOGICMATHS_BIT_WIDTH;
            var ny = pos / LOGICMATHS_BIT_WIDTH;

            x = nx;
            y = ny;
        }

        // *******************************************************************************
        public static int ConvertXYToMask(int x, int y)
        {
            return y * LOGICMATHS_BIT_WIDTH + x;
        }

        // *******************************************************************************
        public static void SetBit(ref int destPattern, int x, int y)
        {
            var n = ConvertXYToMask(x, y);
            destPattern |= 1 << n;
        }

        // *******************************************************************************
        public static void ClearBit(ref int destPattern, int x, int y)
        {
            var n = ConvertXYToMask(x, y);
            destPattern &= ~(1 << n);
        }

        // *******************************************************************************
        public static void SetSingleBit(ref int destPattern, int n)
        {
            destPattern |= 1 << n;
        }

        // *******************************************************************************
        public static void ClearSingleBit(ref int destPattern, int n)
        {
            destPattern &= ~(1 << n);
        }

        // *******************************************************************************
        public static void ClearAllBits(ref int destPattern)
        {
            destPattern = 0;
        }

        // *******************************************************************************
        public static void SetAllBits(ref int destPattern)
        {
            destPattern = -1;
        }

        // *******************************************************************************
        public static void ToggleBit(ref int destPattern, int x, int y)
        {
            var n = ConvertXYToMask(x, y);
            destPattern ^= 1 << n;
        }

        // *******************************************************************************
        public static bool IsBitSet(int destPattern, int x, int y)
        {
            var n = ConvertXYToMask(x, y);
            if ((destPattern & (1 << n)) != 0) return true;

            return false;
        }

        // *******************************************************************************
        public static bool IsSingleBitSet(int destPattern, int n)
        {
            if ((destPattern & (1 << n)) != 0) return true;

            return false;
        }


        // *******************************************************************************
        // *******************************************************************************
        // *******************************************************************************
    }

}