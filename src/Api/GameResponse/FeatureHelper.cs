using System.Collections.Generic;
using System.Linq;
using Service.Logic;

namespace Service.LogicCommon {
    public static class FeatureHelper {
        public static Spin GetBaseGame(IEnumerable<Feature> features)
        {
            return GetSingleFeature<Spin>(features, FeatureIds.BaseGame);
        }

        public static IEnumerable<FreeSpin> GetFreeSpins(IEnumerable<Feature> features) {
            return features.Where(feature => feature is FreeSpin && feature.id == FeatureIds.FreeSpin).Select(feature => feature as FreeSpin);
        }

        public static IEnumerable<Payout> GetPayouts(IEnumerable<Feature> features)
        {
            return features.Where(feature => feature is Payout).Select(feature => feature as Payout);
        }

        public static bool WillDoFreeSpins(IEnumerable<Feature> features) {
            return features.Any(feature => feature is InitFreeSpins && feature.id == FeatureIds.FreeSpins);
        }

        public static bool WillDoFeature<T>(IEnumerable<Feature> features, string featureId = null) where T: Feature {
            return features.Any(feature =>  feature is T && (featureId == null ||  feature.id == featureId));
        }

        public static IEnumerable<T> GetFeature<T>(IEnumerable<Feature> features, string featureId = null) where T: Feature {
            return features.Where(feature => feature is T && (featureId == null ||  feature.id == featureId)).Select(feature => feature as T);
        }

        public static T GetSingleFeature<T>(IEnumerable<Feature> features, string featureId = null) where T: Feature {
            IEnumerable<T> enumerable = features.Where(feature => feature is T && (featureId == null || feature.id == featureId)).Select(feature => feature as T);
            if(enumerable.Count() != 1) {
                return null;
            }
            return enumerable.Single();
        }

        public static IEnumerable<Feature> SkipToFreeSpins(IEnumerable<Feature> features) {
            return features.SkipWhile(feature => !(feature is InitFreeSpins && feature.id == FeatureIds.FreeSpins));
        }

        public static IEnumerable<Feature> SkipFreeSpins(IEnumerable<Feature> features) {
            return features.TakeWhile(feature => !(feature is InitFreeSpins && feature.id == FeatureIds.FreeSpins));
        }

        public static IEnumerable<IEnumerable<Feature>> GetStages(IEnumerable<Feature> features) {
            var stages = new List<IEnumerable<Feature>>();
            while(features.Count() > 0) {
                var stage = features.TakeWhile(feature => !(feature is RequestNextStage));
                stages.Add(stage);
                features = features.Skip(stage.Count() + 1);
            }

            return stages;
        }

        public static bool AtLastStage(IEnumerable<Feature> features) {
            if(features.Count() == 0)
                return true;
            Feature feature = features.Last();
            return !(feature is RequestNextStage);
        }
    }
}