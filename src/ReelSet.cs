using System.Collections.Generic;
using System.Linq;

namespace Service.LogicCommon
{
    public class ReelSet
    {
        public string name;
        public List<List<string>> reels;

        private List<List<int>> _reels;

        public void ConvertReels(Dictionary<string, Symbol> symbolsNameDict)
        {
            _reels = reels.Select((reel) => reel.Select((symbolName ) => symbolsNameDict[symbolName].id).ToList()).ToList();
        }

        public List<List< int>> GetReels()
        {
            return _reels;
        }
    }
}