using System;
using System.Collections.Generic;
using System.Linq;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


public static class RndHelper
{
    public static Random Instance = new Random();

    /// <summary>
    /// Return random Int value from 0
    /// </summary>
    public static int Int(this Random rnd)
    {
        return rnd.Next();
    }

    /// <summary>
    /// Return random Int value from 0 to given exclusive upper bound
    /// </summary>
    public static int Int(this Random rnd, int exclusiveUpperBound)
    {
        return rnd.Next(exclusiveUpperBound);
    }

    public static List<int> Ints(this Random rnd, int exclusiveUpperBound, int count)
    {
        if (count > exclusiveUpperBound) count = exclusiveUpperBound;

        var res = new HashSet<int>();

        while (res.Count < count)
            res.Add(rnd.Next(exclusiveUpperBound));

        return res.ToList();
    }

    /// <summary>
    /// Return random Float value from 0 to given upper bound
    /// </summary>
    public static float Float(this Random rnd, float exclusiveUpperBound)
    {
        return (float)rnd.NextDouble() * exclusiveUpperBound;
    }

    /// <summary>
    /// Return random Float value from given diapason
    /// </summary>
    public static float Float(this Random rnd, float from, float to)
    {
        return (float)(rnd.NextDouble() * (to - from) + from);
    }

    /// <summary>
    /// Return True with defined probability
    /// </summary>
    public static bool Bool(this Random rnd, float probability = 0.5f)
    {
        return rnd.NextDouble() < probability;
    }

    /// <summary>
    /// Чем ближе точка value к центру между triangleFrom и triangleTo - тем больше вероятность возврата true.
    /// В центре - вероятность 1, на краях и за пределами triangleFrom и triangleTo - вероятность возврата true = 0.
    /// </summary>
    public static bool InTriangle(this Random rnd, float value, float triangleFrom, float triangleTo)
    {
        var d = (triangleTo - triangleFrom) / 2;
        var center = triangleFrom + d;
        var dist = Math.Abs(value - center);
        return rnd.Float(d) > dist;
    }

    /// <summary>
    /// Random float value (Triangle distribution)
    /// </summary>
    public static float Triangle(this Random rnd, float from, float to)
    {
        var v = (float)(rnd.NextDouble() + rnd.NextDouble()) / 2;
        return from + v * (to - from);
    }

    /// <summary>
    /// Random float value (Gauss distribution)
    /// </summary>
    public static float Gauss(this Random rnd, float center, float sigma)
    {
        return (float)(center + sigma * (rnd.NextDouble() + rnd.NextDouble() + rnd.NextDouble() + rnd.NextDouble() - 2) / 2);
    }

    /// <summary>
    /// Random float value (Gauss distribution by BoxMuller formula)
    /// </summary>
    public static float GaussBoxMuller(this Random rnd, float center = 0, float sigma = 1)
    {
        //якщо невикористаного значення немає - генерується наступна пара
        double x = 0, y = 0, R = 2;
        while (R > 1 || R <= double.Epsilon)
        {
            //генеруються випадкові x та y, поки не буде виконана умова R <= 1
            x = rnd.NextDouble() * 2 - 1;
            y = rnd.NextDouble() * 2 - 1;
            R = x * x + y * y;
        }
        double t = Math.Sqrt(-2 * Math.Log(R) / R);
        var res = (float)(x * t);
        res = res * sigma + center;
        return res;
    }

    /// <summary>
    /// Random float value (Log distribution)
    /// </summary>
    public static float Log(this Random rnd, float avg)
    {
        return -(float)(avg * Math.Log(rnd.NextDouble()));
    }

    /// <summary>
    /// Shuffle enumeration
    /// </summary>
    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> list, Random rnd)
    {
        return list.OrderBy(_ => rnd.Next());
    }

    public static T GetRnd<T>(this IEnumerable<T> list, Random rnd)
    {
        return list.ToList().GetRnd(rnd);
    }

    public static T GetRnd<T>(this IList<T> list, Random rnd)
    {
        if (list == null || list.Count == 0)
            return default(T);

        return list[rnd.Int(list.Count)];
    }

    public static List<T> GetRnds<T>(this IList<T> list, int count, Random rnd)
    {
        var res = new List<T>();

        if (list == null || list.Count == 0 || count < 1)
            return res;

        foreach (var index in rnd.Ints(list.Count, count))
            res.Add(list[index]);

        return res;
    }

    /// <summary>
    /// Возвращает индекс согласно вероятностям
    /// </summary>
    public static int GetRndIndex(this IList<float> probabilities, Random rnd,  float sumOfProb = 1f)
    {
        if (probabilities == null || probabilities.Count == 0)
            return -1;

        if (sumOfProb <= float.Epsilon)
            return rnd.Int(probabilities.Count);

        var v = rnd.Float(sumOfProb);

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
    public static T GetRnd<T>(this IList<T> list, IList<float> probabilities, Random rnd, float sumOfProb = 1f)
    {
        return list[probabilities.GetRndIndex(rnd, sumOfProb)];
    }

    #region Uniform low dispersion distributions

    /// <summary>
    /// Uniform distribution with low dispersion (0..1)
    /// </summary>
    /// <param name="index">Index of sequence</param>
    /// <returns>Pseudo-random vector</returns>
    /// <remarks>
    /// http://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/
    /// https://observablehq.com/@jrus/plastic-sequence
    /// </remarks>
    public static Vector3 PlasticSequence3(int index)
    {
        const double p1 = 0.8191725133961644; // inverse of plastic number
        const double p2 = 0.6710436067037892;
        const double p3 = 0.5497004779019702;

        return new Vector3((float)((p1 * index) % 1), (float)((p2 * index) % 1), (float)((p3 * index) % 1));
    }

    /// <summary>
    /// Uniform distribution with low dispersion (0..1)
    /// </summary>
    /// <param name="index">Index of sequence</param>
    /// <returns>Pseudo-random vector</returns>
    /// <remarks>
    /// http://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/
    /// https://observablehq.com/@jrus/plastic-sequence
    /// </remarks>
    public static Vector2 PlasticSequence2(int index)
    {
        const double p1 = 0.7548776662466927; // inverse of plastic number
        const double p2 = 0.5698402909980532;

        return new Vector2((float)((p1 * index) % 1), (float)((p2 * index) % 1));
    }

    /// <summary>
    /// Uniform distribution with low dispersion (0..1)
    /// </summary>
    /// <param name="index">Index of sequence</param>
    /// <returns>Pseudo-random value</returns>
    /// <remarks>
    /// http://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/
    /// https://observablehq.com/@jrus/plastic-sequence
    /// </remarks>
    public static float PlasticSequence1(int index)
    {
        const double p = 0.618033988749894848; // inverse of golden ratio

        return (float)((p * index) % 1);
    }

    /// <summary>
    /// Halton uniform distribution with low dispersion (0..1) 
    /// </summary>
    /// <param name="index">Index of sequence</param>
    /// <returns>Pseudo-random Vector3</returns>
    /// <remarks>
    /// https://en.wikipedia.org/wiki/Halton_sequence
    /// </remarks>
    public static Vector3 HaltonSequence3(int index)
    {
        return new Vector3(HaltonSequence(index, 2), HaltonSequence(index, 3), HaltonSequence(index, 5));
    }

    /// <summary>
    /// Halton uniform distribution with low dispersion (0..1) 
    /// </summary>
    /// <param name="index">Index of sequence</param>
    /// <returns>Pseudo-random Vector2</returns>
    /// <remarks>
    /// https://en.wikipedia.org/wiki/Halton_sequence
    /// </remarks>
    public static Vector2 HaltonSequence2(int index)
    {
        return new Vector2(HaltonSequence(index, 2), HaltonSequence(index, 3));
    }

    /// <summary>
    /// Halton uniform distribution with low dispersion (0..1) 
    /// </summary>
    /// <param name="index">Index of sequence</param>
    /// <param name="basePrime">Any prime number (2, 3, 5, 7, 11, etc)</param>
    /// <returns>Pseudo-random value</returns>
    /// <remarks>
    /// https://en.wikipedia.org/wiki/Halton_sequence
    /// </remarks>
    public static float HaltonSequence(int index, int basePrime = 2)
    {
        var fraction = 1.0;
        var result = 0.0;
        while (index > 0)
        {
            fraction /= basePrime;
            result += fraction * (index % basePrime);
            index = UnityEngine.Mathf.FloorToInt(index / basePrime); // floor division
        }
        return (float)result;
    }

    //public static Vector2 GridPlusGauss(this Random rnd, int density = 5)
    //{
    //    var x = rnd.Next(0, density);
    //    var y = rnd.Next(0, density);
    //    var xx = rnd.GaussBoxMuller(x, 1f / density);
    //    var yy = rnd.GaussBoxMuller(y, 1f / density);
    //    return new Vector2(xx, yy);
    //}

    #endregion
}
