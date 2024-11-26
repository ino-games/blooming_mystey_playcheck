
using System.Collections.Generic;
using Service.LogicCommon;

namespace Service.Logic {

    public class InternalState
    {
        internal long bet;
        internal long coinValue;
        internal bool buyBonus;
        internal decimal buyBonusCost;
        internal Dictionary<long, PersistentData> persistentDataMap = null;
        internal PersistentData persistentData;
        internal PersistentData initialPersistentData;
        internal Force force = null;
        internal long cashWin;
        internal long coinWin;
        internal BaseGameState baseGameState = new BaseGameState();
        internal FreeSpinsState freeSpinsState;

        public List<Feature> features = new List<Feature>();
    }
}