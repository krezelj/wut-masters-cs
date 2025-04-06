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

        public static List<(int di, int dj)> GetNeighborDiffs(
            int i, int j, int width, int height, Func<int, int, bool>? isValid = null)
        {
            List<(int, int)> diffs = new List<(int, int)>();
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

                    if (isValid != null && isValid(newI, newJ))
                    {
                        diffs.Add((di, dj));
                    }
                }
            }
            return diffs;
        }

        public static List<(int i, int j)> GetNeighbors(
            int i, int j, int width, int height, Func<int, int, bool>? isValid = null
        )
        {
            var diffs = GetNeighborDiffs(i, j, width, height, isValid);
            List<(int di, int dj)> neighbors = new List<(int, int)>();
            foreach (var diff in diffs)
            {
                neighbors.Add((i + diff.di, j + diff.dj));
            }
            return neighbors;
        }
    }
}