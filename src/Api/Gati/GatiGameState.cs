using System.Collections.Generic;
using Service.Logic;
using Service.LogicCommon.Rng;

namespace Service.LogicCommon.Gati
{
    public class GatiGameState: GameStateBase
    {
        public Force force;
        internal List<GatiRandomNumber> randomNumbers = new List<GatiRandomNumber>();
    }
}