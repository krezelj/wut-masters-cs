using System.Numerics;
using System.Text;

namespace MastersAlgorithms.Games
{
    public static class BitBoard
    {
        public static readonly ulong rowMask;
        public static readonly ulong colMask;
        public static readonly ulong lastRowMask;
        public static readonly ulong lastColMask;

        public static int N_ROWS;
        public static int N_COLS;

        static BitBoard()
        {
            N_ROWS = 8;
            N_COLS = 8;

            rowMask = (1ul << N_ROWS) - 1;
            lastRowMask = rowMask << (N_ROWS - 1) * N_COLS;

            colMask = 0ul;
            for (int col = 0; col < N_COLS; col++)
            {
                colMask |= 1ul << col * N_ROWS;
            }
            lastColMask = colMask << N_COLS - 1;
        }

        public static int Index(this ulong bitboard)
        {
            return BitOperations.TrailingZeroCount(bitboard);
        }

        public static ulong GetPosition(int row, int col)
        {
            return 1ul << (row * N_COLS + col);
        }

        public static void SetPositions(this ref ulong bitboard, ulong positions)
        {
            bitboard |= positions;
        }

        public static void ClearPositions(this ref ulong bitboard, ulong positions)
        {
            bitboard &= ~positions;
        }

        public static void TogglePositions(this ref ulong bitboard, ulong positions)
        {
            bitboard ^= positions;
        }

        public static ulong GetRow(this ulong bitboard, int row)
        {
            return (bitboard >> (row * N_COLS)) & rowMask;
        }

        public static ulong GetColumn(this ulong bitboard, int col)
        {
            return (bitboard >> col) & colMask;
        }

        public static bool IsEmpty(this ulong bitboard)
        {
            return (bitboard & 0xFFFFFFFFFFFFFFFF) == 0;
        }

        public static ulong PopNextPosition(this ref ulong bitboard)
        {
            int i = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;
            return 1ul << i;
        }

        public static int PopNextIndex(this ref ulong bitboard)
        {
            int i = BitOperations.TrailingZeroCount(bitboard);
            bitboard &= bitboard - 1;
            return i;
        }

        public static void PopNext(this ref ulong bitboard)
        {
            bitboard &= bitboard - 1;
        }

        public static IEnumerable<ulong> EnumerateBits(this ulong bitboard)
        {
            while (bitboard > 0)
                yield return bitboard.PopNextPosition();
        }

        public static bool Contains(this ulong bitboard, ulong position)
        {
            return (bitboard & position) > 0;
        }

        public static void ShiftToNeighbor(this ref ulong bitboard, int offset)
        {
            // converts -9, -8, and -7 to -1
            // converts -1, 0, and 1 to 0
            // converts 7, 8, and 9 to 1
            bitboard = bitboard.ShiftVertical(offset + 1 >> 3);

            // converts -9, -1, and 7 to -1
            // converts -8, 0, and 8 to 0
            // converts -7, 1, and 9 to 1
            bitboard = bitboard.ShiftHorizontal((offset + 1 & 7) - 1);
        }

        public static ulong ShiftHorizontal(this ulong bitboard, int offset)
        {
            if (offset < 0)
            {
                bitboard >>= -offset;
                bitboard &= ~lastColMask;
            }
            else if (offset > 0)
            {
                bitboard <<= offset;
                bitboard &= ~colMask;
            }
            return bitboard;
        }

        public static ulong ShiftVertical(this ulong bitboard, int offset)
        {
            if (offset < 0)
                bitboard >>= -offset * N_COLS;
            else if (offset > 0)
                bitboard <<= offset * N_COLS;
            return bitboard;
        }

        public static ulong Expand4(this ulong bitboard)
        {
            ulong left = (bitboard >> 1) & ~lastColMask;
            ulong right = (bitboard << 1) & ~colMask;
            ulong up = bitboard >> N_COLS;
            ulong down = bitboard << N_COLS;
            return bitboard | left | right | up | down;
        }

        public static ulong Expand8(this ulong bitboard)
        {
            ulong left = (bitboard >> 1) & ~lastColMask;
            ulong right = (bitboard << 1) & ~colMask;
            ulong up = bitboard >> N_COLS;
            ulong down = bitboard << N_COLS;

            ulong upLeft = left >> N_COLS;
            ulong upRight = right >> N_COLS;
            ulong downLeft = left << N_COLS;
            ulong downRight = right << N_COLS;


            return bitboard | left | right | up | down | upLeft | upRight | downLeft | downRight;
        }

        public static ulong ReverseBits(this ulong x)
        {
            x = ((x >> 1) & 0x5555555555555555UL) | ((x & 0x5555555555555555UL) << 1);
            x = ((x >> 2) & 0x3333333333333333UL) | ((x & 0x3333333333333333UL) << 2);
            x = ((x >> 4) & 0x0F0F0F0F0F0F0F0FUL) | ((x & 0x0F0F0F0F0F0F0F0FUL) << 4);
            x = ((x >> 8) & 0x00FF00FF00FF00FFUL) | ((x & 0x00FF00FF00FF00FFUL) << 8);
            x = ((x >> 16) & 0x0000FFFF0000FFFFUL) | ((x & 0x0000FFFF0000FFFFUL) << 16);
            x = (x >> 32) | (x << 32);
            return x;
        }

        public static string String(this ulong bitboard)
        {
            string binaryRepr = Convert.ToString((long)bitboard, 2).PadLeft(64, '0');
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < N_ROWS; i++)
            {
                sb.AppendLine(binaryRepr[(i * N_COLS)..((i + 1) * N_COLS)]);
            }
            return string.Concat(sb.ToString().Reverse());
        }
    }
}
