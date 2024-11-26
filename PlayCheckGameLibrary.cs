using Casino.Library.WebApps.Playcheck.Game.Models;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace BloomingMystery
{
    public class PlayCheckGameLibrary : GameLibrary
    {
        public static string VERSION = "1.0.0";

        public PlayCheckGameLibrary()
        {

        }


        /// <summary>
        /// This method returns the templateProjectId value configured in config.json in gameSettings node.
        /// This code must remain untouched in order for templating to work on environments or live.
        ///
        /// During the package building on the PlayCheck side, the value in the config file will be substituted with the correct one.
        /// You don't need to edit it. 
        /// </summary>
        /// <param name="extraSettings"></param>
        /// <returns></returns>
        private static string GetTemplateProjectId(Dictionary<string, object> extraSettings)
        {
            ((Dictionary<string, object>)extraSettings["gameSettings"]).TryGetValue("templateProjectId", out object templateProjectId);
            if (string.IsNullOrEmpty(templateProjectId as string))
            {
                throw new Exception("templateProjectId value is missing or empty in config.json file");
            }

            return (string)templateProjectId;
        }

        public override GameDetails GenerateGameDetails(string gameData, string imagePath, IPluginLogger logger, Dictionary<string, object> extraSettings)
        {
            var symbolMapper = new SymbolMapper();
            var parser = new GameDataParser(symbolMapper);

            var Data = parser.ParseGameData(gameData, imagePath, VERSION);

            //log data show on playcheck for testing
            //Data.AddLogInfo("---------  RAW DATA ------");
            //Data.AddLogInfo(gameData);
            //Data.AddLogInfo("----- PARSE JSON DATA ----");
            //Data.AddLogInfo(JsonConvert.SerializeObject(Data));
            //Data.AddLogInfo("--------------------------");

            return new GameDetails
            {
                TemplateProjectId = GetTemplateProjectId(extraSettings),
                Template = @"index",
                Data = Data
            };
        }
    }

    


}