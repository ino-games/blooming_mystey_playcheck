using System;
using System.Linq;
using Service.LogicCommon.Gati;

namespace Service.LogicCommon.Rng
{
    public abstract class AbstractGatiRng
    {

        public int ChooseMe(GatiGameState state, int[] choices)
        {
            var sumOfWeight = choices.Sum();
            var returnValue = -1;

            if (sumOfWeight == 0) return returnValue;

            var rnd = Next(state, sumOfWeight);
            for (var i = 0; i < choices.Length; i++)
            {
                if (rnd < choices[i])
                {
                    returnValue = i;
                    break;
                }

                rnd -= choices[i];
            }

            return returnValue;
        }

        public int ChooseMe(GatiGameState state, int[,] choices, int arrayIndex)
        {
            var sumOfWeight = 0;

            var len = choices.GetLength(1);

            for (var i = 0; i < len; i++) sumOfWeight += choices[arrayIndex, i];

            if (sumOfWeight == 0)
                return -1;

            var rnd = Next(state, sumOfWeight);

            for (var i = 0; i < choices.Length; i++)
            {
                if (rnd < choices[arrayIndex, i])
                    return i;
                rnd -= choices[arrayIndex, i];
            }

            return -1;
        }


        public int Next(GatiGameState state, int maxValue)
        {
            GatiRandomNumber rnd;
            if (state.force != null && state.force.cheat.Count > 0)
            {
                var next = state.force.cheat[0];
                state.force.cheat.RemoveAt(0);

                if (state.force.cheat.Count == 0) state.force.cheat = null;

                rnd = new GatiRandomNumber((uint)state.randomNumbers.Count(), 0, next);
            } else {
                rnd = GetRandomNumber(0, maxValue);
            }

            state.randomNumbers.Add(rnd);

            return rnd.Value;
        }

        public int Next(GatiGameState state, int minValue, int maxValue)
        {
            if (maxValue <= minValue)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (maxValue == minValue)
            {
                return minValue;
            }

            return Next(state, maxValue - minValue + 1) + minValue;
        }

        public bool ThousandChance(GatiGameState state, int probability)
        {
            return Next(state, 1000) < probability;
        }

        public bool OneIn(GatiGameState state, int probability)
        {
            return Next(state, probability) == 0;
        }

        protected abstract GatiRandomNumber GetRandomNumber(int low, int high);

        

        internal virtual int GetMTI()
        {
            throw new NotImplementedException();
        }

    }
}