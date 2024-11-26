
using System;
using System.Linq;
using Service.LogicCommon;
using Service.LogicCommon.Rng;
using Service.LogicCommon.Utils;
using static Service.Logic.GameDefs;

using static Service.LogicCommon.FeatureHelper;

namespace Service.Logic
{
    class FreeSpinsState
    {
        public Int32 freeSpinsTrailIndex;
        public Int32 freeSpinsMultiplier;
    }


    class FreeSpins
    {

        public readonly Int32[] trailCompositionList =
        {
            FSTT_BIG_HIT,
            FSTT_BIG_HIT,
            FSTT_5_SPINS_X2_MULTIPLIER,
            FSTT_BIG_HIT,
            FSTT_BIG_HIT,
            FSTT_5_SPINS_X3_MULTIPLIER,
            FSTT_BIG_HIT,
            FSTT_BIG_HIT,
            FSTT_5_SPINS_X5_MULTIPLIER,
            FSTT_BIG_HIT,
            FSTT_BIG_HIT,
            FSTT_5_SPINS_X10_MULTIPLIER,
        };
        private Rng rng;
        private PayWays payWays;
        private BandSet bandSet;
        private GameSpin gameSpin;
        private BaseGame baseGame;
        private ExplodingWilds explodingWilds;


        // *******************************************************************************
        public FreeSpins(Rng rng, PayWays payWays, BandSet bandSet, GameSpin gameSpin, BaseGame baseGame, ExplodingWilds explodingWilds)
        {
            this.rng = rng;
            this.payWays = payWays;
            this.bandSet = bandSet;
            this.gameSpin = gameSpin;
            this.baseGame = baseGame;
            this.explodingWilds = explodingWilds;
        }

        // ***************************************************************************************


        // given a finalTrailIndex, returns how many extra spins will be earned
        Int32 GetExtraSpinCount(Int32 finalTrailndex)
        {
            Int32 trailTypeIndex, extraSpinsCount = 0;

            for (Int32 n = 0; n <= finalTrailndex; n++)
            {
                trailTypeIndex = trailCompositionList[n];
                if (trailTypeIndex > FSTT_BIG_HIT)
                    extraSpinsCount += FS_SPIN_COUNT_INCREMENT;
            }

            return extraSpinsCount;
        }


        // adjusts the bias values either side of the passed index
        void TweakNeighbourBiases(Int32[] biasTable, Int32 length, Int32 index, Int32 delta, Int32 minimum)
        {
            // left of index
            if (index > 0)
            {
                // we ignore zero [invalid], or -ve [temporarily invalid]
                if (biasTable[index - 1] > 0)
                {
                    biasTable[index - 1] -= delta;
                    if (biasTable[index - 1] < minimum)
                        biasTable[index - 1] = minimum;
                }
            }

            // right of index
            if (index < length - 1)
            {
                if (biasTable[index + 1] > 0)
                {
                    biasTable[index + 1] -= delta;
                    if (biasTable[index + 1] < minimum)
                        biasTable[index + 1] = minimum;
                }
            }
        }


        Int32 GetFrameMultiplierTotal(InternalState internalState)
        {
            Int32 totalMultiplier = 0;

            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                    totalMultiplier += internalState.persistentData.frameWindow[reel, row];

            return totalMultiplier;
        }

        // returns TRUE if the passed gameSpin->spinInReelWindow contains at least 1 WILD
        bool SpinInContainsWild(GameSpinState gameSpin)
        {
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
            {
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    if (gameSpin.spinInReelWindow[reel, row] == LOGIC_SYMBOL_WILD)
                        return true;
                }
            }

            return false;
        }



        // we will have pre-validated that the trailIndex can be incremented ( ie is not already at the final position)
        // returns the spinCount increment (or zero)
        Int32 IncrementTrail(FreeSpinsState freeSpinsState)
        {
            Int32 trailTypeIndex, spinCountIncrement = 0;
            Int32[] multiplierLookUp = { 0, 2, 3, 5, 10 };

            freeSpinsState.freeSpinsTrailIndex++;
            trailTypeIndex = trailCompositionList[freeSpinsState.freeSpinsTrailIndex];
            if (trailTypeIndex > FSTT_BIG_HIT)
            {
                spinCountIncrement = FS_SPIN_COUNT_INCREMENT;
                freeSpinsState.freeSpinsMultiplier = multiplierLookUp[trailTypeIndex];
            }

            return spinCountIncrement;
        }


        // creates a bitfield of all the active frames
        Int32 CreateFrameBits(InternalState internalState)
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



        // some of these bools should probably be exclusive, ie if bigHit is landing, we don't also want explodingWilds on this spin,
        // since it will visually clutter the bigHit transition
        Int32 CreateGameSpin(InternalState internalState, BaseGameState baseGameState, FreeSpinsState freeSpinsState, GameSpinState gameSpinState, Int32 spinIndex, Int32 mood, Int32 maxAllowableFrames, bool hasExplodingWilds, bool hasSmallWin, bool hasBigHit, int spinsRemaining)
        {
            Int32 spinCountIncrement = 0;

            gameSpinState.results.Add(new RequestNextStage { id = FeatureIds.FreeSpin });

            this.gameSpin.ResetGameSpin(gameSpinState, freeSpinsState.freeSpinsMultiplier, spinIndex);
            gameSpinState.spinConfig.spinType = GAME_MODE_FREE_SPINS;

            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                for (Int32 row = 0; row < REEL_WINDOW; row++)
                    gameSpinState.spinInFrameWindow[reel, row] = internalState.persistentData.frameWindow[reel, row];

            var multiplierAtStart = freeSpinsState.freeSpinsMultiplier;
            var trailIndexAtStart = freeSpinsState.freeSpinsTrailIndex;

            if (hasBigHit)
            {
                spinCountIncrement = IncrementTrail(freeSpinsState);
                gameSpinState.willDoBigHit = true;
                gameSpinState.multiplier = freeSpinsState.freeSpinsMultiplier;
            }

            gameSpinState.willDoExplodingWilds = hasExplodingWilds;

            if (gameSpin.SpinRandomReels(internalState, gameSpinState, maxAllowableFrames, hasSmallWin, hasBigHit, GAME_MODE_FREE_SPINS) == 0)
                gameSpinState.willDoExplodingWilds = false;         // this would be invalid if we didn't add at least one initial WILD

            gameSpinState.results.Add(new FreeSpin
            {
                id = FeatureIds.FreeSpin,
                reelWindow = ReelUtils.GetReelWindow(gameSpinState.spinInReelWindow, internalState.persistentData.frameWindow),
                reelSetId = gameSpinState.bandset,
                stopPositions = gameSpinState.stopPositions.ToList(),
                spinsAdded = spinCountIncrement,
                spinsRemaining = spinsRemaining + spinCountIncrement
            });

            gameSpinState.results.Add(new Collection { id = FeatureIds.Trail, value = freeSpinsState.freeSpinsTrailIndex + 1, update =  freeSpinsState.freeSpinsTrailIndex - trailIndexAtStart });
            gameSpinState.results.Add(new Collection { id = FeatureIds.Multiplier, value = freeSpinsState.freeSpinsMultiplier, update = freeSpinsState.freeSpinsMultiplier - multiplierAtStart });

            if (gameSpinState.willDoExplodingWilds)
            {
                explodingWilds.AddExplodingWildsAttempt(internalState, gameSpinState, maxAllowableFrames, mood, GAME_MODE_FREE_SPINS);
                if (gameSpinState.willDoExplodingWilds)
                {
                    gameSpinState.results.AddRange(gameSpinState.explodingWilds.results);
                }
            }
            else
                gameSpin.CopySpinInToFinalFreeSpins(internalState, gameSpinState);

            if (gameSpinState.willDoBigHit)
            {
                gameSpin.SetFramedSymbolsWild(internalState, gameSpinState);
                gameSpinState.results.Add(new Spin
                {
                    id = FeatureIds.BigHit,
                    reelWindow = ReelUtils.GetReelWindow(gameSpinState.finalReelWindow, internalState.persistentData.frameWindow),
                    stopPositions = gameSpinState.stopPositions.ToList(),
                });
            }

            gameSpinState.frameBits = CreateFrameBits(internalState);

            gameSpin.CalculateWins(internalState, gameSpinState);                                       // now we can calculate our wins


            if (gameSpinState.totalWinInCoins > 0)
            {
                PayLinePayout payout = new PayLinePayout
                {
                    id = FeatureIds.FreeSpin,
                    winDetails = gameSpinState.winDetails,
                    win = gameSpinState.winDetails.cashWin
                };
                if(gameSpinState.willDoBigHit) payout.id = FeatureIds.BigHit;
                if(gameSpinState.willDoExplodingWilds) payout.id = FeatureIds.ExplodingWild;

                gameSpinState.results.Add(payout);
            }

            return spinCountIncrement;
        }


        void SetSpinInReels(InternalState internalState, GameSpinState gameSpin, Int32 bandSet)
        {
            gameSpin.bandset = bandSet;

            // spin the reels to a random position
            for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
            {
                Int32 length = this.bandSet.GetBandSetReelLength(bandSet, reel);
                gameSpin.stopPositions[reel] = this.rng.Next(internalState.force, length);

                for (Int32 row = 0; row < REEL_WINDOW; row++)
                {
                    gameSpin.spinInReelWindow[reel, row] = this.bandSet.GetSymbol(gameSpin.bandset, reel, gameSpin.stopPositions[reel] + row);
                    gameSpin.finalReelWindow[reel, row] = gameSpin.spinInReelWindow[reel, row];
                }
            }
        }


        // given a winlineIndex, find another random one that doesn't intersect with it
        // this assume both lines are full length (5 reels)
        Int32 FindNonIntersectingWinLine(InternalState internalState, Int32 winLineIndex)
        {
            Int32 row1, row2;
            Int32 otherWinLineIndex, matchCount;

            otherWinLineIndex = this.rng.Next(internalState.force, NUMBER_WIN_LINES);

            for (Int32 n = 0; n < NUMBER_WIN_LINES; n++)
            {
                // only test if they are different
                if (otherWinLineIndex != winLineIndex)
                {
                    matchCount = 0;
                    for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                    {
                        row1 = PAYLINES[winLineIndex, reel];
                        row2 = PAYLINES[otherWinLineIndex, reel];
                        if (row1 == row2)
                            matchCount++;
                    }

                    if (matchCount == 0)
                    {
                        //no intersection, we have a suitable otherWinLineIndex
                        return otherWinLineIndex;
                    }
                }

                otherWinLineIndex++;
                otherWinLineIndex %= NUMBER_WIN_LINES;
            }

            return -1;      // should not be possible
        }





        // adds a win of 5x, 10x, or 15x stake (sizeIndex)
        void AddSmallWin(InternalState internalState, GameSpinState gameSpinState, Int32 sizeIndex)
        {
            Int32[] winLineBias = { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

            Int32 winCount;
            Int32 winLine = 0, winningSymbol;

            Int32[] winCountLookUp = { 1, 1, 2 };
            Int32[,] winSymbolLookUp =
            {
                { LOGIC_SYMBOL_HIGH2, -1},						// 0 (5x stake)
				{ LOGIC_SYMBOL_HIGH1, -1},						// 1 (10x stake)
				{ LOGIC_SYMBOL_HIGH1, LOGIC_SYMBOL_HIGH2},		// 2 (15x stake)
			};


            // start with a losing spin
            SetSpinInReels(internalState, gameSpinState, LOGIC_BANDSET_LOSING);

            winCount = winCountLookUp[sizeIndex];

            for (Int32 n = 0; n < winCount; n++)
            {
                Int32 row;

                if (n == 0)
                {
                    winLine = this.rng.ChooseMe(internalState.force, winLineBias);
                    winLineBias[winLine] = 0;
                }
                else
                    winLine = FindNonIntersectingWinLine(internalState, winLine);

                winningSymbol = winSymbolLookUp[sizeIndex, n];

                for (Int32 reel = 0; reel < NUMBER_REELS; reel++)
                {
                    row = PAYLINES[winLine, reel];
                    gameSpinState.spinInReelWindow[reel, row] = winningSymbol;
                    gameSpinState.finalReelWindow[reel, row] = winningSymbol;
                }
            }

            gameSpin.CalculateWins(internalState, gameSpinState);
        }

        // takes a signed bias table, and activates the passed length, ie -ve elements become +ve, zero elements ignored
        void ActivateBiasTableSection(Int32[] biasTable, int activationLength)
        {
            for (int n = 0; n < activationLength; n++)
            {
                if (biasTable[n] < 0)
                    biasTable[n] = -biasTable[n];
            }
        }



        long CreateFreeSpinsBonusAttempt(InternalState internalState, FreeSpinsResult freeSpinsResult, Int32 scatterCountIndex, Int32 mood, bool fastStart)
        {
            Int32[] startingSpinCount = { 10, 15, 20 };
            //GameSpin				gameSpin;

            Int32 spinCount = startingSpinCount[scatterCountIndex];

            bool hasExplodingWilds = false, hasSmallWin = false, hasBigHit = false;
            Int32 explodingWildsActive = FB_EXP_WILDS;
            var stakeInPence = internalState.bet;

            Int32[,] finalTrailIndexByMoodBias =
            {
				//		0,		1,		2,		3,		4,		5,		6,		7,		8,		9,		10,		11
				//		BH,		BH,		+5/x2,	BH,		BH,		+5/x3,	BH,		BH,		+5/x4,	BH,		BH,		+5/x5
				//------------------------------------------------------------------------------------------------------

				{ 5,        10,     1,      0,      0,      0,      0,      0,      0,      0,      0,      0       },
                { 0,        5,      10,     15,     5,      1,      0,      0,      0,      0,      0,      0       },
                { 0,        0,      5,      10,     10,     5,      1,      0,      0,      0,      0,      0       },
                { 0,        0,      3,      5,      10,     10,     5,      2,      1,      0,      0,      0,      },
                { 0,        0,      0,      5,      12,     15,     12,     10,     6,      4,      2,      1       },
            };

            Int32[,] maxAllowableFramesByMoodBias =
            {
				//	0,	1,	2,	3,	4,	5,	6,	7,	8,	9,	10,	11,	12,	13,	14,	15,	16,	17, 18, 19, 20
				//------------------------------------------------------------------------------------------------------	

				{ 0,    0,  0,  0,  0,  0,  0,  0,  0,  0,  5,  5,  5,  5,  5,  0,  0,  0,  0,  0,  0 },	// FS_MOOD_0
				{ 0,    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  5,  5,  5,  5,  5,  0,  0,  0,  0,  0 },	// FS_MOOD_1
				{ 0,    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  5,  5,  5,  5,  5,  0,  0,  0,  0 },	// FS_MOOD_2
				{ 0,    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  5,  5,  5,  5,  5,  0,  0,  0 },	// FS_MOOD_3
				{ 0,    0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  0,  5,  5,  5,  5,  5,  0,  0 },	// FS_MOOD_4
		 	};

            Int32[,] explodingWildsByMoodBias =
            {
				//		0,		1,		2,		3,		4,		5,
				//------------------------------------------------------------------

				{ 1,        7,      10,     2,      0,      0},		// FS_MOOD_0
				{ 0,        5,      10,     5,      0,      0},		// FS_MOOD_1
				{ 0,        2,      10,     5,      1,      0},		// FS_MOOD_2
				{ 0,        0,      5,      10,     2,      0},		// FS_MOOD_3
				{ 0,        0,      5,      20,     2,      1},		// FS_MOOD_4
			};


            Int32[,] forcedWinsByMoodBias =
            {
				//	0,		1,		2
				//---------------------------
				{   80,     20,     1},			// FS_MOOD_0
				{   20,     5,      1},			// FS_MOOD_1
				{   30,     10,     2},			// FS_MOOD_2
				{   40,     10,     5},			// FS_MOOD_3
				{   40,     8,      8},			// FS_MOOD_4
			};


            // this defines how close to the maxMultiplierTotal each mood can reach
            float[] maxAllowableMultiplierScalerByMood = { 0.3f, 0.5f, 0.4f, 0.3f, 0.2f };

            // minimum stake multiple
            Int32[] minimumStakeMultipleByScatterCountIndex = { 5, 10, 15 };
            Int32 finalTrailIndex = this.rng.ChooseMe(internalState.force, finalTrailIndexByMoodBias, mood);
            Int32 bigHitCount = finalTrailIndex + 1;
            Int32 finalSpinCount = spinCount + GetExtraSpinCount(finalTrailIndex);
            Int32 maxAllowableFrames = this.rng.ChooseMe(internalState.force, maxAllowableFramesByMoodBias, mood);
            Int32 maxAllowableMultiplierTotal = (Int32)((MAX_FRAME_MULTIPLIER * maxAllowableFrames) * maxAllowableMultiplierScalerByMood[mood]);
            Int32 explodingWildsCount = this.rng.ChooseMe(internalState.force, explodingWildsByMoodBias, mood);
            Int32 forcedWinCount = this.rng.ChooseMe(internalState.force, forcedWinsByMoodBias, mood);
            Int32[] fastStartFinalTrailIndexDeltaBias = { 0, 120, 40, 10 };

            // this will be used to select which spins will receive features
            Int32[] spinIndexBias = new Int32[LOGIC_MAX_FREESPINS];

            // this will contain the feature bits for each spin
            Int32[] featureBitsPerSpin = new Int32[LOGIC_MAX_FREESPINS];
            Int32 bigHitAddedCount, activationCount;





            internalState.freeSpinsState.freeSpinsTrailIndex = -1;            // we start 'off' the trail
            internalState.freeSpinsState.freeSpinsMultiplier = 1;         // at x1 multiplier

            freeSpinsResult.totalWinInCoins = 0;
            freeSpinsResult.totalWinInCash = 0;
            freeSpinsResult.initialSpinCount = spinCount;       // starting spin count

            // clear previous freeSpinList
            freeSpinsResult.freeSpinList.Clear();


            // fastStart starts FS with the trail at the 3rd trail element,
            // so multiplier starts at x2, and the finalTrailIndex is a minimum of 2

            freeSpinsResult.fastStart = fastStart;
            if (fastStart)
            {
                internalState.freeSpinsState.freeSpinsTrailIndex = 2;
                internalState.freeSpinsState.freeSpinsMultiplier = 2;
                finalTrailIndex += this.rng.ChooseMe(internalState.force, fastStartFinalTrailIndexDeltaBias);
                if (finalTrailIndex < internalState.freeSpinsState.freeSpinsTrailIndex)
                    finalTrailIndex = internalState.freeSpinsState.freeSpinsTrailIndex;
                if (finalTrailIndex >= MAX_FREE_SPINS_TRAIL_LENGTH)
                    finalTrailIndex = MAX_FREE_SPINS_TRAIL_LENGTH - 1;
                finalSpinCount = spinCount + GetExtraSpinCount(finalTrailIndex);
                spinCount += FS_SPIN_COUNT_INCREMENT;
                bigHitCount = finalTrailIndex + 1 - 3;
            }


            for (Int32 n = 0; n < LOGIC_MAX_FREESPINS; n++)
            {
                // clear feature bits for all spins
                featureBitsPerSpin[n] = 0;

                // prepare initial bias for selecting BigHits
                if (n < finalSpinCount)
                    spinIndexBias[n] = -100;
                else
                    spinIndexBias[n] = 0;
            }

            activationCount = spinCount;
            ActivateBiasTableSection(spinIndexBias, activationCount);           // set the inital range of valid elements
            bigHitAddedCount = 0;

            // first select spins to contain bigHits
            for (Int32 n = 0; n < bigHitCount; n++)
            {
                Int32 bigHitIndex = this.rng.ChooseMeAllowNegativeBias(internalState.force, spinIndexBias);


                featureBitsPerSpin[bigHitIndex] |= FB_BIG_HIT;
                spinIndexBias[bigHitIndex] = 0;
                TweakNeighbourBiases(spinIndexBias, LOGIC_MAX_FREESPINS, bigHitIndex, 40, 10);

                bigHitAddedCount++;
                if (bigHitAddedCount == 3)
                {
                    // if we have added 3, we can extend the range for bigHit additions
                    bigHitAddedCount = 0;
                    activationCount += FS_SPIN_COUNT_INCREMENT;

                    ActivateBiasTableSection(spinIndexBias, activationCount);
                }
            }

            ActivateBiasTableSection(spinIndexBias, finalSpinCount);

            // now exploding wilds. since we don't reset spinIndexbias[], this will ensure we don't place
            // them on the same spins as the bigHits
            for (Int32 n = 0; n < explodingWildsCount; n++)
            {
                Int32 explodingWildsIndex = this.rng.ChooseMeAllowNegativeBias(internalState.force, spinIndexBias);

                featureBitsPerSpin[explodingWildsIndex] |= FB_EXP_WILDS;
                spinIndexBias[explodingWildsIndex] = 0;
                TweakNeighbourBiases(spinIndexBias, LOGIC_MAX_FREESPINS, explodingWildsIndex, 40, 10);
            }


            // finally forced wins, this may be zero
            for (Int32 n = 0; n < forcedWinCount; n++)
            {
                Int32 forcedWinIndex = this.rng.ChooseMeAllowNegativeBias(internalState.force, spinIndexBias);


                featureBitsPerSpin[forcedWinIndex] |= FB_FORCED_WIN;
                spinIndexBias[forcedWinIndex] = 0;
                TweakNeighbourBiases(spinIndexBias, LOGIC_MAX_FREESPINS, forcedWinIndex, 40, 10);
            }


            for (Int32 n = 0; n < finalSpinCount; n++)
            {
                // add a GameSpin
                GameSpinState gameSpin = new GameSpinState();

                freeSpinsResult.freeSpinList.Add(gameSpin);
                gameSpin.spinIndex = n;

                hasExplodingWilds = false;
                hasSmallWin = false;
                hasBigHit = false;


                if ((featureBitsPerSpin[n] & FB_BIG_HIT) != 0)
                    hasBigHit = true;

                if ((featureBitsPerSpin[n] & FB_FORCED_WIN) != 0)
                    hasSmallWin = true;

                if ((featureBitsPerSpin[n] & explodingWildsActive) != 0)
                    hasExplodingWilds = true;

                spinCount += CreateGameSpin(internalState, internalState.baseGameState, internalState.freeSpinsState, gameSpin, n, mood, maxAllowableFrames, hasExplodingWilds, hasSmallWin, hasBigHit, spinCount - n - 1);
                freeSpinsResult.totalWinInCoins += gameSpin.totalWinInCoins;


                if (explodingWildsActive > 0)
                {
                    // switch off further exploding wilds if we exceed the allowable multiplier total
                    if (GetFrameMultiplierTotal(internalState) > maxAllowableMultiplierTotal)
                        explodingWildsActive = 0;
                }

            }

            // add a failsafe small win here, if we haven't reached the minimum stake multiple
            if (freeSpinsResult.totalWinInCoins < 100 * minimumStakeMultipleByScatterCountIndex[scatterCountIndex])
            {
                Int32 failSafeSpinIndex;
                bool hasWild;
                GameSpinState gameSpinState;


                // create a fresh spinIndexBias[] to choose the spin
                for (Int32 n = 0; n < finalSpinCount; n++)
                {
                    gameSpinState = freeSpinsResult.freeSpinList[n];
                    hasWild = SpinInContainsWild(gameSpinState);

                    // a spin is a valid choice if it contained no featureBits, won nothing, and contains no WILDS
                    if (featureBitsPerSpin[n] == 0 && gameSpinState.totalWinInCoins == 0 && hasWild == false)
                        spinIndexBias[n] = 1;
                    else
                        spinIndexBias[n] = 0;
                }

                failSafeSpinIndex = this.rng.ChooseMe(internalState.force, spinIndexBias);
                if (failSafeSpinIndex != -1)
                {
                    gameSpinState = freeSpinsResult.freeSpinList[failSafeSpinIndex];
                    var fsFeature = GetSingleFeature<FreeSpin>(gameSpinState.results);

                    var collections = GetFeature<Collection>(gameSpinState.results);

                    gameSpinState.results.Clear();
                    AddSmallWin(internalState, gameSpinState, scatterCountIndex);

                    gameSpinState.results.Add(new RequestNextStage { id = FeatureIds.FreeSpin });
                    gameSpinState.results.Add(new FreeSpin
                    {
                        id = FeatureIds.FreeSpin,
                        reelSetId = gameSpinState.bandset,
                        reelWindow = ReelUtils.GetReelWindow(gameSpinState.spinInReelWindow, gameSpinState.spinInFrameWindow),
                        stopPositions = gameSpinState.stopPositions.ToList(),
                        spinsAdded = fsFeature.spinsAdded,
                        spinsRemaining = fsFeature.spinsRemaining
                    });
                    if (gameSpinState.totalWinInCoins > 0)
                        gameSpinState.results.Add(new PayLinePayout
                        {
                            id = FeatureIds.FreeSpin,
                            winDetails = gameSpinState.winDetails,
                            win = gameSpinState.winDetails.cashWin
                        });

                    freeSpinsResult.totalWinInCoins += gameSpinState.totalWinInCoins;
                    gameSpinState.results.AddRange(collections);
                }
            }



            //assert(finalSpinCount == spinCount);

            freeSpinsResult.spinCount = spinCount;      // final spin count
            freeSpinsResult.totalWinInCash = this.payWays.ConvertCoinValueToCash(freeSpinsResult.totalWinInCoins, internalState.coinValue);

            return freeSpinsResult.totalWinInCoins;
        }

        // returns the total win for a valid FS game, or zero if we failed to generate one within the max number of re-tries
        // persistent frame state is preserved in the event of failure. if alwaysFreeSpins is set for a buy bonus, we will
        // preserve the frameWindow, clear it, and then restore after generating the FS bonus
        public long CreateFreeSpinsBonus(InternalState internalState, FreeSpinsResult freeSpinsResult, Int32 scatterCountIndex, bool fastStart)
        {
            //			Int32		mti=CopsAndRobbersthis.rng.GetMT();


            Int32[,] moodBias =
            {
                { 10,10,10,10,10 },
                { 10,10,10,17, 0 },		// used with fastStart active
			};

            long totalWin, totalWinInCash;
            long bet = internalState.bet;

            Int32[] minimumStakeMultipleLookUp = { 5, 10, 15 };
            Int32[] maximumStakeMultipleLookUp = { 100, 250, 350 };

            Int32 mood = this.rng.ChooseMe(internalState.force, moodBias, Convert.ToInt32(fastStart));
            Int32 reTryCount, reTryCountTooSmall = 0, reTryCountTooLarge = 0;
            bool validFS = true;
            Int32 activeFrameCount;


            if (internalState.buyBonus)
            {
                this.gameSpin.SaveBuyBonusFrameWindow(internalState);
                freeSpinsResult.buyBonusResult = true;
            }

            activeFrameCount = this.gameSpin.GetActiveFrameCount(internalState);



            // since frames are now part of the base game, and inherited by FS, we need to cache the starting state,
            // and restore it below, for each attempt at creating a FS bonus
            this.gameSpin.SaveFreeSpinsFrameWindow(internalState);


            internalState.freeSpinsState = new FreeSpinsState();

            for (reTryCount = 0; ; reTryCount++)
            {
                this.gameSpin.RestoreFreeSpinsFrameWindow(internalState);

                totalWin = CreateFreeSpinsBonusAttempt(internalState, freeSpinsResult, scatterCountIndex, mood, fastStart);
                totalWinInCash = this.payWays.ConvertCoinValueToCash(totalWin, internalState.coinValue);

                //mti=CopsAndRobbersthis.rng.GetMT();

                if (totalWinInCash < bet * minimumStakeMultipleLookUp[scatterCountIndex])
                {
                    reTryCountTooSmall++;
                    continue;
                }


                // possible large win handling
                if (totalWinInCash >= bet * 1000)
                {

                    // between 5000x and 12500x stake
                    if (totalWinInCash > bet * 5000)
                    {
                        if (totalWinInCash <= bet * 12500 && this.rng.OneIn(internalState.force, 2000))
                            break;
                    }
                    else
                    {
                        // <5000x stake
                        if (this.rng.OneIn(internalState.force, 1150))
                            break;
                    }
                }

                // some filtering here for wins that are too large in the general case
                if (totalWinInCash > bet * maximumStakeMultipleLookUp[scatterCountIndex])
                {
                    reTryCountTooLarge++;

                    // if alwaysFreeSpins is active, we will just re-try until we get a valid result
                    if (reTryCountTooLarge >= MAX_TOO_LARGE_RETRIES && !internalState.buyBonus)
                    {
                        validFS = false;
                        break;
                    }
                    else
                        continue;
                }

                break;
            }



            if (internalState.buyBonus)
                this.gameSpin.RestoreBuyBonusFrameWindow(internalState);

            if (validFS)
            {
                internalState.features.Add(new InitFreeSpins { id=FeatureIds.FreeSpins, spinCount = freeSpinsResult.initialSpinCount });
                internalState.features.Add(new Collection { id = FeatureIds.Trail, value = freeSpinsResult.fastStart ? 3 : 0 });
                internalState.features.Add(new Collection { id = FeatureIds.Multiplier, value = freeSpinsResult.fastStart ? 2 : 1 });
                foreach (var fsResults in freeSpinsResult.freeSpinList.Select(freeSpin => freeSpin.results))
                {
                    internalState.features.AddRange(fsResults);
                }
                if (fastStart) {
                    internalState.features.Add(new Collection{id = FeatureIds.Scatters, value = 0, update = -internalState.persistentData.twoScatterProgress});
                    internalState.persistentData.twoScatterProgress = 0;
                }

                freeSpinsResult.type = scatterCountIndex;
                if (!internalState.buyBonus)
                    this.gameSpin.ResetFrameWindow(internalState);

                return totalWin;
            } 

            // clear invalid result
            freeSpinsResult.totalWinInCoins = 0;
            freeSpinsResult.totalWinInCash = 0;
            this.gameSpin.RestoreFreeSpinsFrameWindow(internalState);

            return 0;

        }

    }
}
