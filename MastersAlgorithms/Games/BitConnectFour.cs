using System.Numerics;
using System.Text;

namespace MastersAlgorithms.Games
{
    public class BitConnectFourMove(sbyte column) : IMove
    {
        public int Index => column;

        public override string ToString()
        {
            return Index.ToString();
        }
    }

    public class BitConnectFour : IGame
    {
        public const sbyte RED = 0;
        public const sbyte YELLOW = 1;
        public const sbyte EMPTY = 2;
        public const sbyte HEIGHT = 6;
        public const sbyte WIDTH = 7;
        public const sbyte BB_WIDTH = 8;
        private const ulong MASK = 0xFFFF808080808080UL; // bottom rows and right column
        private static readonly int[] _directions = [
                -1, // left
                // 1, // right
                -BB_WIDTH, // up
                // BB_WIDTH, // down
                // -1 - BB_WIDTH, // down left
                // 1 - BB_WIDTH, // down right
                -1 + BB_WIDTH, // up left
                1 + BB_WIDTH, // up right
            ];

        public int PossibleMovesCount => WIDTH;
        public static int PossibleResultsCount => 3;

        private sbyte _player = RED;
        public int Player => _player;
        private sbyte _opponent => (sbyte)(1 - _player);

        private IMove? _lastMove = null;
        public IMove? LastMove => _lastMove;

        private int _moveCounter = 0;
        public int MoveCounter => _moveCounter;

        public bool IsOver
        {
            get
            {
                return EmptyMask == 0
                    || ConnectionsFromPositions(_redBoard) > 0
                    || ConnectionsFromPositions(_yellowBoard) > 0;
            }
        }

        public int Result
        {
            get
            {
                if (!IsOver)
                    return -1;

                if (ConnectionsFromPositions(_redBoard) > 0) // red has connections
                    return RED;
                if (ConnectionsFromPositions(_yellowBoard) > 0) // yellow has connections
                    return YELLOW;

                return PossibleResultsCount - 1; // draw
            }
        }

        private sbyte[] _columns;
        private ulong _redBoard;
        private ulong _yellowBoard;

        public ref ulong PlayerBoard
        {
            get
            {
                if (_player == RED)
                    return ref _redBoard;
                else
                    return ref _yellowBoard;
            }
        }

        public ref ulong OpponentBoard
        {
            get
            {
                if (_player == YELLOW)
                    return ref _redBoard;
                else
                    return ref _yellowBoard;
            }
        }

        public ulong EmptyMask => (~_redBoard) & (~_yellowBoard) & (~MASK);

        private bool _useZobrist;
        private ZobristHash? _zobristHash;
        public ulong zKey => _useZobrist ? _zobristHash!.Key : throw new Exception("ZobristHash is not used.");

        public BitConnectFour() : this(RED, true) { } // dummy parameterless constructor;

        public BitConnectFour(sbyte player = RED, bool useZobrist = true)
        {
            _player = player;

            _redBoard = 0UL;
            _yellowBoard = 0UL;
            _columns = new sbyte[WIDTH];

            _useZobrist = useZobrist;
            if (_useZobrist)
            {
                // the bitboards have size 8x8 with last two rows and the last column
                // being sentinels. For simplicity in zobrist hashing, we assume the last column
                // is used so that the indexes align.
                _zobristHash = new ZobristHash(nTypes: 3, nPositions: (WIDTH + 1) * HEIGHT);
                SetZobristHash();
            }
        }

        private void SetZobristHash()
        {
            _zobristHash!.ResetKey();
            _zobristHash!.UpdatePosition(RED, _redBoard);
            _zobristHash!.UpdatePosition(YELLOW, _yellowBoard);
            _zobristHash!.UpdatePosition(EMPTY, EmptyMask);
            if (_player == YELLOW)
                _zobristHash!.UpdatePlayer();
        }

        public IGame Copy(bool disableZobrist = false)
        {
            BitConnectFour newGame = new BitConnectFour(
                player: _player,
                useZobrist: disableZobrist ? false : _useZobrist);
            newGame._redBoard = _redBoard;
            newGame._yellowBoard = _yellowBoard;
            newGame._lastMove = _lastMove;
            newGame._moveCounter = _moveCounter;
            for (int i = 0; i < WIDTH; i++)
            {
                newGame._columns[i] = _columns[i];
            }

            return newGame;
        }

        public IMove[] GetMoves()
        {
            int freeColumnCount = 0;
            for (int i = 0; i < WIDTH; i++)
            {
                if (_columns[i] < HEIGHT)
                    freeColumnCount++;
            }

            IMove[] moves = new IMove[freeColumnCount];
            int idx = 0;
            for (int i = 0; i < WIDTH; i++)
            {
                if (_columns[i] < HEIGHT)
                    moves[idx++] = new BitConnectFourMove((sbyte)i);
            }

            return moves;
        }

        public IMove GetRandomMove()
        {
            ulong lastRowMask = 0b1111111UL.ShiftVertical(HEIGHT - 1);
            if (((_redBoard | _yellowBoard) & lastRowMask) > 0) // some columns are filled
            {
                var moves = GetMoves();
                return moves[Utils.RNG.Next(moves.Length)];
            }

            // if all columns are not filled, generate random action
            return new BitConnectFourMove((sbyte)Utils.RNG.Next(PossibleMovesCount));
        }

        public void MakeMove(IMove move, bool updateMove = true)
        {
            _lastMove = move;
            _moveCounter++;

            int column = move.Index;
            int row = _columns[column];
            ulong position = 1UL << (row * BB_WIDTH + column);
            PlayerBoard.SetPositions(position);
            _columns[column]++;
            if (_useZobrist)
            {
                _zobristHash!.UpdatePosition(_player, position);
                _zobristHash!.UpdatePosition(EMPTY, position);
                _zobristHash!.UpdatePlayer();
            }
            SwitchPlayers();
        }

        public void UndoMove(IMove move)
        {
            _lastMove = null;
            _moveCounter--;

            SwitchPlayers();

            int column = move.Index;
            _columns[column]--;
            int row = _columns[column];
            ulong position = 1UL << (row * BB_WIDTH + column);
            PlayerBoard.ClearPositions(position);

            if (_useZobrist)
            {
                _zobristHash!.UpdatePosition(_player, position);
                _zobristHash!.UpdatePosition(EMPTY, position);
                _zobristHash!.UpdatePlayer();
            }
        }

        public int ConnectionsFromPositions(ulong positions, int depth = 3)
        {
            int connections = 0;
            for (int i = 0; i < _directions.Length; i++)
            {
                ulong mask = positions;
                for (int d = 0; d < depth; d++)
                {
                    // shift horizontal can shift rows up as well if offset is large enough
                    mask = mask.ShiftHorizontal(_directions[i]) & positions;
                    if (mask == 0)
                        break;
                }
                connections += BitOperations.PopCount(mask);
            }
            return connections;
        }

        public float Evaluate()
        {
            // TODO possible improvements
            // directions should be weighted since it's easier to block a vertical connections

            float value;
            if (IsOver)
            {
                if (ConnectionsFromPositions(_redBoard) > 0)
                    value = 1e6f;
                else if (ConnectionsFromPositions(_yellowBoard) > 0)
                    value = -1e6f;
                else
                    value = 0.0f;
            }
            else
            {
                ulong _redMask = ~MASK & ~_yellowBoard;
                ulong _yellowMask = ~MASK & ~_redBoard;
                value = ConnectionsFromPositions(_redMask) - ConnectionsFromPositions(_yellowMask);
            }

            return value * (_player == RED ? 1 : -1);
        }

        public float MobilityEvaluate()
        {
            throw new NotImplementedException();
        }

        public float NaiveEvaluate()
        {
            throw new NotImplementedException();
        }

        public float RandomEvaluate()
        {
            throw new NotImplementedException();
        }

        public bool[] GetActionMasks(out IMove[] moves)
        {
            bool[] mask = new bool[PossibleMovesCount];
            moves = GetMoves();

            foreach (IMove move in moves)
            {
                mask[move.Index] = true;
            }
            return mask;
        }

        public IMove GetMoveFromAction(int action)
        {
            return new BitConnectFourMove((sbyte)action);
        }

        public IMove GetMoveFromString(string m)
        {
            return new BitConnectFourMove(sbyte.Parse(m));
        }

        public float[] GetObservation(ObservationMode mode = ObservationMode.FLAT)
        {
            switch (mode)
            {
                case ObservationMode.FLAT:
                    return GetFlatObservation();
                case ObservationMode.IMAGE:
                default:
                    throw new NotImplementedException();
            }
        }

        private float[] GetFlatObservation()
        {
            float[] obs = new float[2 * WIDTH * HEIGHT];
            const int offset = WIDTH * HEIGHT;
            var mask = PlayerBoard;
            while (mask > 0)
            {
                obs[CellIndexFromBitboard(mask.PopNextIndex())] = 1.0f;
            }
            mask = OpponentBoard;
            while (mask > 0)
            {
                obs[CellIndexFromBitboard(mask.PopNextIndex()) + offset] = 1.0f;
            }
            return obs;
        }

        public void SortMoves(ref IMove[] moves, int moveIndex)
        {
            // couldn't be bothered to implement this and
            // it's not really necessary for the thesis

            // throw new NotImplementedException();
        }

        public void SwitchPlayers()
        {
            _player = (sbyte)(1 - _player);
        }

        public void Display(bool showMoves = true)
        {
            char[,] symbols = new char[HEIGHT, WIDTH];
            ulong tmp = _redBoard;
            while (tmp > 0)
            {
                (int i, int j) = CellPositionFromBitboard(tmp.PopNextPosition().Index());
                symbols[i, j] = 'X';
            }
            tmp = _yellowBoard;
            while (tmp > 0)
            {
                (int i, int j) = CellPositionFromBitboard(tmp.PopNextPosition().Index());
                symbols[i, j] = 'O';
            }

            Console.Write("  ");
            for (int j = 0; j < WIDTH; ++j)
            {
                Console.Write((char)('a' + j) + " ");
            }
            Console.WriteLine();

            for (int i = HEIGHT - 1; i >= 0; --i)
            {
                Console.Write($"{i + 1} ");
                for (int j = 0; j < WIDTH; ++j)
                {
                    if (symbols[i, j] == '\0')
                        Console.Write("  ");
                    else
                        Console.Write(symbols[i, j] + " ");
                }
                Console.WriteLine();
            }
            Console.WriteLine(zKey);
        }

        private int CellIndexFromBitboard(int bbIndex)
        {
            int row = bbIndex / BB_WIDTH;
            int column = bbIndex % BB_WIDTH;

            return row * WIDTH + column;
        }

        private (int row, int column) CellPositionFromBitboard(int bbIndex)
        {
            int row = bbIndex / BB_WIDTH;
            int column = bbIndex % BB_WIDTH;

            return (row, column);
        }

        public override string ToString()
        {
            char[] chars = new char[HEIGHT * WIDTH + 1];

            ulong tmp = _redBoard;
            while (tmp > 0)
            {
                int idx = CellIndexFromBitboard(tmp.PopNextPosition().Index());
                chars[idx] = 'X';
            }
            tmp = _yellowBoard;
            while (tmp > 0)
            {
                int idx = CellIndexFromBitboard(tmp.PopNextPosition().Index());
                chars[idx] = 'O';
            }
            tmp = EmptyMask;
            while (tmp > 0)
            {
                int idx = CellIndexFromBitboard(tmp.PopNextPosition().Index());
                chars[idx] = '.';
            }

            chars[^1] = (char)(_player + '0');
            return new string(chars);
        }
    }
}