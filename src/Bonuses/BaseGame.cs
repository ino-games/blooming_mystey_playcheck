using System;
using System.Linq;
using Service.LogicCommon;
using Service.LogicCommon.Rng;
using Service.LogicCommon.Utils;
using static Service.Logic.GameDefs;

namespace Service.Logic
{

    class BaseGameState
    {
        public Int32[] winningSymbolBias = new Int32[NUMBER_REELS * REEL_WINDOW];
        public Int32[,] ewFrameWindowCopy = new Int32[NUMBER_REELS, REEL_WINDOW];
        public Int32[,] fsFrameWindowCopy = new Int32[NUMBER_REELS, REEL_WINDOW];
        public Int32[,] bbFrameWindowCopy = new Int32[NUMBER_REELS, REEL_WINDOW];

        //ADDED
		public Int32[,]	frameWindowAtGameStart = new Int32[NUMBER_REELS, REEL_WINDOW];

        public Int32			twoScatterProgressAtGameStart;
        //ADDED
    }

    class BaseGame
    {
        // *******************************************************************************
        // This is like logic.c
        // *******************************************************************************




        // this will hold the state of a spin after scoring, 0/1==symbol position not part/part of a win respectively

        private Rng rng;
        private PayWays payWays;
        private GameSpin gameSpin;
        private ExplodingWilds explodingWilds;

        public BaseGame(Rng rng, PayWays payWays, GameSpin gameSpin, ExplodingWilds explodingWilds)
        {
            Console.WriteLine("Initialising BaseGame");
            this.rng = rng;
            this.payWays = payWays;
            this.gameSpin = gameSpin;
            this.explodingWilds = explodingWilds;
        }


        // *******************************************************************************
        public void DoSpin(InternalState internalState, GameResults thisGame, BaseGameResult baseGameResult)
        {
            long wins;

            baseGameResult.baseGameSpin.spinConfig.SetForBaseGame();
            wins = DoBaseGameSpin(internalState, baseGameResult, baseGameResult.baseGameSpin);
            gameSpin.TrackSpinsSinceLastWild(internalState, internalState.baseGameState, baseGameResult.baseGameSpin);
            thisGame.winsFromBaseGameInCoins = baseGameResult.baseGameSpin.baseSpinWinInCoins;
            thisGame.winsFromExplodingWildsInCoins = baseGameResult.baseGameSpin.explodingWildsWinInCoins;
            thisGame.winsFromBigHitInCoins = baseGameResult.baseGameSpin.bigHitWinInCoins;
        }

        public long DoBaseGameSpin(InternalState internalState, BaseGameResult baseGameResult, GameSpinState gameSpin)
        {
            long win;
            int addedWildCount, reSpinChance, outOf;
            Int32 addedScatterCount = 0;
            bool forcedTwoScatterReSpin = false;

            // reset our game spin params
            this.gameSpin.ResetGameSpin(gameSpin, 1, 0);
            gameSpin.spinConfig.spinType = GAME_MODE_BASEGAME;


            // cache active frame count, this is used by a number of the trigger functions below
            internalState.persistentData.activeFrameCount = this.gameSpin.GetActiveFrameCount(internalState);
            internalState.features.Add(new Collection { id = FeatureIds.Scatters, value = internalState.persistentData.twoScatterProgress });


            //assert(logicGlobs.activeFrameCount <= 11);
            //Meter_Bump(METER_BG_ACTIVE_COUNT_0 + logicGlobs.activeFrameCount);


            // these are nested by priority, from lowest hit rate to highest, and only one can be active currently
            if (!this.gameSpin.HandleFreeSpinsTrigger(internalState, baseGameResult, gameSpin))
                if (!this.gameSpin.HandleExplodingWildsTrigger(internalState, baseGameResult, gameSpin))
                    if (!this.gameSpin.HandleBigHitTrigger(internalState, baseGameResult, gameSpin))
                        this.gameSpin.HandleSmallWinTrigger(internalState, baseGameResult, gameSpin);


            if (!gameSpin.willDoBigHit)
            {
                // this bias table will keep track of available non-winning symbol positions
                this.gameSpin.FillBiasTable(internalState.baseGameState.winningSymbolBias, 1);

                if (internalState.buyBonus)
                {
                    gameSpin.bandset = LOGIC_BANDSET_LOSING;
                    this.gameSpin.SetSpinInReels(internalState, gameSpin, gameSpin.bandset);
                     internalState.features.Add(new Spin
                    {
                        id = FeatureIds.BaseGame,
                        reelWindow = ReelUtils.GetReelWindow(gameSpin.spinInReelWindow, internalState.persistentData.frameWindow),
                        reelSetId = gameSpin.bandset,
                        stopPositions = gameSpin.stopPositions.ToList()
                    });
                    addedWildCount = 0;
                }
                else
                {
                    // spin random reels
                    addedWildCount = this.gameSpin.SpinRandomReels(internalState, gameSpin, MAX_ALLOWABLE_FRAMES_BASEGAME, baseGameResult.willDoSmallWin, gameSpin.willDoBigHit, GAME_MODE_BASEGAME);
                    internalState.features.Add(new Spin
                    {
                        id = FeatureIds.BaseGame,
                        reelWindow = ReelUtils.GetReelWindow(gameSpin.spinInReelWindow, internalState.persistentData.frameWindow),
                        reelSetId = gameSpin.bandset,
                        stopPositions = gameSpin.stopPositions.ToList()
                    });
                    if (addedWildCount == 0)
                        gameSpin.willDoExplodingWilds = false;
                }

                gameSpin.winDetails = this.payWays.CalculateTotal(internalState, gameSpin.spinInReelWindow, gameSpin.spinInMultiplierWindow, NUMBER_WIN_LINES);
                win = gameSpin.winDetails.coinWin;


                this.gameSpin.RemovePayWaysWinsFromBiasTable(gameSpin.winDetails, internalState.baseGameState.winningSymbolBias);

                if (gameSpin.scatterCount >= 3)
                {
                    reSpinChance = 3;
                    outOf = 4;
                }
                else
                {
                    reSpinChance = 1;
                    outOf = 2;
                }

                 

                // new, forced 2 scatter re-spin to occur within first 10 spins, (or the next available, since other features may override this)
                if (internalState.persistentData.nearMissScattersOnSpin != -1 && gameSpin.scatterCount < 3)
                {
                    if (win == 0 && addedWildCount == 0 && gameSpin.willDoBigHit == false && !internalState.buyBonus)

                    {
                        if (internalState.persistentData.playedSpins >= internalState.persistentData.nearMissScattersOnSpin)
                        {
                            internalState.persistentData.nearMissScattersOnSpin = -1;     // switch off
                            forcedTwoScatterReSpin = true;
                            gameSpin.scatterCount = 2;

                            addedScatterCount = this.gameSpin.AddScattersWithReSpin(internalState, gameSpin, gameSpin.scatterCount);

                        }
                    }
                }

                if (forcedTwoScatterReSpin == false)
                {
                    if (win == 0 && addedWildCount == 0 && gameSpin.willDoBigHit == false && !internalState.buyBonus && gameSpin.scatterCount >= 2 && this.rng.XInY(internalState.force, reSpinChance, outOf))
                        addedScatterCount = this.gameSpin.AddScattersWithReSpin(internalState, gameSpin, gameSpin.scatterCount);
                    else
                        addedScatterCount = this.gameSpin.AddScatters(internalState, gameSpin, gameSpin.scatterCount);
                }

                gameSpin.multiplier = 1;

                if (gameSpin.willDoExplodingWilds)
                    this.explodingWilds.AddExplodingWilds(internalState, gameSpin);
                else if (gameSpin.reSpinCount == 0)                // if we have a >0 reSpinCount, finalReelWindow will already be set up
                {
                    this.gameSpin.CopySpinInToFinal(gameSpin);

                    // 1/10 chance of a spin-through gold wild, or one added during first 20 spins
                    if (this.rng.OneIn(internalState.force, 10) || (internalState.persistentData.nearMissGoldWildOnSpin != -1 && internalState.persistentData.playedSpins >= internalState.persistentData.nearMissGoldWildOnSpin))
                    {
                        gameSpin.willAddGoldWild = true;
                        internalState.persistentData.nearMissGoldWildOnSpin = -1;
                    }
                } else {
                  //  this.gameSpin.CopySpinInToFinal(gameSpin);
                }

                if (this.rng.OneIn(internalState.force, 3)) {
                    this.gameSpin.CheckForLowSymbolSubstitution(internalState, gameSpin);
                    var baseGameSpin = FeatureHelper.GetBaseGame(internalState.features);
                    baseGameSpin.reelWindow = ReelUtils.GetReelWindow(gameSpin.spinInReelWindow, internalState.persistentData.frameWindow);
                    baseGameSpin.reelSetId = gameSpin.bandset;
                    baseGameSpin.stopPositions = gameSpin.stopPositions.ToList();
                }   
            } 
            else // Will do bighit
            {
                internalState.persistentData.activeFrameCount = this.gameSpin.GetActiveFrameCount(internalState);
                gameSpin.frameBits = this.gameSpin.CreateFrameBits(internalState);
                this.gameSpin.ResetFrameWindow(internalState);
            }

            this.gameSpin.GenerateScatterOffsetList(gameSpin);
            this.gameSpin.CalculateWins(internalState, gameSpin);                                       // now we can calculate our wins
            internalState.persistentData.playedSpins++;

            if (gameSpin.totalWinInCoins > 0)
            {
                PayLinePayout payout = new PayLinePayout
                {
                    id = FeatureIds.BaseGame,
                    winDetails = gameSpin.winDetails,
                    win = gameSpin.winDetails.cashWin
                };

                if(gameSpin.willDoExplodingWilds) {
                    payout.id = FeatureIds.ExplodingWild;
                } else if(gameSpin.willDoBigHit) {
                    payout.id = FeatureIds.BigHit;
                }

                internalState.features.Add(payout);
            }

            return gameSpin.totalWinInCoins;
        }

        public void CacheOnStartState(InternalState internalState)
        {
            internalState.persistentData.onStartPlayedSpins = internalState.persistentData.playedSpins;
            internalState.persistentData.onStartNearMissScattersOnSpin = internalState.persistentData.nearMissScattersOnSpin;
            internalState.persistentData.onStartLandingSilverWildOnSpin = internalState.persistentData.landingSilverWildOnSpin;
            internalState.persistentData.onStartNearMissGoldWildOnSpin = internalState.persistentData.nearMissGoldWildOnSpin;
        }


//ADDED
		public void SaveFrameWindowAtGameStart(InternalState internalState)
		{
			for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
				for (Int32 row = 0; row < REEL_WINDOW; row++)
					internalState.baseGameState.frameWindowAtGameStart[reel, row] = internalState.persistentData.frameWindow[reel, row];
		}


		public void RestoreGameStartFrameWindow(InternalState internalState)
		{
			for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
				for (Int32 row = 0; row < REEL_WINDOW; row++)
					internalState.persistentData.frameWindow[reel, row] = internalState.baseGameState.frameWindowAtGameStart[reel, row];
		}
//ADDED

    }
}
