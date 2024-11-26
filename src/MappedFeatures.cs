using Service.LogicCommon;

namespace Service.PlayCheckCommon
{

    public class MappedBuyBonus: Feature {
        public bool gambleWon;
        // Mostly for playcheck
        public decimal bet;
        public decimal cost;
        public decimal costMultiplier;
        public MappedBuyBonus(BuyBonus feature) {
            this.type = feature.type;
            this.id = feature.id;
            this.bet = (decimal)feature.bet / 100m;
            this.cost = (decimal)feature.cost / 100m;
            this.costMultiplier = this.cost / this.bet;
        }
    }

    public class MappedGambleForBonus : Feature
    {
        public bool gambleWon;
        // Mostly for playcheck
        public decimal bet;
        public decimal cost;
        public decimal costMultiplier;
        public int chance;

        public MappedGambleForBonus(GambleForBonus feature)
        {
            this.type = feature.type;
            this.id = feature.id;
            this.chance = feature.chance;
            this.bet = (decimal)feature.bet / 100m;
            this.cost = (decimal)feature.cost / 100m;
            this.costMultiplier = (this.cost / this.bet);
            this.gambleWon = feature.gambleWon;
        }
    }

    public class MappedPickResult: Feature {

        public int picksAdded;
        public int picksRemaining;

        // For playcheck and resume
        public long[] optionsPicked;
        public decimal cost = 0;
        public MappedPickResult(PickResult feature) {
            type = feature.type;
            id = feature.id;
            picksAdded = feature.picksAdded;
            picksRemaining = feature.picksRemaining;
            optionsPicked = feature.optionsPicked;
            cost = (decimal) feature.cost / 100m;

        }
    }
}