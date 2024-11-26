
using Service.LogicCommon.Rng;
using Service.LogicCommon.Utils;
using Service.LogicCommon;
using static Service.Logic.GameDefs;
using System;
using System.Linq;
using System.Collections.Generic;

using static Service.LogicCommon.FeatureHelper;

namespace Service.Logic
{
    public class Game
    {
        public readonly RTP targetRTP;

        private readonly Rng rng;
        private readonly GameConfig config;
        private readonly PayWays payWays;

        private readonly BaseGame baseGame;
        private GameSpin gameSpin;
        private ExplodingWilds explodingWilds;
        private FreeSpins freeSpins;

        public Game(ILoggerWrapper logger, Rng rng, GameConfig config, string rtpVariant = "96")
        {
#if TRACE
            Console.WriteLine("Initialising...");
#endif

            this.rng = rng;
            this.config = config;

            switch (rtpVariant)
            {
                case "94":
                    targetRTP = RTP.Target94;
                    break;
                case "92":
                    targetRTP = RTP.Target92;
                    break;
                case "86":
                    targetRTP = RTP.Target86;
                    break;
                default:
                    targetRTP = RTP.Target96;
                    break;
            }

            payWays = new PayWays();
            var bandset = new BandSet();
            gameSpin = new GameSpin(rng, payWays, bandset, targetRTP);
            explodingWilds = new ExplodingWilds(rng, payWays, gameSpin);
            baseGame = new BaseGame(rng, payWays, gameSpin, explodingWilds);
            freeSpins = new FreeSpins(rng, payWays, bandset, gameSpin, baseGame, explodingWilds);
        }

        private int[] gambleChances = new int[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 };
        public Dictionary<int, decimal> GetGambleCosts()
        {
            var gambleCostMap = new Dictionary<int, decimal>();

            foreach (var chance in gambleChances)
            {
                gambleCostMap[chance] = GetBuyBonusCost() * (chance / 100m);
            }

            return gambleCostMap;
        }

        public GameState GambleForBonus(long bet, int chance, InternalState internalState)
        {
            GameState result = new GameState{
                persistentDataMap = internalState.persistentDataMap
            };
            var gambleResult = new GambleForBonus { chance = chance };
            if (rng.ThousandChance(internalState.force, chance * 10))
            {
                gambleResult.gambleWon = true;
                var featureResults = new List<Feature> { gambleResult };
                result = DoGame(bet, true, internalState);
                featureResults.AddRange(result.features.Where(feature => !(feature is BuyBonus)));
                result.features = featureResults;
            }
            else
            {
                gambleResult.gambleWon = false;
                result.features = new List<Feature> { gambleResult };
            }

            result.cost = (long)(GetGambleCosts()[chance] * bet);
            gambleResult.bet = bet;
            gambleResult.cost = result.cost;
            return result;
        }

        // *******************************************************************************
        // This is the equivalent of Logic_DoGame()
        // *******************************************************************************
        public GameState DoGame(long bet, bool buyBonus, InternalState internalState)
        {
            internalState.bet = bet;
            var cost = bet;
            internalState.coinValue = bet / COINS_PER_BET;
            internalState.buyBonus = buyBonus;

            if (internalState.persistentDataMap != null)
            {
                if (internalState.persistentDataMap.ContainsKey(bet))
                {
                    internalState.persistentData = internalState.persistentDataMap[bet];
                }
            }

            if (internalState.buyBonus)
            {
                internalState.buyBonusCost = GetBuyBonusCost();
                cost = (long)(internalState.buyBonusCost * bet);
                internalState.persistentData = new PersistentData();
                internalState.features.Add(new BuyBonus { bet = bet, cost = cost });
                
                // if (internalState.persistentDataMap != null)
                //     internalState.persistentDataMap[bet] = new PersistentData();
            }

            internalState.initialPersistentData = ObjectUtils.Clone(internalState.persistentData);
            GameResults thisGame = new GameResults();

            if (internalState.persistentData == null)
            {
                Initialize(internalState);
            }

            thisGame.baseGameResult.clientRNGSeed = this.rng.Next(internalState.force);

            baseGame.CacheOnStartState(internalState);
            //ADDED
			baseGame.SaveFrameWindowAtGameStart(internalState);
			internalState.baseGameState.twoScatterProgressAtGameStart = internalState.persistentData.twoScatterProgress;
            //ADDED
            baseGame.DoSpin(internalState, thisGame, thisGame.baseGameResult);               // do the basegame spin
            if (thisGame.baseGameResult.willDoFreeSpins)
            {
                bool fastStart = false;

                // we can only set fastStart if not buying FS
                if (!buyBonus)
                    fastStart = (internalState.persistentData.twoScatterProgress == FULL_SCATTER_PROGRESS_BAR_COUNT);


                thisGame.winsFromFreeSpinsInCoins = freeSpins.CreateFreeSpinsBonus(internalState, thisGame.freeSpinsResult, thisGame.baseGameResult.freeSpinsMode, fastStart);   // and handle any freespins
                
                //ADDED
				if (thisGame.winsFromFreeSpinsInCoins==0)
				{
					// could not generate a valid FS, cancel it, and regenerate a basegame spin without scatters
                    thisGame = new GameResults();
					baseGame.RestoreGameStartFrameWindow(internalState);
					internalState.persistentData.twoScatterProgress=internalState.baseGameState.twoScatterProgressAtGameStart;

					thisGame.baseGameResult.willDoFreeSpins=false;
                    
					gameSpin.BaseGameDoOver(internalState, thisGame.baseGameResult.baseGameSpin);
                    internalState.features.Add(new Collection { id = FeatureIds.Scatters, value = internalState.persistentData.twoScatterProgress});
					thisGame.winsFromBaseGameInCoins=thisGame.baseGameResult.baseGameSpin.baseSpinWinInCoins;
				}
//ADDED
            }

            // If we are doing a buyBonus we should save the persistent data map state from the last non buy bonus
            if(!internalState.buyBonus)
                internalState.persistentDataMap[internalState.bet] = internalState.persistentData;

            Force force = null;
#if DEBUG
            force = new Force
            {
                cheat = internalState.force.usedNumbers,
                persistentData = internalState.initialPersistentData
            };
#endif


            internalState.coinWin = thisGame.winsFromBaseGameInCoins;
            internalState.coinWin += thisGame.winsFromExplodingWildsInCoins;
            internalState.coinWin += thisGame.winsFromBigHitInCoins;
            internalState.coinWin += thisGame.winsFromFreeSpinsInCoins;

            internalState.cashWin = payWays.ConvertCoinValueToCash(internalState.coinWin, internalState.coinValue);

            var result = new GameState
            {
                features = internalState.features,
                bet = bet,
                cost = cost,
                win = internalState.cashWin,
                coinWin = internalState.coinWin,
                persistentDataMap = internalState.persistentDataMap,
                force = force
            };

            return result;

        }

        public void Initialize(InternalState internalState)
        {
            internalState.persistentData = new PersistentData();
            internalState.persistentData.twoScatterProgress = 0;
            internalState.persistentData.spinsSinceLastWild = 0;
            gameSpin.ResetFrameWindow(internalState);
            gameSpin.InitialiseFirstXSpinElements(internalState);
            gameSpin.SelectBigHitMinimumStakeMultipleIndex(internalState);
            gameSpin.ChooseNewGuaranteedWildSpinCount(internalState);
        }

        internal decimal GetBuyBonusCost()
        {
            switch (this.targetRTP)
            {
                case RTP.Target86: return BONUS_BUY_COST_86 / 10m;
                case RTP.Target92: return BONUS_BUY_COST_92 / 10m;
                case RTP.Target94: return BONUS_BUY_COST_94 / 10m;
                default: return BONUS_BUY_COST_96 / 10m; // 96
            }
        }


        public GameClientConfig GetConfig()
        {
            var awards = new Dictionary<int, int[]>();
            var awardArray = ArrayUtils.ToList(GameDefs.AWARDS);
            for (var i = 0; i < awardArray.Count(); i++)
            {
                var award = awardArray[i];
                int symbolId = award[0];
                awards.Add(symbolId, award.Skip(1).ToArray());
            }

            var configResponse = new GameClientConfig
            {
                reelSets = config.reelSets,
                buyBonusCost = GetBuyBonusCost(),
                gambleForBonusCosts = GetGambleCosts(),
                awards = awards,
                symbols = config.symbols,
                payLines = config.payLines.Take(GameDefs.NUMBER_LINES).ToList(),
            };

            return configResponse;
        }

        internal static string GetReelSetName(int reelSetId)
        {
            switch (reelSetId)
            {
                case LOGIC_BANDSET_LOSING:
                    return "Losing";
                case LOGIC_BANDSET_NORMAL:
                    return "Normal";
                case LOGIC_BANDSET_JUST_LOW:
                    return "JustLow";
                default:
                    throw new Exception("Unrecognized reelSetId");
            }
        }
    }


}
