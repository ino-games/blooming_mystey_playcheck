using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using static Service.Logic.GameDefs;

namespace Service.Logic
{
    class SpinConfig
    {
        // *******************************************************************************

        // *******************************************************************************
        public Int32 spinType;
        public Int32 randomBonusHitRate;
        public bool allowScatters;
        public Int32 jackpotBonusHitRate;
        public Int32 guaranteedWinChance;


        // *******************************************************************************
        public SpinConfig()
        {
        }

        // *******************************************************************************
        public void SetForBaseGame()
        {
            spinType = GAME_MODE_BASEGAME;
            randomBonusHitRate = 40;
            allowScatters = true;
            jackpotBonusHitRate = 600;
            guaranteedWinChance = 0;
        }

        // *******************************************************************************
        public void SetForFreeSpin()
        {
            spinType = GAME_MODE_FREE_SPINS;
            randomBonusHitRate = 4;
            allowScatters = true;
            jackpotBonusHitRate = 600;
            guaranteedWinChance = 0;
        }

        // *******************************************************************************
        // *******************************************************************************
        // *******************************************************************************

    }
}
