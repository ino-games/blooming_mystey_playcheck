using System;
using System.Collections.Generic;
using System.Linq;

namespace Service.LogicCommon.Evaluation
{


    public class UltraWaysEvaluator
    {
        public GameConfig config;

        public UltraWaysEvaluator(GameConfig _config)
        {
            this.config = _config;
        }

        public UltraWaysWinDetails EvaluateResult(int coinValue, int[][] reelWindow, int[][] uwStatusWindow)
        {
            var symbolGroupsPerReel = GetSymbolGroupsPerReel(reelWindow, uwStatusWindow);

            var root = new EvaluationNode
            {
                layer = -1,
                mask = 0,
                ways = 1,
                symbol = -1
            };

            BuildTree(reelWindow, new List<EvaluationNode> { root }, symbolGroupsPerReel);
            root.ways = 0;

            var evaluatedNodes = EvaluateTree(root);

            //PrintEvaluationTree(root);

            return GetWinDetailsFromEvaluatedNodes(coinValue, evaluatedNodes);
        }

        private List<Dictionary<string, SymbolGroup>> GetSymbolGroupsPerReel(int[][] reelWindow, int[][] uwStatusWindow)
        {
            var symbolGroupsPerReel = new List<Dictionary<string, SymbolGroup>>();

            var reelWindowMasks = reelWindow.Select(reel => reel.Select(symbol => config.GetSymbolById(symbol).mask).ToArray()).ToArray();

            for (var x = 0; x < reelWindow.Length; x++)
            {
                Dictionary<string, SymbolGroup> item = new Dictionary<string, SymbolGroup>();
                symbolGroupsPerReel.Add(item);
                var reel = reelWindowMasks[x];
                for (var y = 0; y < reel.Length; y++)
                {
                    var mask = reel[y];
                    var symbol = reelWindow[x][y];
                    var ultraWaysStatus = uwStatusWindow[x][y];
                    var key = $"{mask}_{ultraWaysStatus}";

                    if (item.ContainsKey(key))
                        item[key].count += 1;
                    else
                        item[key] = new SymbolGroup
                        {
                            symbol = symbol,
                            count = 1,
                            mask = mask,
                            ultraWaysStatus = (UltraWaysStatus)ultraWaysStatus
                        };
                }
            }
            return symbolGroupsPerReel;
        }

        private UltraWaysWinDetails GetWinDetailsFromEvaluatedNodes(int coinValue, List<EvaluationNode> evaluatedNodes)
        {

            int totalAmount = 0;

            var nodeDictionary = new Dictionary<string, EvaluationNode>();

            foreach (var node in evaluatedNodes)
            {
                var key = $"{node.award.symbolId}_{node.award.count}";
                if (nodeDictionary.ContainsKey(key))
                {
                    var existingNode = nodeDictionary[key];
                    existingNode.ways += node.ways;
                    existingNode.wins += node.wins;
                }
                else
                {
                    nodeDictionary.Add(key, node);
                }
            }


            var winList = nodeDictionary.Keys.Select(key =>
            {
                var node = nodeDictionary[key];
                UltraWaysWinItem winItem = new UltraWaysWinItem
                {
                    symbol = node.award.symbolId,
                    count = node.award.count,
                    award = node.award.amount,
                    amount = node.award.amount * node.ways * coinValue,
                    ways = node.ways
                };
                totalAmount += winItem.amount;
                return winItem;
            }).OrderByDescending(node => node.amount).ToList();
            var winDetails = new UltraWaysWinDetails { winList = winList, totalAmount = totalAmount };
            return winDetails;
        }

        private struct EvalNodeSymbolGroup
        {
            public EvaluationNode node;
            public SymbolGroup group;
        }

        private void BuildTree(int[][] reelWindow, List<EvaluationNode> currentLayer, List<Dictionary<string, SymbolGroup>> symbolGroupsPerReel)
        {
            for (var reelIndex = 0; reelIndex < reelWindow.Length; reelIndex++)
            {
                if (currentLayer.Count == 0) break;

                var splitQuads = ReelHasWildQuadAndAnother(symbolGroupsPerReel[reelIndex]);
                var nextLayer = new List<EvaluationNode>();
                foreach (var parent in currentLayer)
                {
                    var quadParents = new List<EvalNodeSymbolGroup>();
                    var parentIsRoot = parent.symbol == -1;
                    foreach (var symbolGroupKey in symbolGroupsPerReel[reelIndex].Keys)
                    {
                        var symbolGroup = symbolGroupsPerReel[reelIndex][symbolGroupKey];
                        var mask = symbolGroup.mask;
                        uint aggregatedMask = parentIsRoot ? mask : (parent.mask & mask);
                        if (aggregatedMask == 0) continue;

                        var count = symbolGroup.count;
                        var child = new EvaluationNode(parent, aggregatedMask);

                        if (splitQuads && symbolGroup.ultraWaysStatus == UltraWaysStatus.QUAD)
                        {
                            child.ways *= 2 * count;

                            SetAward(child);
                            child.AddToParent();
                            quadParents.Add(new EvalNodeSymbolGroup{
                                node = child, 
                                group = symbolGroup
                            });
                        }
                        else
                        {
                            if (symbolGroup.ultraWaysStatus == UltraWaysStatus.QUAD)
                            {
                                child.layer += 1;
                                child.ways *= Pow(2 * count, 2);
                            }
                            else if (symbolGroup.ultraWaysStatus == UltraWaysStatus.DOUBLE)
                            {
                                child.ways *= 2 * count;
                            }
                            else
                            {
                                child.ways *= count;
                            }

                            SetAward(child);

                            child.AddToParent();
                            nextLayer.Add(child);
                        }
                    }

                    // Special handling if there are both wild quads and other symbol quads on the same reel.
                    foreach (var parentItem in quadParents)
                    {
                        foreach (var item in quadParents)
                        {
                            var aggregatedMask = parentItem.node.mask & item.node.mask;
                            if (aggregatedMask == 0) continue;

                            var child = new EvaluationNode(parentItem.node, aggregatedMask);
                            child.ways *= 2 * item.group.count;

                            SetAward(child);
                            child.AddToParent();
                            nextLayer.Add(child);
                        }
                    }
                }

                currentLayer = nextLayer;
            }

        }

        private void SetAward(EvaluationNode node)
        {
            node.award = GetAward(node.mask, node.layer);
            node.count = node.award.count;
            node.symbol = node.award.symbolId;
        }

        private bool ReelHasWildQuadAndAnother(Dictionary<string, SymbolGroup> symbolGroups)
        {
            var hasWildQuad = false;
            var hasOtherQuad = false;
            foreach (var symbol in symbolGroups.Values.
                Where(symbolGroup => symbolGroup.ultraWaysStatus == UltraWaysStatus.QUAD).
                Select(symbolGroup => config.GetSymbolById(symbolGroup.symbol)))
            {
                if (symbol.isWild)
                    hasWildQuad = true;
                else
                    hasOtherQuad = true;
            }
            return hasWildQuad && hasOtherQuad;
        }

        private SymbolAward GetAward(uint symbolCombination, int layerIndex)
        {
            var winAmount = 0;
            var winningSymbol = -1;
            for (var symbolId = 0; symbolId < config.symbols.Count; symbolId++)
            {
                if ((config.symbols[symbolId].mask & symbolCombination) == 0) continue;
                winningSymbol = symbolId;
                var amount = config.symbols[symbolId].awards[layerIndex];
                if (amount == 0 || amount < winAmount)
                {
                    if (amount > 0)
                        break;
                    continue;
                }


                winAmount = amount;
                winningSymbol = symbolId;
                break;
            }
            return new SymbolAward
            {
                mask = symbolCombination,
                symbolId = winningSymbol,
                amount = winAmount,
                count = layerIndex + 1
            };
        }

        private int Pow(int value, int powerOf)
        {
            var result = 1;
            for (int i = 0; i < powerOf; i++)
                result *= value;
            return result;
        }

        private void PrintEvaluationTree(EvaluationNode node)
        {
            foreach (var child in node.children)
                PrintEvaluationTree(child);

            if (node.wins > 0)
            {
                PrintNode(node);
            }
        }

        private void PrintNode(EvaluationNode node)
        {
            Console.WriteLine($"Mask: {node.mask}, ways: {node.ways}, symbol: {node.award.symbolId}, wins: {node.wins}, count: {node.award.count}, award: {node.award.amount}");
        }

        private List<EvaluationNode> EvaluateTree(EvaluationNode node)
        {
            IEnumerable<EvaluationNode> r = new List<EvaluationNode>();
            foreach (var child in node.children)
                r = r.Concat(EvaluateTree(child));

            r = AggregateEvaluatedNodes(r);

            var results = r.ToList();

            var evaluatedNode = EvaluateNode(node, r);
            if (evaluatedNode.ways > 0 && evaluatedNode.award.amount > 0)
                results.Add(evaluatedNode);

            return results;
        }

        private IEnumerable<EvaluationNode> AggregateEvaluatedNodes(IEnumerable<EvaluationNode> evaluatedNodes)
        {
            var mergedDictionary = new Dictionary<string, EvaluationNode>();
            foreach (var evaluatedNode in evaluatedNodes)
            {
                var key = $"{evaluatedNode.award.symbolId}_{evaluatedNode.award.count}";
                if (mergedDictionary.ContainsKey(key))
                {
                    mergedDictionary[key].ways += evaluatedNode.ways;
                }
                else
                {
                    mergedDictionary.Add(key, evaluatedNode);
                }
            }
            return mergedDictionary.Values;
        }

        private EvaluationNode EvaluateNode(EvaluationNode node, IEnumerable<EvaluationNode> results)
        {
            if (results.Count() > 0)
            {
                if (results.Any(evaluatedNode => node.symbol == evaluatedNode.symbol))
                {
                    node.ways = 0;
                }
            }
            return node;
        }
    }

    public enum UltraWaysStatus
    {
        SINGLE,
        DOUBLE,
        QUAD
    }
    internal class SymbolAward
    {
        public uint mask;
        public int symbolId;
        public int amount;
        public int count;
    }
    internal class EvaluationNode
    {
        public uint mask;

        public int symbol;
        public int layer;
        public EvaluationNode parent;
        public int count;
        public int ways;
        public List<EvaluationNode> children = new List<EvaluationNode>();
        internal SymbolAward award;
        internal int wins = 0;
        internal bool includeInWin = false;

        public EvaluationNode()
        {

        }

        public EvaluationNode(EvaluationNode _parent, uint _mask) {
            parent = _parent;
            mask = _mask;
            layer = parent.layer + 1;
            ways = parent.ways;
        }

        public void AddToParent()
        {
            parent.AddChild(this);
        }

        public void AddChild(EvaluationNode child)
        {
            children.Add(child);
        }

    }

    public class SymbolGroup
    {
        public int symbol;
        public int count;
        public uint mask;
        public UltraWaysStatus ultraWaysStatus;
    }
}
