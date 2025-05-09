using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace MastersAlgorithms
{
    public static class Utils
    {
        public static Random RNG;

        static Utils()
        {
            RNG = new Random(0);
        }

        public static void SetRNGSeed(int seed)
        {
            RNG = new Random(seed);
        }

        public static bool InLimits(int i, int j, int width, int height)
        {
            return i >= 0 && i < width && j >= 0 && j < height;
        }

        public static (int, int)[] GetNeighborDiffs(
            int i, int j, int width, int height, bool ignoreLimits = false)
        {
            (int, int)[] diffs = new (int, int)[8];
            int count = 0;

            for (int di = -1; di <= 1; di++)
            {
                for (int dj = -1; dj <= 1; dj++)
                {
                    if (di == 0 && dj == 0)
                        continue;
                    int newI = i + di;
                    int newJ = j + dj;
                    if (ignoreLimits || InLimits(newI, newJ, width, height))
                        diffs[count++] = (di, dj);
                }
            }
            Array.Resize(ref diffs, count);
            return diffs;
        }

        // public static IEnumerable<(int i, int j)> GetNeighbors(
        //     int i, int j, int width, int height
        // )
        // {
        //     var diffs = GetNeighborDiffs(i, j, width, height);
        //     foreach (var diff in diffs)
        //     {
        //         yield return (i + diff.di, j + diff.dj);
        //     }
        // }

        public static float[] Softmax(float[] input)
        {
            int length = input.Length;
            float[] output = new float[length];
            float max = input.Max();

            float sum = 0.0f;
            for (int i = 0; i < length; i++)
            {
                output[i] = MathF.Exp(input[i] - max);
                sum += output[i];
            }

            for (int i = 0; i < length; i++)
            {
                output[i] /= sum;
            }
            return output;
        }

        public static float[] MaskedSoftmax(float[] input, bool[] mask)
        {
            int length = input.Length;
            float[] output = new float[length];
            float max = input.Max();

            float sum = 0.0f;
            for (int i = 0; i < length; i++)
            {
                if (mask[i])
                {
                    output[i] = MathF.Exp(input[i] - max);
                    sum += output[i];
                }
                else
                {
                    output[i] = 0;
                }
            }

            for (int i = 0; i < length; i++)
            {
                output[i] /= sum;
            }
            return output;
        }
    }
}