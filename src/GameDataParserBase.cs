using Newtonsoft.Json;
using System.Linq;
using Newtonsoft.Json.Linq;
using Service.LogicCommon;
using Service.PlayCheckCommon;
using System.Reflection;
using System;
using Service.LogicCommon.Win;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Service.Logic;
using Service.LogicCommon.Api.Spin;

namespace Service.PlayCheckCommon
{
    public class GameDataParserBase
    {
        public GameDataParserBase(ISymbolMapper symbolMapper)
        {
            this.symbolMapper = symbolMapper;
        }
        public virtual ParsedGameData ParseGameData(string gameData, string imagePath, string version = null)
        {

            IEnumerable<Feature> features = null;
            string error = null;
            try
            {
                parsedData = ParseState<PersistedState>(gameData);
                if (parsedData.currentStage.Any(feature => feature.type == FeatureTypes.GambleForBonus))
                {
                    features = parsedData.currentStage;
                }
                else
                {
                    features = parsedData.processedStages.Concat(parsedData.currentStage);
                }

                var assembly = Assembly.GetExecutingAssembly();
                if (version == null) version = "1.0.0";

                //features = features.Select(feature => MapFeatures(feature)).ToList();
                var parsedFeatures = new List<Feature>();
                foreach (var feature in features)
                {
                    parsedFeatures.Add(MapFeatures(feature, parsedFeatures));
                }
                features = parsedFeatures;
            }
            catch (Exception e)
            {
                //log error
                error = e.Message + " / <br/> " + e.StackTrace;
            }


            var Data = new ParsedGameData
            {
                imagePath = imagePath,
                text = "Output", // Remove?
                features = features ?? new List<Feature>(),
                version = version,
                reelCols = GameDefs.NUMBER_REELS,
                reelRows = GameDefs.REEL_WINDOW,
                bet = parsedData.bet / 100m
            };

            //track parsing error
            if (!string.IsNullOrEmpty(error)) Data.AddLogInfo(error);

            return Data;
        }

        protected virtual Feature MapFeatures(Feature feature, List<Feature> previousFeatures)
        {
            if (feature is PayLinePayout paylinePayout)
            {
                DecimalPayLinePayout decimalPayLinePayout = new DecimalPayLinePayout(paylinePayout);
                //find previous spin data
                var lastSpin = previousFeatures.FindLast(f => f is MappedSpin || f is MappedFreeSpin) as MappedSpin;
                decimalPayLinePayout.winDetails.spin = lastSpin;
                decimalPayLinePayout.winDetails.winList = decimalPayLinePayout.winDetails.winList.Select(winItem =>
                {
                    MapWinItemSymbolImage(feature, winItem);

                    //load win symbol data
                    if (lastSpin != null) {
                        winItem.symbolsData = new List<List<SymbolData>>();
                        for(var i = 0; i < winItem.symbolsInWin.Count; i++) {
                            winItem.symbolsData.Add(new List<SymbolData>());
                            for(var j = 0; j < winItem.symbolsInWin[i].Count; j++) {
                                if (winItem.symbolsInWin[i][j] > 0) {
                                    //win symbol
                                    winItem.symbolsData[i].Add(lastSpin.reelWindow[i][j]);
                                } else {
                                    //no win position
                                    winItem.symbolsData[i].Add(null);
                                }
                            }
                        }
                    }
                    
                    return winItem;
                });
                return decimalPayLinePayout;
            }

            if (feature is WaysPayout waysPayout)
            {
                DecimalWaysPayout decimalWaysPayout = new DecimalWaysPayout(waysPayout);
                decimalWaysPayout.winDetails.winList = decimalWaysPayout.winDetails.winList.Select(winItem =>
               {
                   MapWinItemSymbolImage(feature, winItem);
                   return winItem;
               });

                return decimalWaysPayout;
            }

            if (feature is Payout payout)
            {
                return new DecimalPayout(payout);
            }
            
            if (feature is PickResult pickResult)
            {
                return new MappedPickResult(pickResult);
            }

            if(feature is InitFreeSpins) {
                currentFreeSpinIndex = 1;
            }

            if (feature is FreeSpin freeSpin)
            {
                MappedFreeSpin mappedSpin = new MappedFreeSpin(freeSpin, symbolMapper);
                mappedSpin.spinIndex = currentFreeSpinIndex++;
                return mappedSpin;
            }
            if (feature is Spin spin)
            {
                MappedSpin mappedSpin = new MappedSpin(spin, symbolMapper);
                return mappedSpin;
            }

            if(feature is GambleForBonus gambleForBonus) {
                return new MappedGambleForBonus(gambleForBonus);
            }

            if(feature is BuyBonus buyBonus) {
                return new MappedBuyBonus(buyBonus);
            }

            return feature;
        }

        private void MapWinItemSymbolImage(Feature feature, DecimalWinItem winItem)
        {
            var symbol = symbolMapper.MapSymbol(feature, new int[] { winItem.symbolId });
            winItem.symbolImage = symbol.symbolImage;
        }

        private JsonSerializerSettings jsonSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, SerializationBinder = new CustomSerializationBinder() };
        protected ISymbolMapper symbolMapper;
        private int currentFreeSpinIndex = 1;
        protected PersistedState parsedData;

        public T ParseState<T>(string originalValue)
        {
            var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            
            if (originalValue.TrimStart().StartsWith("<"))
            {
                originalValue = this.ParseXml(originalValue);
            }

            var jsonObject = JObject.Parse(originalValue);
            var tokens = jsonObject.SelectTokens("..$type").Select(token => (JValue)token);
            foreach (var token in tokens)
            {
                string value = ((string)token.Value);
                var classToken = value.Substring(0, value.IndexOf(","));
                token.Value = $"{classToken}, {assemblyName}";
            }
            string persistedStateJsonString = jsonObject.ToString();

            return JsonConvert.DeserializeObject<T>(persistedStateJsonString, jsonSettings);
        }

        private string ParseXml(string gamedataXml)
        {
            var veyronData = XElement.Parse(gamedataXml);

            var events = veyronData.Descendants("Event");

            return events.Last().Descendants("Out").First().Descendants("PersistedState").First().Value;
        }
    }

    public class ParsedGameData
    {
        public string logInfo = ""; //set info for showing on playcheck page to debug data
        public bool isHasLogInfo { get { return !string.IsNullOrEmpty(logInfo); } }

        public string imagePath;

        public string text;
        public string version;
        public IEnumerable<Feature> features;
        public int reelCols;
        public int reelRows;
        public decimal bet;

        public void AddLogInfo(string log)
        {
            logInfo += "<br/><br/>\n\n" + log;
        }
    }
}