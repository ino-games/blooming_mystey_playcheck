using Newtonsoft.Json;

namespace Service.LogicCommon {
    public class ObjectUtils {
        public static T Clone<T>(T source) {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source));
        }
    }
}