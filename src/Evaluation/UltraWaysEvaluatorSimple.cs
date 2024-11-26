using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.LogicCommon.Evaluation
{

    public class UltraWaysEvaluatorSimple
    {
        public GameConfig config;

        public UltraWaysEvaluatorSimple(GameConfig _config)
        {
            this.config = _config;
        }

        //TODO Handle the case where a shorter wild win is deducted from a 5oak instead of a 4oak because the 5oak way is evaluated first
        // Possible solution is to Evaluate all 5oak first, then all 4oak, then all 3oak and make the deductions like that
        public UltraWaysWinDetails EvaluateResult( int coinValue, int[][] reelWindow, int[][] uwStatusWindow)
        {
            var winDetails = new UltraWaysWinDetails();

            var payLines = new List<int[]>();

            int rows = reelWindow[0].Length;
            for (var a = 0; a < rows; a++)
                for(var b = 0; b < rows; b++)
                    for(var c = 0; c < rows; c++)
                        for(var d = 0; d < rows; d++)
                            for(var e = 0; e < rows; e++)
                                payLines.Add(new int[]{a, b, c, d, e});


            var evaluatedWays = new List<UltraWaysWinItemSimple>();
            foreach(var ways in payLines.Select(payLine => EvaluateWay(payLine, reelWindow, uwStatusWindow))) {
                foreach (var way in ways)
                    evaluatedWays.Add(way);
            }


            List<UltraWaysWinItemSimple> distinctWays = evaluatedWays.GroupBy(item => item.id).Select(g => g.First()).ToList();
            evaluatedWays = EvaluateWays(distinctWays, coinValue);
            

            // evaluatedWays = evaluatedWays.Where(payline => {
            //     var id = $"{payline.symbol}_{payline.id}";
                
            //     if(usedWays.Any(usedPayLine => usedPayLine.StartsWith(id))) {
            //         return false;
            //     }


            //     usedWays.Add(id); 
            //     return true;
            // }).ToList();


            winDetails.winList = AggregateWinItems(evaluatedWays);
            foreach(var winItem in winDetails.winList) {
                winDetails.totalAmount += winItem.amount;
            }

            return winDetails;
        }

        private List<UltraWaysWinItem> AggregateWinItems(IEnumerable<UltraWaysWinItem> winItems)
        {
            var mergedDictionary = new Dictionary<string, UltraWaysWinItem>();
            foreach(var winItem in winItems) {
                var key = $"{winItem.symbol}_{winItem.count}";
                if(mergedDictionary.ContainsKey(key)) {
                    mergedDictionary[key].ways += winItem.ways;
                    mergedDictionary[key].amount += winItem.amount;
                } else {
                    mergedDictionary.Add(key, winItem);
                }
            }
            return mergedDictionary.Values.OrderByDescending(winItem => winItem.count).OrderByDescending(winItem => winItem.amount).ToList();
        }

        public List<UltraWaysWinItemSimple> EvaluateWay(int[] way, int[][] reelWindow, int[][] uwStatusWindow)
        {
            var evaluatedWays = new List<UltraWaysWinItemSimple>();
            
            var symbolMasks = GetSymbolMasksForPayLine(way, reelWindow);
                     
            for (var symbolCount = 1; symbolCount <= symbolMasks.Count; symbolCount++)
            {
                var symbolCombination = AggregateSymbolMasks(symbolMasks, symbolCount); 
                string currentPayLineId = String.Join("", way.Take(symbolCount));

                var nDoubles = 0;
                var nQuads = 0;
                var ways = 1;
                for(var x = 0; x < symbolCount; x++) {
                    var y = way[x];
                    if(uwStatusWindow[x][y] == (int)UltraWaysStatus.DOUBLE)
                    {
                        ways *= 2;
                        nDoubles++;
                    }
                    else if(uwStatusWindow[x][y] == (int)UltraWaysStatus.QUAD)
                    {
                        ways *= 4;
                        nQuads++;
                    }
                }

                if (symbolCombination != 0)
                {
                    UltraWaysWinItemSimple item = null;
                    for (var symbolId = 0; symbolId < config.symbols.Count; symbolId++)
                    {
                        if ((config.symbols[symbolId].mask & symbolCombination) == 0) continue;
                        
                        var award = config.symbols[symbolId].awards[nQuads + symbolCount - 1];

                        item = new UltraWaysWinItemSimple { award = award, symbol = symbolId, layer=symbolCount, count = symbolCount + nQuads, ways = ways, id = currentPayLineId };
                        if(award > 0)
                            break; // Highest win found for this symbol count
                    }
                    if(item?.award > 0)
                        evaluatedWays.Add(item);
                }
            }

            return evaluatedWays;
        }

        public List<UltraWaysWinItemSimple> EvaluateWays(List<UltraWaysWinItemSimple> ways, int coinValue) {
            var groups = new Dictionary<int, List<UltraWaysWinItemSimple>>();
            //group by winItem.count then sort each group by award desc
            foreach(var way in ways) {
                if(!groups.ContainsKey(way.layer)) {
                    groups.Add(way.layer, new List<UltraWaysWinItemSimple>());
                }
                groups[way.layer].Add(way);
            }

            var groups2 = new Dictionary<int, List<UltraWaysWinItemSimple>>();
            foreach(var key in groups.Keys)
            {
                groups2.Add(key, groups[key].OrderBy(item => item.award).ToList());
            }

            groups = groups2;

            // Iterate all winItems of count x starting low, if one has an award, find an winItem with higher count and lower award and matching signature 
            // If winItem with lowerAward is found, deduct ways from that item. If lowerAward item get zero ways, remove it.
            // If no winitem with lower award was found, remove current winItem
            // If current winItem have award 0 remove it

            var results = new List<UltraWaysWinItemSimple>();

            IOrderedEnumerable<int> orderedKeys = groups.Keys.OrderBy(key => key);
            foreach (var key in orderedKeys) {
                foreach(var item in groups[key])
                {
                    if(item.award == 0 || item.ways == 0)
                        continue;

                    var shouldKeep = true;
                    // find one matching winItem in all the next layers if present
                    // compare the wins and deduct ways from lower win.
                    foreach(var nextKey in orderedKeys.Where(nextKey => nextKey > key))
                    {
                        var nextGroup = groups[nextKey].Where(it => it.id.StartsWith(item.id) && it.symbol == item.symbol);
                        if(nextGroup.Count() == 0)
                            continue;
                        var nextItem = nextGroup.First();
                        if(nextItem.award < item.award && nextItem.ways > 0) {
                            nextItem.ways -= item.ways;
                        } else {
                            shouldKeep = false;
                            break;
                        }
                    }
                    
                    if(shouldKeep)
                    {
                        item.amount = item.award * item.ways * coinValue;
                        results.Add(item);
                    }

                }
            }

            return results.OrderByDescending(item => item.amount).ToList();
        }

        private IReadOnlyList<uint> GetSymbolMasksForPayLine(IEnumerable<int> payLine, int[][] reelWindow)
        {
            return payLine.Select((row, col) => config.GetSymbolById(reelWindow[col][row]).mask).ToArray();
        }

        private static uint AggregateSymbolMasks(IReadOnlyList<uint> symbols, int symbolCount)
        {
            var aggregate = symbols[0];
            for (var i = 1; i < symbolCount; i++)
            {
                aggregate &= symbols[i];
            }

            return aggregate;
        }
    }

    public class UltraWaysWinItemSimple: UltraWaysWinItem {
        public string id;
        public int layer;
    }
}
