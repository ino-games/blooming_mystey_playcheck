using System;
using System.Linq;
using System.Runtime.Serialization;
using Service.Logic;

namespace Service.LogicCommon.Rng
{
    public abstract class Rng
    {

        public int ChooseMe(Force force, int[] choices)
        {
            var sumOfWeight = choices.Sum();
            var returnValue = -1;

            if (sumOfWeight == 0) return returnValue;

            var rnd = Next(force, sumOfWeight);
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

        public int ChooseMe(Force force, int[,] choices, int arrayIndex)
        {
            var sumOfWeight = 0;

            var len = choices.GetLength(1);

            for (var i = 0; i < len; i++) sumOfWeight += choices[arrayIndex, i];

            if (sumOfWeight == 0)
                return -1;

            var rnd = Next(force, sumOfWeight);

            for (var i = 0; i < choices.Length; i++)
            {
                if (rnd < choices[arrayIndex, i])
                    return i;
                rnd -= choices[arrayIndex, i];
            }

            return -1;
        }

        public Int32 ChooseMe(Force force, Int32[,,] choices, int arrayIndex1, int arrayIndex2)
        {
            Int32 sumOfWeight = 0;

            int len = choices.GetLength(2);

            for (Int32 i = 0; i < len; i++)
            {
                sumOfWeight += choices[arrayIndex1, arrayIndex2, i];
            }

            if (sumOfWeight == 0)
                return -1;

            Int32 rnd = Next(force, sumOfWeight);

            for (Int32 i = 0; i < choices.Length; i++)
            {
                if (rnd < choices[arrayIndex1, arrayIndex2, i])
                    return i;
                rnd -= choices[arrayIndex1, arrayIndex2, i];
            }

            return -1;
        }

        public Int32 ChooseMeMaxLength(Force force, Int32[] choices, Int32 maxLength)
        {
            Int32 sumOfWeight = 0;

            for (Int32 i = 0; i < maxLength; i++)
            {
                sumOfWeight += choices[i];
            }

            if (sumOfWeight == 0)
                return -1;

            Int32 rnd = Next(force, sumOfWeight);

            for (Int32 i = 0; i < maxLength; i++)
            {
                if (rnd < choices[i])
                    return i;
                rnd -= choices[i];
            }

            return -1;
        }

        public bool OneIn(Force force, int probability)
        {
            if (probability == 1)
                return true;

            if (probability < 1)
                return false;
            return Next(force, probability) == 0;
        }
        public bool XInY(Force force, Int32 chances, Int32 totalChance)
        {
            if (Next(force, totalChance) < chances)
            {
                return true;
            }

            return false;
        }

        public bool ThousandChance(Force force, int probability)
        {
            return Next(force, 1000) < probability;
        }

        public bool ChanceOutOfHundredThousand(Force force, int probability)
        {
            return Next(force, 100_000) < probability;
        }

        public int Next(Force force)
        {
            return Next(force, int.MaxValue);
        }

        /// <summary>
        /// Method <c>Next</c> draw a random number from 0 to <param>maxValue</param> -1.
        /// </summary>
        public int Next(Force force, int maxValue)
        {
            if (force != null && force.cheat != null && force.cheat.Count > 0)
            {
                var next = force.cheat[0];
                force.cheat.RemoveAt(0);
#if TRACE
                Console.WriteLine("Using _cheat: {0}. Cheats remaining: {1}", next, force.cheat.Count);
#endif
                if (force.cheat.Count == 0) force.cheat = null;

#if DEBUG
                force.usedNumbers.Add(next);
#endif

                if(next >= maxValue) 
                    throw new IllegalForcedResultException($"result({next}) >= maxValue({maxValue})");
                return next;
            }

            var rnd = GetRandomNumber(0, maxValue);
#if DEBUG
            try
            {
                if (force != null)
                    force.usedNumbers.Add(rnd);
            }
            catch (Exception)
            {
                Console.WriteLine($"Used randomNumbers size: {force.usedNumbers.Count}");
                throw;
            }
#endif
            return rnd;
        }

        public int Next(Force force, int minValue, int maxValue)
        {
            if (maxValue < minValue)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (maxValue == minValue)
            {
                return minValue;
            }

            return Next(force, maxValue - minValue + 1) + minValue;
        }

        public uint RandRangeU32(Force force, uint min, uint max)
        {
            return (uint)Next(force, (Int32)min, (Int32)max);
        }

        abstract internal int GetMTI();

        protected abstract int GetRandomNumber(int i, int maxValue);

        internal int ChooseMeAllowNegativeBias(Force force, int[] choices)
        {

            Int32 sumOfWeight = 0;

            for (Int32 i = 0; i < choices.Length; i++)
            {
                if (choices[i] > 0)
                    sumOfWeight += choices[i];
            }

            if (sumOfWeight == 0)
                return -1;

            Int32 rnd = Next(force, sumOfWeight);

            for (Int32 i = 0; i < choices.Length; i++)
            {
                if (choices[i] > 0)
                {
                    if (rnd < choices[i])
                        return i;
                    rnd -= choices[i];
                }
            }

            return -1;
        }

        [Serializable]
        public class IllegalForcedResultException : Exception
        {
            public IllegalForcedResultException(string message):base(message)
            {
            }
        }
    }
}