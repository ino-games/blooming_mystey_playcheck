using System;
using System.Collections.Generic;
using System.Linq;
using Service.Logic;
using Service.LogicCommon;
using static Service.LogicCommon.UltraWaysUtils;

namespace Service.LogicCommon.Utils
{

    public class ReelUtils {

        public static int CountSymbolOnReels(List<List<int>> reelWindow, int symbol)
        {
            var count = 0;

            for (var x = 0; x < reelWindow.Count; x++)
            {
                var reel = reelWindow[x]; 
                for (var y = 0; y < reel.Count; y++)
                {
                    if (reel[y] == symbol)
                        count++;
                }
            }

            return count;
        }
        public static int CountSymbolOnReels(List<List<int[]>> reelWindow, int symbol)
        {
            var count = 0;

            for (var x = 0; x < reelWindow.Count; x++)
            {
                var reel = reelWindow[x]; 
                for (var y = 0; y < reel.Count; y++)
                {
                    if (reel[y][0] == symbol)
                        count++;
                }
            }

            return count;
        }
        public static int CountSymbolOnReels(int[,] reelWindow, int symbol)
        {
            var count = 0;

            for (var x = 0; x < reelWindow.GetLength(0); x++)
            {
                for (var y = 0; y < reelWindow.GetLength(1); y++)
                {
                    if (reelWindow[x, y] == symbol)
                        count++;
                }
            }

            return count;
        }

        public static int CountSymbolCondition<T>(T[,] reelWindow, Func<T, bool> checkSymbolCondition)
        {
            var count = 0;

            for (var x = 0; x < reelWindow.GetLength(0); x++)
            {
                for (var y = 0; y < reelWindow.GetLength(1); y++)
                {
                    if (checkSymbolCondition(reelWindow[x, y]))
                        count++;
                }
            }

            return count;
        }

        public static int CountSymbolCondition<T>(List<List<T>> reelWindow, Func<T, bool> checkSymbolCondition)
        {
            var count = 0;

            for (var x = 0; x < reelWindow.Count; x++)
            {
                var reel = reelWindow[x];
                for (var y = 0; y < reel.Count; y++)
                {
                    if (checkSymbolCondition(reel[y]))
                        count++;
                }
            }

            return count;
        }

        public static int GetSymbol(List<List<int>> reelSet, int x, int y)
        {
            int pos = 0;
            int reelLength = reelSet[x].Count;

            if (y < reelLength)
                pos = y;
            else
                pos = y % reelLength;

            return reelSet[x][pos];
        }

        public static int CountSymbolOnReels(int[,] reelWindow, UWSymbolState[,] symbolStatusWindow, int symbol)
        {
            var count = 0;

            for (var x = 0; x < reelWindow.GetLength(0); x++)
            {
                for (var y = 0; y < reelWindow.GetLength(1); y++)
                {
                    if (reelWindow[x, y] == symbol) {
                        var symbolStatus = symbolStatusWindow[x, y];
                        if(symbolStatus == UWSymbolState.NORMAL)
                            count++;
                        else if(symbolStatus == UWSymbolState.DOUBLE)
                            count += 2;
                        else if(symbolStatus == UWSymbolState.QUAD)
                            count += 4;
                    }
                }
            }

            return count;
        }

        internal static int GetSplitSum(UWSymbolState[,] reelWindow)
        {
            var sum = 0;

            for (var x = 0; x < reelWindow.GetLength(0); x++)
            {
                for (var y = 0; y < reelWindow.GetLength(1); y++)
                {
                    UWSymbolState symbolState = reelWindow[x, y];
                    if (symbolState == UWSymbolState.NORMAL)
                        sum++;
                    else if (symbolState == UWSymbolState.DOUBLE)
                        sum += 2;
                    else if(symbolState == UWSymbolState.QUAD)
                        sum += 4;
                }
            }
            return sum;
        }

        internal static List<List<int>> GetDiff(List<List<int>> reelWindowA, List<List<int>> reelWindowB)
        {
            
            var result = ObjectUtils.Clone(reelWindowA);

            for(var x = 0; x < result.Count; x++)
                for(var y = 0; y < result[x].Count; y++) 
                    result[x][y] = reelWindowB[x][y] - reelWindowA[x][y];
                

            return result;
        }

        internal static List<List<int[]>> GetReelWindow(int[,] reelWindowInput, int[,] overlayWindowInput)
        {
            var reelWindow = new List<List<int[]>>();
            for(var x = 0; x < reelWindowInput.GetLength(0); x++) {
                var reel = new List<int[]>();
                reelWindow.Add(reel);
                for(var y = 0; y < reelWindowInput.GetLength(1); y++) {
                    reel.Add(new int[]{reelWindowInput[x, y], overlayWindowInput[x, y]});
                }
            }
            
            return reelWindow;
        }

        internal static List<List<int[]>> GetReelWindow(int[,] reelWindowInput)
        {
            var reelWindow = new List<List<int[]>>();
            for(var x = 0; x < reelWindowInput.GetLength(0); x++) {
                var reel = new List<int[]>();
                reelWindow.Add(reel);
                for(var y = 0; y < reelWindowInput.GetLength(1); y++) {
                    reel.Add(new int[]{reelWindowInput[x, y]});
                }
            }
            
            return reelWindow;
        }

        internal static List<List<T>> MapReelWindow<T>(List<List<int[]>> reelWindow, Func<int[], T> mapperFn)
        {
            return reelWindow.Select(reel => reel.Select(mapperFn).ToList()).ToList();
        }

        internal static List<List<int[]>> MapToReelWindow<T>(List<List<T>> reelWindow, Func<T, int[]> mapperFn)
        {
            return reelWindow.Select(reel => reel.Select(mapperFn).ToList()).ToList();
        }

        internal static int OverlayWindowMapper(int[] symbol)
        {
            return symbol[1];
        }
    }
}