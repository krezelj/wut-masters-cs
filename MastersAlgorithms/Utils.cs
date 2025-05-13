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

        public static float[] Softmax(float[] input, int batchCount = 1)
        {
            int length = input.Length;
            int batchLength = length / batchCount;
            float[] output = new float[length];

            for (int b = 0; b < batchCount; b++)
            {
                int start = b * batchLength;
                int end = start + batchLength;

                float max = float.MinValue;
                for (int i = start; i < end; i++)
                {
                    if (input[i] > max)
                        max = input[i];
                }

                float sum = 0.0f;
                for (int i = start; i < end; i++)
                {
                    output[i] = MathF.Exp(input[i] - max);
                    sum += output[i];
                }

                for (int i = start; i < end; i++)
                {
                    output[i] /= sum;
                }
            }

            return output;
        }

        public static float[] MaskedSoftmax(float[] input, bool[] mask, int batchCount = 1)
        {
            int length = input.Length;
            int batchLength = length / batchCount;
            float[] output = new float[length];

            for (int b = 0; b < batchCount; b++)
            {
                int start = b * batchLength;
                int end = start + batchLength;

                float max = float.MinValue;
                for (int i = start; i < end; i++)
                {
                    if (mask[i] && input[i] > max)
                        max = input[i];
                }

                float sum = 0.0f;
                for (int i = start; i < end; i++)
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

                for (int i = start; i < end; i++)
                {
                    output[i] /= sum;
                }
            }

            return output;
        }

        public static int Sample(float[] probs)
        {
            float p = RNG.NextSingle();
            float cumSum = 0.0f;

            for (int i = 0; i < probs.Length; i++)
            {
                cumSum += probs[i];
                if (p <= cumSum)
                {
                    return i;
                }
            }

            return probs.Length - 1;
        }

        internal static int ArgMax(float[] input)
        {
            float max = float.MinValue;
            int maxIndex = -1;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] > max)
                {
                    max = input[i];
                    maxIndex = i;
                }
            }
            return maxIndex;
        }
    }
}