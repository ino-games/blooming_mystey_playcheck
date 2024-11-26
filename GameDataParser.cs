using static Service.Logic.GameDefs;
using Service.LogicCommon;
using Service.Logic;
using Service.PlayCheckCommon;
using System.Linq;
using System.Collections.Generic;

namespace BloomingMystery
{
    public class GameDataParser : GameDataParserBase
    {
        private MappedSpin savedSpin;
        private bool respinActive = false;
        private int currentMultiplier;
        private bool boostActive;

        public GameDataParser(ISymbolMapper symbolMapper) : base(symbolMapper)
        {
        }

        public override ParsedGameData ParseGameData(string gameData, string imagePath, string version = null)
        {
            ParsedGameData parsedGameData = base.ParseGameData(gameData, imagePath, version);
            parsedGameData.features = parsedGameData.features.Where(feature => feature.id != FeatureIds.ExplodingWildEnd);

            var reorderedFeatures = new List<Feature>();

            bool baseGameModeAdded = false;
            Feature trail = null;
            foreach (var feature in parsedGameData.features)
            {
                if (feature.type == FeatureTypes.FreeSpin && trail != null)
                {
                    reorderedFeatures.Add(trail);
                    trail = null;
                }
                if (feature.id == FeatureIds.Scatters && !baseGameModeAdded)
                {
                    reorderedFeatures.Add(new Feature { type = "gameMode", id = FeatureIds.BaseGame });
                    baseGameModeAdded = true;
                }
                if (feature.id != FeatureIds.Trail)
                {
                    reorderedFeatures.Add(feature);
                }
                else if (feature.id == FeatureIds.Trail)
                {
                    trail = feature;
                }
            }

            if (trail != null)
                reorderedFeatures.Add(trail);

            parsedGameData.features = reorderedFeatures;

            return parsedGameData;
        }

        protected override Feature MapFeatures(Feature feature, List<Feature> previousFeatures)
        {
            feature = base.MapFeatures(feature, previousFeatures);

            if (feature.type == FeatureTypes.Payout) {
                //some show win positions didnt setup in result, so we setup manually here based on payline index
                var paylines = new List<int[]> {
                    new int[] { 0, 0, 0, 0, 0 },
                    new int[] { 1, 1, 1, 1, 1 },
                    new int[] { 2, 2, 2, 2, 2 },
                    new int[] { 3, 3, 3, 3, 3 },
                    new int[] { 0, 1, 2, 1, 0 },
                    new int[] { 1, 2, 3, 2, 1 },
                    new int[] { 2, 1, 0, 1, 2 },
                    new int[] { 3, 2, 1, 2, 3 },
                    new int[] { 0, 1, 1, 1, 0 },
                    new int[] { 1, 2, 2, 2, 1 },
                    new int[] { 2, 3, 3, 3, 2 },
                    new int[] { 3, 2, 2, 2, 3 },
                    new int[] { 2, 1, 1, 1, 2 },
                    new int[] { 1, 0, 0, 0, 1 },
                    new int[] { 0, 1, 0, 1, 0 },
                    new int[] { 1, 2, 1, 2, 1 },
                    new int[] { 2, 3, 2, 3, 2 },
                    new int[] { 3, 2, 3, 2, 3 },
                    new int[] { 2, 1, 2, 1, 2 },
                    new int[] { 1, 0, 1, 0, 1 }
                };
                DecimalPayLinePayout decimalPayLinePayout = feature as DecimalPayLinePayout;
                var lastSpin = previousFeatures.FindLast(f => (f is MappedSpin || f is MappedFreeSpin) && f.id != "explodingWildEnd") as MappedSpin;
                decimalPayLinePayout.winDetails.spin = lastSpin;

                decimalPayLinePayout.winDetails.winList = decimalPayLinePayout.winDetails.winList.Select(winItem =>
                {
                    var winIndexes = paylines[winItem.payLineId];
                    for(var i = 0; i < winIndexes.Length; i++) {
                        var row = winIndexes[i];
                        if (i < winItem.symbolCount) {
                            //win symbol => set 1
                            winItem.symbolsInWin[i][row] = 1;
                            winItem.symbolsData[i][row] = lastSpin.reelWindow[i][row]; //load symbol data
                        } else {
                            //in payline but not win symbol => set -1
                            winItem.symbolsInWin[i][row] = -1;
                        }
                    }
                    return winItem;
                });
                return decimalPayLinePayout;
            }

            if (feature.type == FeatureTypes.Collection && feature.id == FeatureIds.Trail)
            {
                TrailCollection trailCollection = new TrailCollection(feature as Collection);
                if(boostActive) {
                    trailCollection.extraSpins = 5;
                    trailCollection.updatedMultiplier = 2;
                    boostActive = false;
                }
                return trailCollection;
            }

            if (feature.type == FeatureTypes.Collection && feature.id == FeatureIds.Multiplier)
            {
                this.currentMultiplier = (feature as Collection).value;
            }

            if (feature.type == FeatureTypes.Collection && feature.id == FeatureIds.Scatters)
            {
                ScatterCollection scatterCollection = new ScatterCollection(feature as Collection);
                if (scatterCollection.boostActive)
                    this.boostActive = true;
                return scatterCollection;
            }

            if (feature.type == FeatureTypes.Spin && feature.id == FeatureIds.ExplodingWild)
            {
                var spin = feature as MappedSpin;
                for (var x = 0; x < spin.reelWindow.Count(); x++)
                    for (var y = 0; y < spin.reelWindow[x].Count(); y++)
                    {
                        var symbol = spin.reelWindow[x][y];
                        var savedSymbol = savedSpin.reelWindow[x][y];

                        if (symbol.data[0] == 0)
                        {
                            symbol.symbolImage = savedSymbol.symbolImage;
                        }

                        if (symbol.data[0] == 1)
                        {
                            symbol.symbolImage = "wild_golden";
                            if (savedSymbol.data[0] == GameDefs.LOGIC_SYMBOL_WILD)
                                savedSymbol.symbolImage = "wild_golden";
                        }
                        if (symbol.data[0] == 2)
                        {
                            symbol.symbolImage = "wild";
                        }

                    }
            }

            if (feature is MappedSpin mappedSpin)
            {
                this.savedSpin = mappedSpin;
                if (mappedSpin.id == FeatureIds.BaseGame) mappedSpin.renderGameMode = false;

                if (mappedSpin.id == FeatureIds.Respin)
                {
                    if (!this.respinActive)
                        this.respinActive = true;
                    else
                        mappedSpin.renderGameMode = false;
                }

                if (mappedSpin.id == FeatureIds.FreeSpin)
                {
                    GameMappedFreeSpin gameMappedFreeSpin = new GameMappedFreeSpin(mappedSpin as MappedFreeSpin, currentMultiplier);
                    if (boostActive)
                    {
                        boostActive = false;
                        gameMappedFreeSpin.spinsAdded = 5;
                    }

                    return gameMappedFreeSpin;
                }



            }

            return feature;
        }

    }

    internal class GameMappedFreeSpin : MappedFreeSpin
    {
        public int multiplier;

        public GameMappedFreeSpin(MappedFreeSpin mappedSpin, int currentMultiplier) : base()
        {
            this.id = mappedSpin.id;
            this.type = mappedSpin.type;
            this.reelWindow = mappedSpin.reelWindow;
            this.reelWindowInRows = mappedSpin.reelWindowInRows;
            this.spinIndex = mappedSpin.spinIndex;
            this.spinsAdded = mappedSpin.spinsAdded;
            this.spinsRemaining = mappedSpin.spinsRemaining;

            this.multiplier = currentMultiplier;

        }
    }

    internal class TrailCollection : Feature
    {
        public int value;
        public int update = 0;
        public string[] symbolImages;

        public int extraSpins = 0;
        public int updatedMultiplier = 0;
        public TrailCollection(Collection collection)
        {
            this.type = collection.type;
            this.id = collection.id;
            this.value = collection.value;
            this.update = collection.update;
            this.symbolImages = new string[]{
                    "bigHit", "bigHit", "upgrade",
                    "bigHit", "bigHit", "upgrade2",
                    "bigHit", "bigHit", "upgrade3",
                    "bigHit", "bigHit", "upgrade4"
                };

            for (var i = value; i < symbolImages.Length; i++)
            {
                symbolImages[i] = $"{symbolImages[i]}_off";
            }

            if (update > 0)
            {
                switch (value)
                {
                    case 3:
                        extraSpins = 5;
                        updatedMultiplier = 2;
                        break;
                    case 6:
                        extraSpins = 5;
                        updatedMultiplier = 3;
                        break;
                    case 9:
                        extraSpins = 5;
                        updatedMultiplier = 5;
                        break;
                    case 12:
                        extraSpins = 5;
                        updatedMultiplier = 10;
                        break;
                }
            }
        }
    }
    internal class ScatterCollection : Feature
    {
        public int value;
        public int update = 0;
        public bool boostActive;
        public int meterProgress;
        public ScatterCollection(Collection collection)
        {
            this.type = collection.type;
            this.id = collection.id;
            this.value = collection.value;
            this.update = collection.update;
            this.meterProgress = (int)(((decimal)value / (decimal)GameDefs.FULL_SCATTER_PROGRESS_BAR_COUNT) * 100);
            boostActive = value >= GameDefs.FULL_SCATTER_PROGRESS_BAR_COUNT;
        }
    }

    public class SymbolMapper : ISymbolMapper
    {
        private Dictionary<string, string> imageMap;

        public SymbolMapper()
        {
            imageMap = new Dictionary<string, string>{
                {$"{LOGIC_SYMBOL_SCATTER}", "scatter"},
                {$"{LOGIC_SYMBOL_BIGHIT}", "bigHit"},
                {$"{LOGIC_SYMBOL_WILD}", "wild"},
                {$"{LOGIC_SYMBOL_HIGH1}", "high1"},
                {$"{LOGIC_SYMBOL_HIGH2}", "high2"},
                {$"{LOGIC_SYMBOL_HIGH3}", "high3"},
                {$"{LOGIC_SYMBOL_HIGH4}", "high4"},
                {$"{LOGIC_SYMBOL_LOW1}", "low1"},
                {$"{LOGIC_SYMBOL_LOW2}", "low2"},
                {$"{LOGIC_SYMBOL_LOW3}", "low3"},
                {$"{LOGIC_SYMBOL_LOW4}", "low4"},
                {$"{LOGIC_SYMBOL_LOW5}", "low5"}
            };
        }

        public SymbolData MapSymbol(Feature feature, int[] symbolDataInput)
        {
            string symbolImage = imageMap[$"{symbolDataInput[0]}"];
            string overlayImage = null;
            int overlay = -1;
            if (symbolDataInput.Length > 1)
                overlay = symbolDataInput[1];

            switch(overlay) {
                case 1:
                    overlayImage = "frame.png";
                    break;
                case 0:
                    break;
                default:
                    if(feature.id == FeatureIds.BigHit)
                        overlayImage = $"x{overlay}.png";
                    else
                        overlayImage = $"x{overlay}_off.png";
                    break;
            }

            SymbolData symbolData = new SymbolData { symbolImage = symbolImage, data = symbolDataInput, overlay = overlay, overlayImage = overlayImage };

            return symbolData;
        }


    }
}