using System;

namespace Service.LogicCommon.Rng
{
    public class MersenneTwisterRng : Rng
    {
        private readonly MersenneTwister _randomInstance ;

        public MersenneTwisterRng() {
            _randomInstance = new MersenneTwister();
        }  

        public MersenneTwisterRng(int initSeed) 
        {
            _randomInstance = new MersenneTwister();
            _randomInstance.initialize(initSeed);
        }

        public MersenneTwisterRng(int[] initSeed) 
        {
            _randomInstance = new MersenneTwister();
            _randomInstance.initialize(initSeed);
        }

        protected override int GetRandomNumber(int i, int maxValue)
        {
            if(i == 0) {
                return _randomInstance.Next(maxValue);
            }
            return _randomInstance.Next(i, maxValue);
        }

        internal void LogMTI()
        {
            Console.WriteLine($"MTI: {_randomInstance.GetMTI()}");
        }
        internal override int GetMTI() {
            return _randomInstance.GetMTI();
        }
    }
}