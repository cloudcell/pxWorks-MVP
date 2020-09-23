using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Templates.FastRndNS
{
    /// <summary>
    /// Работает в два раза быстрее стандратного Random
    /// Потокозащищенный (в отличие от Random)
    /// </summary>
    public static class FastRnd
    {
        //const int COUNT = 1046527;
        const int COUNT = 0xFFFFF;
        static float[] rnds = new float[COUNT + 1];
        static float[] rndsSqrt = new float[COUNT + 1];
        volatile static int counter = 0;

        public static void Restart(int startIndex)
        {
            counter = startIndex & COUNT;
        }

        static FastRnd()
        {
            Initialize();
        }

        public static float Float()
        {
            counter = (counter + 1) & COUNT;
            return rnds[counter];
        }

        public static float FloatSqrt()
        {
            counter = (counter + 1) & COUNT;
            return rndsSqrt[counter];
        }

        public static float FloatNegPos()
        {
            return Float() * 2 - 1;
        }

        public static void Initialize(int seed = 1)
        {
            var rnd = new Random(seed);
            for (int i = 0; i <= COUNT; i++)
                rnds[i] = (float)rnd.NextDouble();
          
            for (int i = 0; i <= COUNT; i++)
                rndsSqrt[i] = (float)Math.Sqrt(rnd.NextDouble());
        }

        /// <summary>
        /// Return random Int value from 0
        /// </summary>
        public static int Int()
        {
            return (int)(Float() * int.MaxValue);
        }

        /// <summary>
        /// Return random Int value from 0 to given exclusive upper bound
        /// </summary>
        public static int Int(int exclusiveUpperBound)
        {
            return (int)(Float() * exclusiveUpperBound);
        }

        public static List<int> Ints(int exclusiveUpperBound, int count)
        {
            if (count > exclusiveUpperBound) count = exclusiveUpperBound;

            var res = new HashSet<int>();

            while (res.Count < count)
                res.Add(Int(exclusiveUpperBound));

            return res.ToList();
        }

        /// <summary>
        /// Return random Float value from 0 to given upper bound
        /// </summary>
        public static float Float(float exclusiveUpperBound)
        {
            return (float)Float() * exclusiveUpperBound;
        }

        /// <summary>
        /// Return random Float value from given diapason
        /// </summary>
        public static float Float(float from, float to)
        {
            return (float)(Float() * (to - from) + from);
        }

        /// <summary>
        /// Return True with defined probability
        /// </summary>
        public static bool Bool(float probability = 0.5f)
        {
            return Float() < probability;
        }

        /// <summary>
        /// Random float falue (Gauss distribution)
        /// </summary>
        public static float Gauss(float center, float sigma)
        {
            return (float)(center + sigma * (Float() + Float() + Float() + Float() - 2) / 2);
        }

        /// <summary>
        /// Random float falue (Log distribution)
        /// </summary>
        public static float Log(float avg)
        {
            return -(float)(avg * Math.Log(Float()));
        }

        /// <summary>
        /// Shuffle enumeration
        /// </summary>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list)
        {
            return list.OrderBy(_ => Float());
        }

        public static string GetRnd(this string str)
        {
            return str.Split('/', '|').GetRnd();
        }

        public static T GetRnd<T>(this IEnumerable<T> list)
        {
            return list.ToList().GetRnd();
        }

        public static T GetRnd<T>(this IList<T> list)
        {
            if (list == null || list.Count == 0)
                return default(T);

            return list[Int(list.Count)];
        }

        public static T Or<T>(this T item, T anotherItem, float probability = 0.5f)
        {
            return Bool(probability) ? anotherItem : item;
        }

        public static List<T> GetRnds<T>(this IList<T> list, int count)
        {
            var res = new List<T>();

            if (list == null || list.Count == 0 || count < 1)
                return res;

            foreach (var index in Ints(list.Count, count))
                res.Add(list[index]);

            return res;
        }

        /// <summary>
        /// Возвращает индекс согласно вероятностям
        /// </summary>
        public static int GetRndIndex(this IList<float> probabilities, float sumOfProb = 1f)
        {
            if (probabilities == null || probabilities.Count == 0)
                return -1;

            if (sumOfProb <= float.Epsilon)
                return Int(probabilities.Count);

            var v = Float(sumOfProb);

            var sum = 0f;
            for (int i = 0; i < probabilities.Count; i++)
            {
                sum += probabilities[i];
                if (sum >= v) return i;
            }

            return probabilities.Count - 1;
        }

        /// <summary>
        /// Возвращает значение согласно вероятностям
        /// </summary>
        public static T GetRnd<T>(this IList<T> list, IList<float> probabilities, float sumOfProb = 1f)
        {
            return list[probabilities.GetRndIndex(sumOfProb)];
        }
    }
}
