
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Service.LogicCommon.Rng
{
    [StructLayout(LayoutKind.Sequential)]
    public struct GatiRandomNumber
    {
        // bits represents actual random number drawn by RNG (32 bit int)
        [JsonProperty("bits")]
        public uint Bits { get; set; }

        // requested range
        [JsonProperty("range")]
        public uint Range { get; set; }

        //random number value, contract is bits mod range = value
        [JsonProperty("value")]
        public int Value { get; set; }

        public GatiRandomNumber(uint bits, uint range, int value)
        {
            this.Bits = bits;
            this.Range = range;
            this.Value = value;
        }
    }
}

// TODO: add bookmark, commit and revert to random numbers