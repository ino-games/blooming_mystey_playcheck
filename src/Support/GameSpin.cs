using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Service.LogicCommon;
using Service.LogicCommon.Rng;
using Service.LogicCommon.Utils;
using Service.LogicCommon.Win;
using static Service.Logic.GameDefs;
using static Service.LogicCommon.FeatureHelper;

namespace Service.Logic
{

    class GameSpinState
    {
        public SpinConfig spinConfig = new SpinConfig();

        public Int32 spinIndex;
        public Int32 bandset;
        public int[] stopPositions = new int[NUMBER_REELS];

        // landing state
        public Int32[,] spinInReelWindow = new Int32[NUMBER_REELS, REEL_WINDOW];
        public Int32[,] spinInMultiplierWindow = new Int32[NUMBER_REELS, REEL_WINDOW];
        public Int32[,] spinInFrameWindow = new Int32[NUMBER_REELS, REEL_WINDOW];
        public Int32 scatterCount = 0;
        public Int32[] scatterOffsetList = new Int32[MAX_SCATTERS];
        public Int32 twoScatterProgressBits;

        // packed re-spin state
        public Int32[,] packedReSpinWindow = new Int32[NUMBER_REELS, REEL_WINDOW];
        public Int32 reSpinCount;

        // final state
        public Int32[,] finalReelWindow = new Int32[NUMBER_REELS, REEL_WINDOW];
        public Int32[,] finalMultiplierWindow = new Int32[NUMBER_REELS, REEL_WINDOW];
        public Int32 frameBits;

        public bool willDoExplodingWilds;
        public bool willDoBigHit;
        public bool willAddGoldWild;

        public long baseSpinWinInCoins = 0;
        public long baseSpinWinInCash = 0;

        public long explodingWildsWinInCoins;
        public long explodingWildsWinInCash;


        public long bigHitWinInCoins;
        public long bigHitWinInCash;

        public Int32 multiplier;
        public long totalWinInCoins;
        public long totalWinInCash;

        public long speculativeBigHitWin;

        public WinDetails<PayLineWinItem> winDetails = new WinDetails<PayLineWinItem>();
        internal ExplodingWildsState explodingWilds = new ExplodingWildsState();

        public List<Feature> results = new List<Feature>();
    }
    // *******************************************************************************
    // Single spin of the reels and any bonuses that can appear on said spin
    // *******************************************************************************
    class GameSpin
    {
        private Rng rng;
        private PayWays payWays;
        private BandSet bandSet;
        private RTP targetRTP;

        // *******************************************************************************
        public GameSpin(Rng rng, PayWays payWays, BandSet bandSet, RTP targetRTP)
        {
            this.rng = rng;
            this.payWays = payWays;
            this.bandSet = bandSet;
            this.targetRTP = targetRTP;
        }

        public void ResetGameSpin(GameSpinState gameSpin, Int32 multiplier, Int32 spinIndex)
        {
            gameSpin.spinIndex = spinIndex;
            gameSpin.totalWinInCoins = 0;
            gameSpin.totalWinInCash = 0;
            gameSpin.baseSpinWinInCoins = 0;
            gameSpin.baseSpinWinInCash = 0;
            gameSpin.explodingWildsWinInCoins = 0;
            gameSpin.explodingWildsWinInCash = 0;
            gameSpin.bigHitWinInCoins = 0;
            gameSpin.bigHitWinInCash = 0;
            gameSpin.reSpinCount = 0;

            gameSpin.speculativeBigHitWin = 0;
            gameSpin.twoScatterProgressBits = 0;
            gameSpin.reSpinCount = 0;
            gameSpin.frameBits = 0;

            gameSpin.multiplier = multiplier;
            for (Int32 x = 0; x < NUMBER_REELS; x++)
                for (Int32 y = 0; y < REEL_WINDOW; y++)
                {
                    gameSpin.spinInMultiplierWindow[x, y] = 0;
                    gameSpin.finalMultiplierWindow[x, y] = 0;
                }

            gameSpin.winDetails = null;
            gameSpin.willDoExplodingWilds = false;
            gameSpin.willDoBigHit = false;
            gameSpin.willAddGoldWild = false;
        }


//ADDED
		// called in the event of failing to generate a valid FS game, takes the basegame GameSpin

		public void BaseGameDoOver(InternalState internalState, GameSpinState gameSpin)
		{
			Int32[] bandsetBias = { 200, 75, 20 };
            internalState.features.Clear();
			// regenerate a base spin, this can use any of the bandsets, but will add no WILDS or scatters
			ResetGameSpin(gameSpin, 1, 0);
			gameSpin.spinConfig.spinType = GAME_MODE_BASEGAME;
			gameSpin.scatterCount = 0;
			gameSpin.bandset = this.rng.ChooseMe(internalState.force, bandsetBias);
			SetSpinInReels(internalState, gameSpin, gameSpin.bandset);

            internalState.features.Add(new Spin
                {
                    id = FeatureIds.BaseGame,
                    reelWindow = ReelUtils.GetReelWindow(gameSpin.spinInReelWindow, internalState.persistentData.frameWindow),
                    reelSetId = gameSpin.bandset,
                    stopPositions = gameSpin.stopPositions.ToList()
                });

			CopySpinInToFinal(gameSpin);
			GenerateScatterOffsetList(gameSpin);
			CalculateWins(internalState, gameSpin);

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
		}
//ADDED


        public void SetSpinInReels(InternalState internalState, GameSpinState gameSpin, Int32 bandSet)
        {
            gameSpin.bandset = bandSet;

            // spin the reels to a random position
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
            {
                Int32 length = this.bandSet.GetBandSetReelLength(gameSpin.bandset, reel);
                gameSpin.stopPositions[reel] = this.rng.Next(internalState.force, length);

                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    gameSpin.spinInReelWindow[reel, row] = this.bandSet.GetSymbol(gameSpin.bandset, reel, gameSpin.stopPositions[reel] + row);
                    gameSpin.spinInMultiplierWindow[reel, row] = 0;
                }
            }
        }


        // adds a win of 1x->5x stake
        void AddSmallWin(InternalState internalState, GameSpinState gameSpin)
        {
            Int32[] winLineBias = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
            Int32[] winSymbolBias = { 0, 0, 0, 0, 3, 4, 5, 5, 5, 5, 5, 4 };

            Int32 winLine, winningSymbol;

            // start with a losing spin
            SetSpinInReels(internalState, gameSpin, LOGIC_BANDSET_LOSING);

            winLine = this.rng.ChooseMe(internalState.force, winLineBias);
            winningSymbol = this.rng.ChooseMe(internalState.force, winSymbolBias);

            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                gameSpin.spinInReelWindow[reel, PAYLINES[winLine, reel]] = winningSymbol;
        }


        public void CopySpinInToFinal(GameSpinState gameSpin)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    gameSpin.finalReelWindow[reel, row] = gameSpin.spinInReelWindow[reel, row];
                    gameSpin.finalMultiplierWindow[reel, row] = gameSpin.spinInMultiplierWindow[reel, row];
                }

        }


        // the FS version will use frameWindow for the multiplier, where there are WILDS 
        public void CopySpinInToFinalFreeSpins(InternalState internalState, GameSpinState gameSpin)
        {
            for (Int32 row = 0; row < REEL_WINDOW; row++)
            {
                for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                {
                    gameSpin.finalReelWindow[reel, row] = gameSpin.spinInReelWindow[reel, row];
                    if (gameSpin.finalReelWindow[reel, row] == LOGIC_SYMBOL_WILD)
                        gameSpin.finalMultiplierWindow[reel, row] = internalState.persistentData.frameWindow[reel, row] - 1;
                    else
                        gameSpin.finalMultiplierWindow[reel, row] = gameSpin.spinInMultiplierWindow[reel, row];
                }
            }
        }


        // creates the spinInReelWindow in the passed GameSpin, returns the number of added WILDS
        public Int32 SpinRandomReels(InternalState internalState, GameSpinState gameSpin, Int32 maxAllowableFrames, bool addSmallWin, bool addBigHit, Int32 gameMode)
        {

            Int32[,] bandsetBias =
            {
                { 200, 75, 20 },		// GAME_MODE_BASEGAME
				{ 100, 150, 50 },		// GAME_MODE_FREE_SPINS
			};

            Int32[,] addBGWildsBias =
            {
                { 15,6,1},				// BH_MSM_X2,
				{ 15,6,1},				// BH_MSM_X3,
				{ 30,6,1},				// BH_MSM_X5,
				{ 40,6,1},				// BH_MSM_X10,
				{ 50,6,1},				// BH_MSM_X20,
			};

            Int32[] addFSWildsBias = { 30, 4, 1 };              // GAME_MODE_FREE_SPINS



            Int32[] symbolOffsetBias = new Int32[REELS_BIAS_TABLE_SIZE];

            Int32 reel, row;
            Int32 wildCount, addedWildCount = 0;
            Int32 bigHitOffset = -1;
            Int32 bandSet;


            if (gameMode == GAME_MODE_BASEGAME && internalState.persistentData.activeFrameCount > 5 && gameSpin.willDoBigHit)
                bandSet = LOGIC_BANDSET_JUST_LOW;
            else
                bandSet = this.rng.ChooseMe(internalState.force, bandsetBias, gameMode);


            if (addSmallWin && gameSpin.willDoExplodingWilds == false)
                AddSmallWin(internalState, gameSpin);
            else
                SetSpinInReels(internalState, gameSpin, bandSet);


            if (addBigHit)
            {
                // add a bigHit symbol. we need to filter positions where there are frames,
                // since we don't want it to land on a frame location

                bigHitOffset = 0;
                for (row = 0; row < REEL_WINDOW; row++)
                {
                    for (reel = 0; reel < NUMBER_REELS; reel++, bigHitOffset++)
                    {
                        if (internalState.persistentData.frameWindow[reel, row] == 0)
                            symbolOffsetBias[bigHitOffset] = 1;
                        else
                            symbolOffsetBias[bigHitOffset] = 0;
                    }
                }

                bigHitOffset = this.rng.ChooseMeAllowNegativeBias(internalState.force, symbolOffsetBias);
                //assert(bigHitOffset != -1);
                reel = bigHitOffset % NUMBER_REELS;
                row = bigHitOffset / NUMBER_REELS;

                gameSpin.spinInReelWindow[reel, row] = LOGIC_SYMBOL_BIGHIT;
            }

            // randomly add 0-2 WILDS (or a guaranteed 1 if exploding wilds is active)
            if (gameSpin.willDoExplodingWilds)
                wildCount = 1;
            else
            {
                if (addBigHit || addSmallWin)
                    wildCount = 0;      // no wilds required, and we know frames exist already, so this will be valid
                else
                {
                    if (gameMode == GAME_MODE_BASEGAME)
                    {
                        wildCount = this.rng.ChooseMe(internalState.force, addBGWildsBias, internalState.persistentData.bigHitMinimumStakeMultipleIndex);

                        if (wildCount == 0 && internalState.persistentData.spinsSinceLastWild >= internalState.persistentData.guaranteedWildSpinCount)
                        {
                            wildCount = 1;
                            ChooseNewGuaranteedWildSpinCount(internalState);
                        }
                    }
                    else
                        wildCount = this.rng.ChooseMe(internalState.force, addFSWildsBias);
                }
            }

            if (internalState.persistentData.landingSilverWildOnSpin != -1)
            {
                if (internalState.persistentData.playedSpins >= internalState.persistentData.landingSilverWildOnSpin && wildCount == 0)
                    wildCount = 1;
            }

            if (wildCount > 0)
            {
                Int32 wildOffset = 0;
                Int32 deactivateCount = REELS_BIAS_TABLE_SIZE - maxAllowableFrames;
                Int32 index, state;
                Int32[] wildBiasLookUpByReel = { 10, 11, 12, 13, 15 };

                for (row = 0; row < REEL_WINDOW; row++)
                {
                    for (reel = 0; reel < NUMBER_REELS; reel++, wildOffset++)
                    {
                        state = internalState.persistentData.frameWindow[reel, row];

                        if (state > 0 && state < MAX_FRAME_MULTIPLIER)
                            symbolOffsetBias[wildOffset] = -wildBiasLookUpByReel[reel];
                        else if (state == MAX_FRAME_MULTIPLIER)
                        {
                            deactivateCount--;
                            symbolOffsetBias[wildOffset] = 0;
                        }
                        else
                            symbolOffsetBias[wildOffset] = wildBiasLookUpByReel[reel];
                    }
                }

                for (Int32 n = 0; n < deactivateCount; n++)
                {
                    index = this.rng.ChooseMeAllowNegativeBias(internalState.force, symbolOffsetBias);
                    if (index != -1)
                        symbolOffsetBias[index] = 0;
                    else
                        break;
                }

                // revert any -ve entries to +ve
                for (Int32 n = 0; n < REELS_BIAS_TABLE_SIZE; n++)
                    if (symbolOffsetBias[n] < 0)
                        symbolOffsetBias[n] = -symbolOffsetBias[n];

                // also invalidate the bigHit position, if it was used
                if (bigHitOffset != -1)
                    symbolOffsetBias[bigHitOffset] = 0;

                // note this has to fail gracefully, we may have invalidated all possible positions to bring in a WILD
                for (Int32 n = 0; n < wildCount; n++)
                {
                    wildOffset = this.rng.ChooseMeAllowNegativeBias(internalState.force, symbolOffsetBias);
                    if (wildOffset == -1)
                        break;

                    reel = wildOffset % NUMBER_REELS;
                    row = wildOffset / NUMBER_REELS;

                    gameSpin.spinInReelWindow[reel, row] = LOGIC_SYMBOL_WILD;
                    internalState.persistentData.frameWindow[reel, row]++;
                    symbolOffsetBias[wildOffset] = 0;

                    addedWildCount++;
                }


            }

            if (addedWildCount > 0)
                internalState.persistentData.landingSilverWildOnSpin = -1;        // switch off

            return addedWildCount;
        }




        public bool HandleFreeSpinsTrigger(InternalState internalState, BaseGameResult baseGameState, GameSpinState gameSpin)
        {
            Int32[] freeSpinsModeBias = { 20, 5, 1 };
            Int32[] nonTriggeringScatterCountBias = { 54, 31, 15 };
            Int32 rtpIndex = (Int32)this.targetRTP;

            Int32[] freeSpinsHitRateByRTPIndex =
            {
                330,		// STAKE_RTP_86
				244,		// STAKE_RTP_92
				220,		// STAKE_RTP_94
				200,		// STAKE_RTP_96
			};

            if (internalState.buyBonus)
            {
                baseGameState.willDoFreeSpins = true;
                baseGameState.freeSpinsMode = this.rng.ChooseMe(internalState.force, freeSpinsModeBias);
                gameSpin.scatterCount = baseGameState.freeSpinsMode + 3;

                return true;
            }


            // only test for (or allow gaffing of) FS if the active frame count is low enough
            if (internalState.persistentData.activeFrameCount <= FREESPINS_TRIGGER_MAX_ACTIVE_FRAMES)
            {
                if (this.rng.OneIn(internalState.force, freeSpinsHitRateByRTPIndex[rtpIndex]))
                {
                    baseGameState.willDoFreeSpins = true;
                    baseGameState.freeSpinsMode = this.rng.ChooseMe(internalState.force, freeSpinsModeBias);
                    gameSpin.scatterCount = baseGameState.freeSpinsMode + 3;

                    return true;
                }
            }

            // choose a non-triggering scatter count (0-2)
            gameSpin.scatterCount = this.rng.ChooseMe(internalState.force, nonTriggeringScatterCountBias);

            return false;
        }




        public bool HandleExplodingWildsTrigger(InternalState internalState, BaseGameResult baseGameResult, GameSpinState gameSpin)
        {
            Int32 hitRate;

            if (internalState.persistentData.activeFrameCount < 2)
                hitRate = 30;
            else if (internalState.persistentData.activeFrameCount < 4)
                hitRate = 90;
            else
                hitRate = 10000;

            if (this.rng.OneIn(internalState.force, hitRate))
            {
                gameSpin.willDoExplodingWilds = true;

                return true;
            }

            return false;
        }



        public void SetFramedSymbolsWild(InternalState internalState, GameSpinState gameSpin)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
            {
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    if (internalState.persistentData.frameWindow[reel, row] > 0)
                    {
                        gameSpin.finalReelWindow[reel, row] = LOGIC_SYMBOL_WILD;
                        gameSpin.finalMultiplierWindow[reel, row] = internalState.persistentData.frameWindow[reel, row] - 1;
                    }
                }
            }
        }





        public bool HandleBigHitTrigger(InternalState internalState, BaseGameResult baseGameResult, GameSpinState gameSpin)
        {
            Int32 frameCount;
            long speculativeBigHitWin, speculativeBigHitWinCash;
            long bet = internalState.bet;

            Int32[] bigHitMinimumStakeMultipleLookUp = { 2, 3, 5, 10, 20 };
            Int32 stakeMultiple;

            //REMOVEME:
            //return false;

            frameCount = internalState.persistentData.activeFrameCount;

            // we will only allow it to trigger if there is at least one active frame
            if (frameCount > 0)
            {
                // speculative bigHit test, we spin the reels, apply the frames and get the win
                // if 3x stake or more, we use this as the result, otherwise, willDoBigHit is set FALSE, and the reels will get re-spun later

                SpinRandomReels(internalState, gameSpin, MAX_ALLOWABLE_FRAMES_BASEGAME, false, true, GAME_MODE_BASEGAME);
                Spin item = new Spin
                    {
                        id = FeatureIds.BaseGame,
                        reelWindow = ReelUtils.GetReelWindow(gameSpin.spinInReelWindow, internalState.persistentData.frameWindow),
                        reelSetId = gameSpin.bandset,
                        stopPositions = gameSpin.stopPositions.ToList()
                    };
                CopySpinInToFinal(gameSpin);
                SetFramedSymbolsWild(internalState, gameSpin);
                gameSpin.winDetails = this.payWays.CalculateTotal(internalState, gameSpin.finalReelWindow, gameSpin.finalMultiplierWindow, NUMBER_WIN_LINES);
                speculativeBigHitWin = gameSpin.winDetails.coinWin;


                speculativeBigHitWinCash = this.payWays.ConvertCoinValueToCash(speculativeBigHitWin, internalState.coinValue);
                stakeMultiple = bigHitMinimumStakeMultipleLookUp[internalState.persistentData.bigHitMinimumStakeMultipleIndex];

                if (speculativeBigHitWinCash >= stakeMultiple * bet)
                {
                    gameSpin.speculativeBigHitWin = speculativeBigHitWin;
                    gameSpin.willDoBigHit = true;

                    // we now choose a new minimum stake multiple
                    SelectBigHitMinimumStakeMultipleIndex(internalState);
                    
                    internalState.features.Add(item);
                    internalState.features.Add(new Spin
                    {
                        id = FeatureIds.BigHit,
                        reelWindow = ReelUtils.GetReelWindow(gameSpin.finalReelWindow, internalState.persistentData.frameWindow),
                        reelSetId = gameSpin.bandset,
                        stopPositions = gameSpin.stopPositions.ToList()
                    });

                    return true;
                }

                return false;
            }

            return false;
        }


        public bool HandleSmallWinTrigger(InternalState internalState, BaseGameResult baseGameResult, GameSpinState gameSpin)
        {
            Int32 totalChance;

            // in order not to have to push FS out too far for 86.7% RTP, we use a slightly less
            // generous hit rate for the small win trigger

            if (this.targetRTP == RTP.Target86)
                totalChance = 532;
            else
                totalChance = 521;

            if (this.rng.XInY(internalState.force, 100, totalChance))
            {
                baseGameResult.willDoSmallWin = true;
                return true;
            }

            return false;
        }


        public void FillBiasTable(Int32[] biasTable, Int32 value)
        {
            Int32 size = biasTable.Length;

            for (Int32 n = 0; n < size; n++)
                biasTable[n] = value;
        }


        void WritePackedReel(GameSpinState gameSpin, Int32 stepIndex, Int32 reel, Int32[,] scratchReelWindow)
        {
            Int32 packedvalue;

            stepIndex *= 8;
            for (Int32 row = 0; row < REEL_WINDOW; row++)
            {
                packedvalue = gameSpin.packedReSpinWindow[reel, row];
                packedvalue &= ~(255 << stepIndex);
                packedvalue |= scratchReelWindow[reel, row] << stepIndex;
                gameSpin.packedReSpinWindow[reel, row] = packedvalue;
            }
        }


        void WritePackedScatterBits(GameSpinState gameSpin, Int32 stepIndex, Int32 scatterBits)
        {
            Int32 packedvalue;

            packedvalue = gameSpin.packedReSpinWindow[stepIndex, 0];
            packedvalue &= 0xffffff;
            packedvalue |= scatterBits << 24;
            gameSpin.packedReSpinWindow[stepIndex, 0] = packedvalue;
        }


        void DropScatterReel(InternalState internalState, GameSpinState gameSpin, Int32[,] scratchReelWindow, Int32 reel)
        {
            Int32 length = this.bandSet.GetBandSetReelLength(gameSpin.bandset, reel);
            Int32 pos = this.rng.Next(internalState.force, length);

            // shuffle down
            for (Int32 n = REEL_WINDOW - 2; n >= 0; n--)
                scratchReelWindow[reel, n + 1] = scratchReelWindow[reel, n];

            // add a new losing symbol at the top of the shuffled reel
            scratchReelWindow[reel, 0] = this.bandSet.GetSymbol(LOGIC_BANDSET_LOSING, reel, pos);
        }


        void ReSpinReel(InternalState internalState, Int32[,] scratchReelWindow, Int32 reel)
        {
            Int32 length = this.bandSet.GetBandSetReelLength(LOGIC_BANDSET_LOSING, reel);
            Int32 pos = this.rng.Next(internalState.force, length);

            for (Int32 n = 0; n < REEL_WINDOW; n++)
                scratchReelWindow[reel, n] = this.bandSet.GetSymbol(LOGIC_BANDSET_LOSING, reel, pos + n);
        }




        // for this we have determined the initial selected spin resulted in no win, added no wilds/bigHit
        // we can therefore replace it with a suitable reSpin result
        // this needs 1 or 2 scatters, which can land in row 0/1/2 only
        public Int32 AddScattersWithReSpin(InternalState internalState, GameSpinState gameSpin, Int32 scatterCountToAdd)
        {
            Int32[] scatterReelBias = { 1, 1, 1, 1, 1 };
            Int32[] initialScatterRowBias = { 5, 3, 2, 0 };
            Int32[] reSpinScatterRowBias = { 5, 4, 1, 0 };

            Int32[,] scratchReelWindow = new Int32[NUMBER_REELS, REEL_WINDOW];
            Int32 scatterReelBits;

            bool[] scatterReel = { false, false, false, false, false };
            bool[] scratchScatterReel = new bool[NUMBER_REELS];

            Int32 highestRow = 0, reel, row;
            Int32 remainingScatters, scattersPerStep, reSpinCount;

            Int32 twoScatterBits = 0, nextTwoScatterBit = 1, scatterCount = 2;


            // promote to 2 inital scatter if 1, that is the minimum
            if (scatterCountToAdd == 1)
                scatterCountToAdd = 2;

            // start with a new losing spin-in
            SetSpinInReels(internalState, gameSpin, LOGIC_BANDSET_LOSING);


            // choose the initial 2 scatters
            for (Int32 n = 0; n < 2; n++)
            {
                reel = this.rng.ChooseMe(internalState.force, scatterReelBias);
                scatterReelBias[reel] = 0;
                // choose a starting row
                row = this.rng.ChooseMe(internalState.force, initialScatterRowBias);
                gameSpin.spinInReelWindow[reel, row] = LOGIC_SYMBOL_SCATTER;
                scatterReel[reel] = true;
                if (row > highestRow)
                    highestRow = row;
            }
            var baseGameSpin = GetBaseGame(internalState.features);
            // internalState.results.Clear();
            // internalState.results.Add(new Spin
            // {
            //     id = FeatureIds.BaseGame,
                baseGameSpin.reelWindow = ReelUtils.GetReelWindow(gameSpin.spinInReelWindow, internalState.persistentData.frameWindow);
                baseGameSpin.reelSetId = gameSpin.bandset;
                baseGameSpin.stopPositions = gameSpin.stopPositions.ToList();
            // });

            if (internalState.persistentData.twoScatterProgress < FULL_SCATTER_PROGRESS_BAR_COUNT)
            {
                internalState.persistentData.twoScatterProgress++;
                internalState.features.Add(new Collection{id = FeatureIds.Scatters, value = internalState.persistentData.twoScatterProgress, update = 1});
                twoScatterBits |= nextTwoScatterBit;
                nextTwoScatterBit <<= 1;
            }



            // clear the packed re-spin window
            // and copy the spinIn to scratch
            for (reel = 0; reel < NUMBER_REELS; reel++)
            {
                for (row = 0; row < REEL_WINDOW; row++)
                {
                    gameSpin.packedReSpinWindow[reel, row] = 0;
                    scratchReelWindow[reel, row] = gameSpin.spinInReelWindow[reel, row];
                }
            }

            remainingScatters = scatterCountToAdd - 2;
            reSpinCount = REEL_WINDOW - 1 - highestRow;

            for (Int32 stage = 0; stage < reSpinCount; stage++)
            {
                scatterReelBits = 0;
                // write the scatter reels
                for (Int32 reelIndex = 0; reelIndex < NUMBER_REELS; reelIndex++)
                {
                    if (scatterReel[reelIndex] == true)
                    {
                        scatterReelBits |= (1 << reelIndex);        // mark this reel as fixed
                                                                    // shuffle reel down, and add new random losing symbol into the top position
                        DropScatterReel(internalState, gameSpin, scratchReelWindow, reelIndex);
                        WritePackedReel(gameSpin, stage, reelIndex, scratchReelWindow);
                    }
                }
                WritePackedScatterBits(gameSpin, stage, scatterReelBits);

                highestRow++;

                // now we re-spin all the non scatter reels with losing symbols
                for (Int32 reelIndex = 0; reelIndex < NUMBER_REELS; reelIndex++)
                {
                    if (scatterReel[reelIndex] == false)
                        ReSpinReel(internalState, scratchReelWindow, reelIndex);
                }


                // now choose a number of scatters, or all remaning, if the last step
                if (stage == reSpinCount - 1)
                {
                    // all remaining
                    scattersPerStep = remainingScatters;
                    remainingScatters = 0;
                }
                else
                {
                    // 0->remaining
                    scattersPerStep = this.rng.Next(internalState.force, 0, remainingScatters);
                    remainingScatters -= scattersPerStep;
                }

                // we mark a stage as being valid for giving 2 scatter progress
                scatterCount += scattersPerStep;
                var scatterCountIncreased = false;
                if (scatterCount == 2 && internalState.persistentData.twoScatterProgress < FULL_SCATTER_PROGRESS_BAR_COUNT)
                {
                    internalState.persistentData.twoScatterProgress++;
                    scatterCountIncreased = true;
                    // internalState.features.Add(new Collection{id = FeatureIds.Scatters, value = internalState.persistentData.twoScatterProgress, update = 1});
                    twoScatterBits |= nextTwoScatterBit;
                    nextTwoScatterBit <<= 1;
                }

                // copy scatterReel[], as we will need to know which reels to write out (that are currently non scatter)
                for (Int32 n = 0; n < NUMBER_REELS; n++)
                    scratchScatterReel[n] = scatterReel[n];

                // add new scatters
                for (Int32 j = 0; j < scattersPerStep; j++)
                {
                    reel = this.rng.ChooseMe(internalState.force, scatterReelBias);
                    scatterReelBias[reel] = 0;
                    // choose a starting row, for simplicity, we will limit it to no lower than highestRow
                    row = this.rng.Next(internalState.force, 0, highestRow);
                    scratchReelWindow[reel, row] = LOGIC_SYMBOL_SCATTER;
                    scatterReel[reel] = true;
                }

                // now write the re-spun reels
                for (Int32 reelIndex = 0; reelIndex < NUMBER_REELS; reelIndex++)
                {
                    if (scratchScatterReel[reelIndex] == false)
                        WritePackedReel(gameSpin, stage, reelIndex, scratchReelWindow);
                }

                internalState.features.Add(new Spin
                {
                    id = FeatureIds.Respin,
                    reelWindow = ReelUtils.GetReelWindow(scratchReelWindow, internalState.persistentData.frameWindow),
                    reelSetId = gameSpin.bandset,
                    stopPositions = gameSpin.stopPositions.ToList()
                });
                if(scatterCountIncreased) {
                    internalState.features.Add(new Collection{id = FeatureIds.Scatters, value = internalState.persistentData.twoScatterProgress, update = 1});
                }
                gameSpin.reSpinCount++;
                if (remainingScatters == 0 && scatterCountToAdd == 5)       // we can bail early if we placed all of the maximum scatter count
                    break;

            }


            gameSpin.twoScatterProgressBits = twoScatterBits;

            // finally write scratchReelWindow[][] to final[][]
            for (reel = 0; reel < NUMBER_REELS; reel++)
                for (row = 0; row < REEL_WINDOW; row++)
                    gameSpin.finalReelWindow[reel, row] = scratchReelWindow[reel, row];

            return scatterCountToAdd;
        }


        // returns the number of scatters actually added
        public Int32 AddScatters(InternalState internalState, GameSpinState gameSpin, Int32 scatterCountToAdd)
        {
            Int32 wildFilterIndex = 0, symbolIndex, addedScatterCount = 0;


            if (scatterCountToAdd > 0)
            {
                Int32[] scatterPositionBias =
                {
                    8,1,8,1,8,		// scatters can appear on all reels, biased to reel 1, 3, 5
					8,1,8,1,8,
                    8,1,8,1,8,
                    8,1,8,1,8,
                };

                Int32 linearScatterIndex = 0, reel, row;

                if (scatterCountToAdd < 3)
                {
                    // for anything less than a FS trigger, we want adding scatters to be non-destructive. to achieve this, we will use the
                    // pre-filtered winningSymbolBias[]

                    for (Int32 n = 0; n < REELS_BIAS_TABLE_SIZE; n++)
                    {
                        if (internalState.baseGameState.winningSymbolBias[n] == 0)
                            scatterPositionBias[n] = 0;
                    }

                }

                // also filter out WILDS and BIGHIT
                for (row = 0; row < REEL_WINDOW; row++)
                {
                    for (reel = 0; reel < NUMBER_REELS; reel++, wildFilterIndex++)
                    {
                        symbolIndex = gameSpin.spinInReelWindow[reel, row];

                        if (symbolIndex == LOGIC_SYMBOL_WILD || symbolIndex == LOGIC_SYMBOL_BIGHIT)
                            scatterPositionBias[wildFilterIndex] = 0;
                    }
                }


                for (Int32 n = 0; n < scatterCountToAdd; n++)
                {
                    linearScatterIndex = this.rng.ChooseMe(internalState.force, scatterPositionBias);
                    if (linearScatterIndex != -1)
                    {
                        reel = linearScatterIndex % NUMBER_REELS;
                        row = linearScatterIndex / NUMBER_REELS;

                        gameSpin.spinInReelWindow[reel, row] = LOGIC_SYMBOL_SCATTER;
                        gameSpin.spinInMultiplierWindow[reel, row] = 0;

                        // invalidate the full reel of scatterPositionBias[], as we only want one scatter per reel 
                        for (Int32 j = 0; j < REEL_WINDOW; j++, reel += NUMBER_REELS)
                            scatterPositionBias[reel] = 0;

                        // special case if we are placing 2 scatters, and we have just placed the first on anything other the bottom row,
                        // invalidate all but the bottom row, to ensure don't have a re-spin triggering condition
                        if (n == 0 && scatterCountToAdd == 2 && row != REEL_WINDOW - 1)
                        {
                            for (Int32 j = 0; j < 3 * NUMBER_REELS; j++)
                                scatterPositionBias[j] = 0;
                        }


                        // also invalidate the chosen position in winningSymbolBias[], this means subsequent additions can also be non destructive, ie the star
                        internalState.baseGameState.winningSymbolBias[linearScatterIndex] = 0;

                        addedScatterCount++;
                    }
                }

                if (addedScatterCount == 2 && internalState.persistentData.twoScatterProgress < FULL_SCATTER_PROGRESS_BAR_COUNT)
                {
                    internalState.persistentData.twoScatterProgress++;

                    // No Respins, so add after base game spin
                    internalState.features.Add(new Collection{id = FeatureIds.Scatters, value = internalState.persistentData.twoScatterProgress, update = 1});
                    gameSpin.twoScatterProgressBits = 1;
                }
            }

            var baseGameSpin = GetBaseGame(internalState.features);
            baseGameSpin.reelWindow = ReelUtils.GetReelWindow(gameSpin.spinInReelWindow, internalState.persistentData.frameWindow);
            baseGameSpin.reelSetId = gameSpin.bandset;
            baseGameSpin.stopPositions = gameSpin.stopPositions.ToList();

            return addedScatterCount;
        }


        // set the scatterOffsetList[]
        public void GenerateScatterOffsetList(GameSpinState gameSpin)
        {
            Int32 linearOffset = 0, scatterIndex = 0;

            gameSpin.scatterCount = 0;
            for (Int32 n = 0; n < MAX_SCATTERS; n++)
                gameSpin.scatterOffsetList[n] = -1;

            for (Int32 row = 0; row < REEL_WINDOW; row++)
            {
                for (Int32 reel = 0; reel < NUMBER_REELS; reel++, linearOffset++)
                    if (gameSpin.finalReelWindow[reel, row] == LOGIC_SYMBOL_SCATTER)
                    {
                        gameSpin.scatterOffsetList[scatterIndex++] = linearOffset;
                        gameSpin.scatterCount++;
                    }
            }
        }



        public void CheckForLowSymbolSubstitution(InternalState internalState, GameSpinState gameSpin)
        {
            Int32[] translationFromSymbolSelectionBias = { 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1 };
            Int32[] translationToSymbolSelectionBias = { 0, 0, 0, 2, 4, 6, 8, 0, 0, 0, 0, 0 };
            Int32[] translationCountBias = { 0, 5, 2 };
            Int32[] minimumHighSymbolCountBias =
            {
                0,		// 0
				0,		// 1
				0,		// 2
				10,		// 3
				8,		// 4
				6,		// 5
				4,		// 6
			};

            Int32[] lowToHighTranslationLookUp = new Int32[LOGIC_SYMBOL_MAX];

            Int32 highSymbolCount = 0, offset, symbolIndex;
            Int32 translationCount;
            Int32 fromIndex, toIndex, minimumHighSymbolCount;


            FillBiasTable(internalState.baseGameState.winningSymbolBias, 1);
            RemovePayWaysWinsFromBiasTable(gameSpin.winDetails, internalState.baseGameState.winningSymbolBias);


            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
            {
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    // at this point the only non-paying symbols present can be WILDS, and they can be treated as a high symbol 
                    if (gameSpin.finalReelWindow[reel, row] <= LOGIC_SYMBOL_HIGH4)
                        highSymbolCount++;
                }
            }

            minimumHighSymbolCount = this.rng.ChooseMe(internalState.force, minimumHighSymbolCountBias);

            if (highSymbolCount < minimumHighSymbolCount)
            {
                // initialise default state of no translation
                for (Int32 n = 0; n < LOGIC_SYMBOL_MAX; n++)
                    lowToHighTranslationLookUp[n] = n;

                // scan logicGlobs.winningSymbolBias[], clearing any relevant entries in translationSymbolSelectionBias[]

                offset = 0;
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    for (Int32 reel = 0; reel < NUMBER_REELS; reel++, offset++)
                    {
                        if (internalState.baseGameState.winningSymbolBias[offset] == 0)
                        {
                            symbolIndex = gameSpin.finalReelWindow[reel, row];
                            translationFromSymbolSelectionBias[symbolIndex] = 0;
                            translationToSymbolSelectionBias[symbolIndex] = 0;
                        }
                    }
                }

                translationCount = this.rng.ChooseMe(internalState.force, translationCountBias);

                for (Int32 n = 0; n < translationCount; n++)
                {
                    fromIndex = this.rng.ChooseMe(internalState.force, translationFromSymbolSelectionBias);
                    toIndex = this.rng.ChooseMe(internalState.force, translationToSymbolSelectionBias);

                    if (fromIndex != -1 && toIndex != -1)
                    {
                        translationFromSymbolSelectionBias[fromIndex] = 0;
                        translationToSymbolSelectionBias[toIndex] = 0;

                        lowToHighTranslationLookUp[fromIndex] = toIndex;
                    }
                }

                // finally we can re-map using lowToHighTranslationLookUp[]
                for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                {
                    for (Int32 row = 0; row < REEL_WINDOW; row++)
                    {
                        fromIndex = gameSpin.finalReelWindow[reel, row];
                        gameSpin.spinInReelWindow[reel, row] = lowToHighTranslationLookUp[fromIndex];
                        gameSpin.finalReelWindow[reel, row] = lowToHighTranslationLookUp[fromIndex];
                    }
                }
            }
        }


        // creates a bitfield of all the active frames
        public Int32 CreateFrameBits(InternalState internalState)
        {
            Int32 currentBit = 1, frameBits = 0;

            for (Int32 row = 0; row < REEL_WINDOW; row++)
            {
                for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                {
                    if (internalState.persistentData.frameWindow[reel, row] > 0)
                        frameBits |= currentBit;
                    currentBit <<= 1;
                }
            }

            return frameBits;
        }




        public void CalculateWins(InternalState internalState, GameSpinState gameSpin)
        {
            gameSpin.winDetails = this.payWays.CalculateTotal(internalState, gameSpin.finalReelWindow, gameSpin.finalMultiplierWindow, NUMBER_WIN_LINES, gameSpin.multiplier);
            gameSpin.totalWinInCoins = gameSpin.winDetails.coinWin;
            gameSpin.totalWinInCash = gameSpin.winDetails.cashWin;

            // the base spin win is assigned to bigHit/explodingWilds/baseSpinWin in order of precedence
            if (gameSpin.willDoBigHit)
            {
                gameSpin.bigHitWinInCoins = gameSpin.totalWinInCoins;
                gameSpin.bigHitWinInCash = gameSpin.totalWinInCash;
            }
            else if (gameSpin.willDoExplodingWilds)
            {
                gameSpin.explodingWildsWinInCoins = gameSpin.totalWinInCoins;
                gameSpin.explodingWildsWinInCash = gameSpin.totalWinInCash;
            }
            else
            {
                gameSpin.baseSpinWinInCoins = gameSpin.totalWinInCoins;
                gameSpin.baseSpinWinInCash = gameSpin.totalWinInCash;
            }
        }





        public void ChooseNewGuaranteedWildSpinCount(InternalState internalState)
        {
            internalState.persistentData.guaranteedWildSpinCount = this.rng.Next(internalState.force, 15, 20);
        }

        public void SaveFrameWindow(InternalState internalState)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                    internalState.baseGameState.ewFrameWindowCopy[reel, row] = internalState.persistentData.frameWindow[reel, row];
        }


        public void SaveFreeSpinsFrameWindow(InternalState internalState)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                    internalState.baseGameState.fsFrameWindowCopy[reel, row] = internalState.persistentData.frameWindow[reel, row];
        }


        public void SaveBuyBonusFrameWindow(InternalState internalState)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    internalState.baseGameState.bbFrameWindowCopy[reel, row] = internalState.persistentData.frameWindow[reel, row];
                    internalState.persistentData.frameWindow[reel, row] = 0;
                }
        }


        public void RestoreFrameWindow(InternalState internalState)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                    internalState.persistentData.frameWindow[reel, row] = internalState.baseGameState.ewFrameWindowCopy[reel, row];
        }


        public void RestoreFreeSpinsFrameWindow(InternalState internalState)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                    internalState.persistentData.frameWindow[reel, row] = internalState.baseGameState.fsFrameWindowCopy[reel, row];
        }


        public void RestoreBuyBonusFrameWindow(InternalState internalState)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                    internalState.persistentData.frameWindow[reel, row] = internalState.baseGameState.bbFrameWindowCopy[reel, row];
        }


        // each Int32 represents the multiplier level, and therefore zero indicates the frame is inactive
        public void ResetFrameWindow(InternalState internalState)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                    internalState.persistentData.frameWindow[reel, row] = 0;
        }



        public void InitialiseFirstXSpinElements(InternalState internalState)
        {
            Int32[] first20SpinsBias = new Int32[20];

            // fill first half only
            for (Int32 n = 0; n < 20; n++)
            {
                if (n < 10)
                    first20SpinsBias[n] = 1;
                else
                    first20SpinsBias[n] = 0;
            }

            internalState.persistentData.nearMissScattersOnSpin = this.rng.ChooseMe(internalState.force, first20SpinsBias);
            first20SpinsBias[internalState.persistentData.nearMissScattersOnSpin] = 0;
            internalState.persistentData.landingSilverWildOnSpin = this.rng.ChooseMe(internalState.force, first20SpinsBias);
            first20SpinsBias[internalState.persistentData.landingSilverWildOnSpin] = 0;

            // now enable the upper 10 spins
            for (Int32 n = 10; n < 20; n++)
                first20SpinsBias[n] = 1;
            internalState.persistentData.nearMissGoldWildOnSpin = this.rng.ChooseMe(internalState.force, first20SpinsBias);

            internalState.persistentData.playedSpins = 0;
        }


        // this index is used to look up the minimum stake multiple for the next bigHit
        // a new one is selected after a bigHit is triggered
        public void SelectBigHitMinimumStakeMultipleIndex(InternalState internalState)
        {
            Int32[] bigHitMinimumStakeMultipleIndexBias =
            {
                100,	// x2,
				500,	// x3,
				80,		// x5,
				50,		// x10,
				20,		// x20,
			};

            internalState.persistentData.bigHitMinimumStakeMultipleIndex = this.rng.ChooseMe(internalState.force, bigHitMinimumStakeMultipleIndexBias);
        }




        // returns the count of active frames, and the total of active multipliers in pTotal
        public Int32 GetActiveFrameCount(InternalState internalState)
        {
            Int32 count = 0, total = 0;

            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
            {
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    if (internalState.persistentData.frameWindow[reel, row] > 0)
                    {
                        total += internalState.persistentData.frameWindow[reel, row];
                        count++;
                    }
                }
            }

            return count;
        }




        // takes a WinListGroup and a prepared bias table, and zeroes any entries in that bias table that are part of a win
        public void RemovePayWaysWinsFromBiasTable(WinDetails<PayLineWinItem> winDetails, Int32[] biasTable)
        {
            Int32 winLength, payLineIndex, row;
            Int32 winCount = winDetails.winList.Count;

            for (Int32 n = 0; n < winCount; n++)
            {
                winLength = winDetails.winList[n].symbolCount;
                payLineIndex = winDetails.winList[n].payLineId;

                for (Int32 reel = 0; reel < winLength; reel++)
                {
                    row = PAYLINES[payLineIndex, reel];
                    biasTable[(row * NUMBER_REELS) + reel] = 0;
                }
            }
        }


        public void TrackSpinsSinceLastWild(InternalState internalState, BaseGameState baseGameState, GameSpinState gameSpin)
        {
            Int32 wildCount = 0;

            internalState.persistentData.spinsSinceLastWild++;

            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
            {
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    if (gameSpin.spinInReelWindow[reel, row] == LOGIC_SYMBOL_WILD)
                        wildCount++;
                }
            }

            if (wildCount > 0)
                internalState.persistentData.spinsSinceLastWild = 0;
        }

    }
}
