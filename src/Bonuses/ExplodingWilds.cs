using System;
using System.Collections.Generic;
using System.Linq;
using Service.LogicCommon;
using Service.LogicCommon.Rng;
using Service.LogicCommon.Utils;
using static Service.Logic.GameDefs;

namespace Service.Logic
{
    public class ExplodingWildsState
    {
        public Int32[,] list = new Int32[MAX_EXPLODING_WILDS_STAGES, MAX_EXPLODING_WILDS_PER_STAGE];
        public Int32 stageCount;

        public Int32 explodingWildsStageIndex;
        public Int32 explodingWildsListIndex;

        public List<Feature> results = new List<Feature>();
    }

    class ExplodingWilds
    {
        // the exploding wilds, each entry will be encoded to hold the source and dest offset
        // -1 indicates an unused entry

        private Rng rng;
        private PayWays payWays;
        private GameSpin gameSpin;

        public ExplodingWilds(Rng rng, PayWays payWays, GameSpin gameSpin)
        {
            this.rng = rng;
            this.payWays = payWays;
            this.gameSpin = gameSpin;
        }


        void ClearExplodingWilds(GameSpinState gameSpin)
        {
            for (Int32 n = 0; n < MAX_EXPLODING_WILDS_STAGES; n++)
                for (Int32 j = 0; j < MAX_EXPLODING_WILDS_PER_STAGE; j++)
                    gameSpin.explodingWilds.list[n, j] = -1;
            gameSpin.explodingWilds.stageCount = 0;

            gameSpin.explodingWilds.explodingWildsStageIndex = 0;
            gameSpin.explodingWilds.explodingWildsListIndex = 0;
        }



        void MassageBiasTableUsingMaxAllowableFrames(InternalState internalState, GameSpinState gameSpin, Int32[] biasTable, Int32[] biasLookUpTable, Int32 maxAllowableFrames)
        {
            Int32 activeFrameCount = 0, reel, row, index, state;
            Int32 deactivateCount;
            Int32 symbolIndex;

            // this bias towards lower reels, rather than higher, as we use it to determine positions that are deactivated
            Int32[] lookUpBiasByReel = { 15, 13, 12, 11, 10 };
            Int32[] lookUpFinalBiasByReel = { 50, 55, 60, 65, 70 };


            index = 0;
            activeFrameCount = 0;
            for (row = 0; row < REEL_WINDOW; row++)
            {
                for (reel = 0; reel < NUMBER_REELS; reel++, index++)
                {
                    state = internalState.persistentData.frameWindow[reel, row];
                    if (state > 0)
                    {
                        biasTable[index] = -biasLookUpTable[state];                   // active frame, setup with -ve bias, so it will be passed over for selection when we deactivate positions below
                        activeFrameCount++;
                    }
                    else
                    {
                        symbolIndex = gameSpin.spinInReelWindow[reel, row];
                        if (symbolIndex == LOGIC_SYMBOL_BIGHIT || symbolIndex == LOGIC_SYMBOL_SCATTER)
                            biasTable[index] = 0;                                      // unsuitable location for WILD to explode to 
                        else
                            biasTable[index] = lookUpBiasByReel[reel];                 // inactive frame
                    }
                }
            }

            // now we filter deactivateCount of the entries, 
            deactivateCount = REELS_BIAS_TABLE_SIZE - maxAllowableFrames;
            for (Int32 n = 0; n < deactivateCount; n++)
            {
                index = this.rng.ChooseMeAllowNegativeBias(internalState.force, biasTable);
                if (index == -1)
                    break;
                biasTable[index] = 0;
            }

            // finally, revert any -ve entries to +ve, and force +ve entries to the inactive frame look-up value
            for (Int32 n = 0; n < REELS_BIAS_TABLE_SIZE; n++)
            {
                reel = n % NUMBER_REELS;

                if (biasTable[n] < 0)
                    biasTable[n] = -biasTable[n];
                else if (biasTable[n] > 0)
                    biasTable[n] = lookUpFinalBiasByReel[reel];
            }
        }


        void TemporarilyInvalidateBiasTableEntriesAtMaxFrameWindow(InternalState internalState, GameSpinState gameSpin, Int32[] biasTable, Int32 maxMultiplier)
        {
            Int32 row, reel, offset;

            for (row = 0, offset = 0; row < REEL_WINDOW; row++)
            {
                for (reel = 0; reel < NUMBER_REELS; reel++, offset++)
                {
                    if (internalState.persistentData.frameWindow[reel, row] >= maxMultiplier)
                        biasTable[offset] = -biasTable[offset];
                }
            }
        }




        // adds an exploding wild to the passed stage, we will have already validated fromIndex/toIndex
        void AddExplodingWild(InternalState internalState, GameSpinState gameSpin, Int32 fromIndex, Int32 toIndex)
        {
            Int32 fromReel = fromIndex % NUMBER_REELS;
            Int32 fromRow = fromIndex / NUMBER_REELS;
            Int32 toReel = toIndex % NUMBER_REELS;
            Int32 toRow = toIndex / NUMBER_REELS;

            // pack the from/to indices
            gameSpin.explodingWilds.list[gameSpin.explodingWilds.explodingWildsStageIndex, gameSpin.explodingWilds.explodingWildsListIndex++] = (fromIndex << EW_FROM_SHIFT) | toIndex;
            // update the relevant symbol and multiplier index

            //assert(logicGlobs.frameWindow[toReel][toRow] < 5);

            internalState.persistentData.frameWindow[toReel, toRow]++;

            gameSpin.finalReelWindow[toReel, toRow] = LOGIC_SYMBOL_WILD;
            gameSpin.finalMultiplierWindow[toReel, toRow] = internalState.persistentData.frameWindow[toReel, toRow] - 1;
        }

        public long AddExplodingWildsAttempt(InternalState internalState, GameSpinState gameSpin, Int32 maxAllowableFrames, Int32 mood, Int32 mode)
        {
            Int32[,] explodingWildsCountBias =
            {
				//	0,	1,	2,	3,	4,	5,	6,	7,	8,	9,	10
				// ----------------------------------------------
				{   0,  20, 15, 8,  2,  0,  0,  0,  0,  0,  0   },		// FS_MOOD_0
				{   0,  20, 15, 10, 5,  4,  2,  0,  0,  0,  0   },		// FS_MOOD_1
				{   0,  5,  5,  20, 15, 10, 5,  4,  2,  0,  0   },		// FS_MOOD_2
				{   0,  5,  5,  20, 15, 10, 5,  4,  2,  0,  0   },		// FS_MOOD_3
				{   0,  5,  5,  20, 15, 10, 5,  4,  2,  0,  0   },		// FS_MOOD_4
			};

            Int32[] explodingWildsCountPerStageBias =
            {
                0,					// 0
				5,					// 1
				10,					// 2
				20,					// 3
				5,					// 4
			};


            // look-up table for toWildPositionBias, keyed off the frame state
            Int32[] toWildBiasLookUpByFrameState =
            {
				// relative probability decreases as multiplier increases, with x5 being excluded
				50,				// FS_FRAME_STATE_OFF,
				40,				// FS_FRAME_STATE_X1,
				30,				// FS_FRAME_STATE_X2,
				20,				// FS_FRAME_STATE_X3,
				10,				// FS_FRAME_STATE_X4,
				0,				// FS_FRAME_STATE_X5,
			};

            // where WILDS explode from, each position can only be used for one exploding group
            Int32[] fromWildPositionBias = new Int32[REELS_BIAS_TABLE_SIZE];

            // where they explode to, can be used repeatedly
            Int32[] toWildPositionBias = new Int32[REELS_BIAS_TABLE_SIZE];


            Int32 totalWildCount = this.rng.ChooseMe(internalState.force, explodingWildsCountBias, mood);
            Int32 totalWildCountRemaining = totalWildCount;

            Int32 wildCountPerStage;
            Int32 reel, row, offset;
            long finalWin;



            ClearExplodingWilds(gameSpin);

            // prepare the bias tables:
            MassageBiasTableUsingMaxAllowableFrames(internalState, gameSpin, toWildPositionBias, toWildBiasLookUpByFrameState, maxAllowableFrames);

            // fromWildPositionBias[] is valid only where a WILD has initially landed
            for (row = 0, offset = 0; row < REEL_WINDOW; row++)
            {
                for (reel = 0; reel < NUMBER_REELS; reel++, offset++)
                {
                    gameSpin.finalReelWindow[reel, row] = gameSpin.spinInReelWindow[reel, row];
                    gameSpin.finalMultiplierWindow[reel, row] = gameSpin.spinInMultiplierWindow[reel, row];

                    if (gameSpin.spinInReelWindow[reel, row] == LOGIC_SYMBOL_WILD)
                        fromWildPositionBias[offset] = 1;
                    else
                        fromWildPositionBias[offset] = 0;
                }
            }


            for (; ; )
            {
                Int32 fromIndex, toIndex;
                bool forcedExit = false;
                Int32 maxMultiplier;

                wildCountPerStage = this.rng.ChooseMe(internalState.force, explodingWildsCountPerStageBias);
                if (totalWildCountRemaining < wildCountPerStage)
                    wildCountPerStage = totalWildCountRemaining;

                fromIndex = this.rng.ChooseMeAllowNegativeBias(internalState.force, fromWildPositionBias);

                //assert(fromIndex!=-1);
                fromWildPositionBias[fromIndex] = 0;            // each WILD can only be an explosion source once
                                                                //reel=fromIndex%NUMBER_REELS;
                                                                //row=fromIndex/NUMBER_REELS;

                if (mode == GAME_MODE_BASEGAME)
                    maxMultiplier = MAX_FRAME_MULTIPLIER;
                else
                    maxMultiplier = MAX_FRAME_MULTIPLIER;

                TemporarilyInvalidateBiasTableEntriesAtMaxFrameWindow(internalState, gameSpin, toWildPositionBias, maxMultiplier);

                // invalidate fromIndex position (if active), we don't want a WILD to explode onto itself
                if (toWildPositionBias[fromIndex] > 0)
                    toWildPositionBias[fromIndex] = -toWildPositionBias[fromIndex];

                Int32 fromReel = fromIndex % NUMBER_REELS;
                Int32 fromRow = fromIndex / NUMBER_REELS;

                List<List<int>> modifiedReelWindow = ArrayUtils.ToList(gameSpin.finalReelWindow).Select(newReel => newReel.Select(val => 0).ToList()).ToList();
                modifiedReelWindow[fromReel][fromRow] = 1;

                // place the WILDS in this stage
                for (Int32 n = 0; n < wildCountPerStage; n++)
                {
                    toIndex = this.rng.ChooseMeAllowNegativeBias(internalState.force, toWildPositionBias);
                    if (toIndex == -1)
                    {
                        // we need to fail gracefully if we fail to find a position to explode to
                        forcedExit = true;
                        break;
                    }

                    toWildPositionBias[toIndex] = -toWildPositionBias[toIndex];             // temporarily invalidate this position
                    fromWildPositionBias[toIndex] = 1;                                      // mark a new position that WILDS can explode from on the next stage

                    Int32 toReel = toIndex % NUMBER_REELS;
                    Int32 toRow = toIndex / NUMBER_REELS;
                    AddExplodingWild(internalState, gameSpin, fromIndex, toIndex);
                    totalWildCountRemaining--;
                    modifiedReelWindow[toReel][toRow] = 2;
                }

                gameSpin.explodingWilds.results.Add(new Spin
                {
                    id = FeatureIds.ExplodingWild,
                    reelWindow = ReelUtils.GetReelWindow(ArrayUtils.ToArray(modifiedReelWindow), internalState.persistentData.frameWindow)
                });

                if (forcedExit)
                    break;

                gameSpin.explodingWilds.explodingWildsListIndex = 0;
                gameSpin.explodingWilds.explodingWildsStageIndex++;

                if (totalWildCountRemaining == 0)
                    break;          // all WILDS placed

                if (gameSpin.explodingWilds.explodingWildsStageIndex == MAX_EXPLODING_WILDS_STAGES)
                    break;          // all stages filled (this could mean some WILDS might not have been placed)

                // restore any temporarily invalidated positions in toWildPositionBias[], ready for the next stage
                for (Int32 n = 0; n < REELS_BIAS_TABLE_SIZE; n++)
                    if (toWildPositionBias[n] < 0)
                        toWildPositionBias[n] = -toWildPositionBias[n];

            }

            gameSpin.explodingWilds.stageCount = gameSpin.explodingWilds.explodingWildsStageIndex;

            // clear willDoExplodingWilds if we failed to add any 
            if (gameSpin.explodingWilds.stageCount == 0)
                gameSpin.willDoExplodingWilds = false;
            else
                gameSpin.explodingWilds.results.Add(new Spin
                {
                    id = FeatureIds.ExplodingWildEnd,
                    reelWindow = ReelUtils.GetReelWindow(gameSpin.finalReelWindow, internalState.persistentData.frameWindow)
                });

            var scratchWinList = this.payWays.CalculateTotal(internalState, gameSpin.finalReelWindow, gameSpin.finalMultiplierWindow, NUMBER_WIN_LINES);
            finalWin = scratchWinList.coinWin;

            return finalWin;
        }

        public void AddExplodingWilds(InternalState internalState, GameSpinState gameSpin)
        {
            long finalWin, finalWinCash;
            long bet = internalState.bet;

            bool validEW = false;

            this.gameSpin.SaveFrameWindow(internalState);

            for (Int32 n = 0; n < MAX_EXPLODING_WILDS_ATTEMPTS; n++)
            {
                this.gameSpin.RestoreFrameWindow(internalState);

                gameSpin.explodingWilds.results.Clear();
                finalWin = AddExplodingWildsAttempt(internalState, gameSpin, MAX_ALLOWABLE_FRAMES_BASEGAME, FS_MOOD_1, GAME_MODE_BASEGAME);

                finalWin *= gameSpin.multiplier;
                finalWinCash = this.payWays.ConvertCoinValueToCash(finalWin, internalState.coinValue);

                if (finalWinCash == 0)
                    continue;           // win is too small

                if (finalWinCash < MAX_EX_STAKE_MULTIPLIE_WIN_UNCONTESTED * bet)
                {
                    validEW = true;
                    break;      // win size is OK
                }
                else if (finalWinCash < MAX_EW_STAKE_MULTIPLE_WIN * bet)
                {
                    // 30% chance of allowing 41-100x stake
                    if (this.rng.XInY(internalState.force, 30, 100))
                    {
                        validEW = true;
                        break;
                    }
                }
                else
                {
                    // 5% chance of allowing anything over 100x stake
                    if (this.rng.XInY(internalState.force, 5, 100))
                    {
                        validEW = true;
                        break;
                    }
                }
            }

            if (!validEW)
                this.gameSpin.RestoreFrameWindow(internalState);
            else
                internalState.features.AddRange(gameSpin.explodingWilds.results);

        }


    }
}