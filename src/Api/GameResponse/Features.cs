using System.Collections.Generic;
using System.Linq;
using Service.LogicCommon.Win;

namespace Service.LogicCommon
{

    public class FeatureTypes
    {

        public const string Spin = "spin";
        public const string InitFreeSpins = "initFreeSpins";
        public const string FreeSpin = "freeSpin";
        public const string PickAction = "pickAction";
        public const string PickResult = "pickResult";
        public const string RequestNextStage = "requestNextStage";
        public const string Payout = "payout";
        public const string Collection = "collection";
        public const string ItemCollection = "itemCollection";
        public const string BuyBonus = "buyBonus";
        public const string GambleForBonus = "gambleForBonus";
        public static List<string> GetTypes()
        {
            List<string> constansts = new List<string>();
            foreach (var constant in typeof(FeatureTypes).GetFields())
            {
                if (constant.IsLiteral && !constant.IsInitOnly)
                {
                    constansts.Add((string)constant.GetValue(null));
                }
            }
            return constansts;

        }
    }

    public class Feature
    {
        private string _type = "";
        public virtual string type { get => _type; set => _type = value; }
        public string id = "";

        public override string ToString(){
            return $"{base.ToString()} {this.id}";
        }  
    }

    public class RequestNextStage : Feature
    {
        public override string type { get => FeatureTypes.RequestNextStage; }
    }

    public class Payout : Feature
    {
        public override string type { get => FeatureTypes.Payout; }
        public long win;
    }

    public class PayLinePayout : Payout
    {
        public WinDetails<PayLineWinItem> winDetails;
    }

    public class WaysPayout : Payout
    {
        public WinDetails<WaysWinItem> winDetails;
    }

    public class ScatterPayout : Payout
    {
        public int symbolId;
        public int symbolCount;
        public List<List<int>> reelWindow;
    }

    public class Spin : Feature
    {
        
        public override string type { get => FeatureTypes.Spin; }
        public List<List<int[]>> reelWindow;
        // Only for data extract
        public int reelSetId;
        public List<int> stopPositions;

    }

    public class InitFreeSpins : Feature
    {
        public override string type { get => FeatureTypes.InitFreeSpins; }
        public int spinCount;
    }

    /**
        Any type of grouped spins, free spins, linknwin spins or anyother bonus with multiple spin stages.
    */
    public class FreeSpin : Spin
    {
        public override string type { get => FeatureTypes.FreeSpin; }

        public int spinsRemaining;
        public int spinsAdded = 0;
    }

    /** 
        Global collections, can be used for bars filling up or multipliers
    */
    public class Collection : Feature
    {
        public override string type { get => FeatureTypes.Collection; }
        public int value;
        public int update = 0;
    }

    public class ItemCollection: Feature
    {
        public override string type { get => FeatureTypes.ItemCollection; }
        public object[] items;

        public T[] GetItems<T>() { return items.Select(item => (T)item).ToArray();}
    }

    public class PickAction : Feature
    {

        public override string type { get => FeatureTypes.PickAction; }
        public long[] options;
        public int pickCount;
    }

    public class PickResult : Feature
    {

        public override string type { get => FeatureTypes.PickResult; }
        public int picksAdded;
        public int picksRemaining;

        // For playcheck and resume
        public long[] optionsPicked;
        public long cost = 0;
    }

    public class GambleForBonus : Feature
    {
        
        public override string type { get => FeatureTypes.GambleForBonus; }
        public bool gambleWon;
        // Mostly for playcheck
        public long bet;
        public long cost;
        public int chance;
    }

    public class BuyBonus : Feature
    {
        
        public override string type { get => FeatureTypes.BuyBonus; }
        // Mostly for playcheck
        public long bet;
        public long cost;
    }

}