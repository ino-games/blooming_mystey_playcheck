using System.Collections.Generic;
using System.Linq;
using Service.LogicCommon.Win;

namespace Service.LogicCommon.Evaluation
{
    public class PayLineEvaluator<WinItemType> where WinItemType: PayLineWinItem, new()
    {
        public GameConfig config;

        public PayLineEvaluator(GameConfig _config)
        {
            this.config = _config;
        }

        public bool[][] GetLosingWindow(Win.WinDetails<WinItemType> winDetails, int[][] reelWindow)
        {
            var losingWindow = reelWindow.Select(reel => reel.Select(val => true).ToArray()).ToArray();

            foreach (var winItem in winDetails.winList)
            {
                for (var i = 0; i < winItem.symbolCount; i++)
                {
                    var payLine = config.payLines[winItem.payLineId];
                    losingWindow[i][payLine[i]] = false;
                }
            }
            

            return losingWindow;
        }

        public bool[][] GetWinningWindow(Win.WinDetails<WinItemType> winDetails, int[][] reelWindow)
        {
            return GetLosingWindow(winDetails, reelWindow).Select(reel => reel.Select(value => !value).ToArray()).ToArray();
        }

        public Win.WinDetails<WinItemType> EvaluateResult( long coinValue, int[][] reelWindow)
        {
            var winDetails = new Win.WinDetails<WinItemType> {};
            
            winDetails.winList = config.payLines.Select((payLine, index) =>
            {
                var winItem = EvaluatePayLine(index, payLine, reelWindow);
                winItem.winInCash *= coinValue;
                winDetails.cashWin += winItem.winInCash;
                return winItem;
            }).Where(evaluatedPayLine => evaluatedPayLine.winInCash > 0).ToList();

            return winDetails;
        }

        private WinItemType EvaluatePayLine(int payLineId, List<int> payLine, int[][] reelWindow) 
        {
            int winningSymbol = -1;
            int winAmount = 0;
            var winCount = 0;
            
            var symbolMasks = GetSymbolMasksForPayLine(payLine, reelWindow);            
            for (var symbolCount = 2; symbolCount <= symbolMasks.Count; symbolCount++)
            {
                var symbolCombination = AggregateSymbolMasks(symbolMasks, symbolCount); 
                if (symbolCombination != 0)
                {
                    for (var symbolId = 0; symbolId < config.symbols.Count; symbolId++)
                    {
                        if ((config.symbols[symbolId].mask & symbolCombination) == 0) continue;
                        var amount = config.symbols[symbolId].awards[symbolCount - 1];
                        if (amount == 0 || amount < winAmount)
                        {
                            if (amount > 0)
                                break;
                            continue;
                        }


                        winAmount = amount;
                        winningSymbol = symbolId;
                        winCount = symbolCount;
                        break;
                    }
                }
                else
                    break;
            }
            return new WinItemType(){winInCash = winAmount, symbolCount = winCount, symbolId = winningSymbol, payLineId = payLineId};
        }

        private IReadOnlyList<uint> GetSymbolMasksForPayLine(List<int> payLine, int[][] reelWindow)
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
}
