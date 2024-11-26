using System.Collections.Generic;
using System.Linq;
using Service.LogicCommon;

namespace Service.PlayCheckCommon
{
    public class MappedSpin : Feature
    {
        public List<List<SymbolData>> reelWindow;
        public List<List<SymbolData>> reelWindowInRows;

        public bool renderGameMode = true;

        public MappedSpin()
        {
        }

        // private Spin spin;
        // private Dictionary<string, string> imageMap;

        public MappedSpin(Spin spin, ISymbolMapper symbolMapper)//, Dictionary<string, string> imageMap, bool isLinkNWin)
        {
            this.id = spin.id;
            this.type = spin.type;
            this.reelWindow = spin.reelWindow.Select(reel => reel.Select(symbol => symbolMapper.MapSymbol(spin, symbol)).ToList()).ToList();
            var rows = spin.reelWindow[0].Count();
            var cols = spin.reelWindow.Count();
            reelWindowInRows = new List<List<SymbolData>>();
            for(var y = 0; y < rows; y++) {
                var row = new List<SymbolData>();
                reelWindowInRows.Add(row);
                for(var x = 0; x < cols; x++) {
                    row.Add(this.reelWindow[x][y]);
                }
            }
        }
    }
    public class MappedFreeSpin : MappedSpin
    {
        public int spinIndex = 1;
        public int spinsAdded;
        public int spinsRemaining;

        public bool isFirstSpin { get {return spinIndex == 1;}}

        public MappedFreeSpin(FreeSpin spin, ISymbolMapper symbolMapper) : base(spin, symbolMapper)
        {
            this.spinsAdded = spin.spinsAdded;
            this.spinsRemaining = spin.spinsRemaining;
            
        }

        public MappedFreeSpin():base()
        {
        }
    }

    public class SymbolData
    {
        public string symbolImage;
        public string overlayImage;
        public int overlay;
        public int[] data;

        public override string ToString(){
            return $"{base.ToString()} {this.symbolImage}";
        } 
    }

    public interface ISymbolMapper
    {
        SymbolData MapSymbol(Feature feature, int[] symbolDataInput);
    }
    
}