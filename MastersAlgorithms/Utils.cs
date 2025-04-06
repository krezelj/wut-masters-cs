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

        public static bool InLimits(int i, int j, int width, int height)
        {
            return i >= 0 && i < width && j >= 0 && j < height;
        }

        public static (int, int)[] GetNeighborDiffs(
            int i, int j, int width, int height)
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
                    if (!InLimits(newI, newJ, width, height))
                        continue;

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
    }
}