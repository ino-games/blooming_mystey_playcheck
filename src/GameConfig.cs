using System.Collections.Generic;
using System.Linq;

namespace Service.LogicCommon
{
    public class GameConfig
    {
        public int nColumns;
        public int nRows;
        public List<Symbol> symbols;
        public List<List<int>> payLines;
        public List<List<List<int>>> reelSets;

        private readonly Dictionary<string, Symbol> _symbolsNameDict = new Dictionary<string, Symbol>();
        private readonly Dictionary<int, Symbol> _symbolsIdDict = new Dictionary<int, Symbol>();
       
        public void Initialize()
        {
            symbols.ForEach((symbol) =>
            {
                _symbolsNameDict.Add(symbol.name, symbol);
                _symbolsIdDict.Add(symbol.id, symbol);
            });
            SetSymbolMasks();
        }

        public Symbol GetSymbolById(int symbolId)
        {
            return _symbolsIdDict[symbolId];
        }

        public Symbol GetSymbolByName(string symbolName)
        {
            return _symbolsNameDict[symbolName];
        }

        private void SetSymbolMasks()
        {
            
            foreach (var symbol in symbols.Where(symbol => !symbol.isWild))
            {
                symbol.mask = (uint) (1 << symbol.id);
            }

            var symbolsWithWildSubstitution = symbols.Where(symbol => symbol.wildSubstitution).ToList();
            if (symbolsWithWildSubstitution.Count == 0) return;
            var wildMask = symbolsWithWildSubstitution.Select(symbol => symbol.mask).Skip(1).Aggregate(symbolsWithWildSubstitution[0].mask, (a1, b1) => a1 | b1);

            foreach (var symbol in symbols.Where(symbol => symbol.isWild))
            {
                symbol.mask = wildMask;
            }
        }
    }
}