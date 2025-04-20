using System.Numerics;

namespace MastersAlgorithms.Games
{

    public class BitOthelloMove(ulong position, sbyte nullMoves) : IMove
    {
        public int Index => IsNull ? -1 : position.Index();
        public ulong Position => position;
        public bool IsNull => position == 0;
        public sbyte NullMoves => nullMoves;
        public ulong FlipMask;

        public static BitOthelloMove NullMove(sbyte nullMoves)
        {
            return new BitOthelloMove(0UL, nullMoves);
        }
    }

    public class BitOthello : IGame
    {
        private const sbyte BLACK = 0;
        private const sbyte WHITE = 1;
        private const sbyte BOARD_SIZE = 8;

        private sbyte _player = BLACK;
        public int Player => _player;

        public bool IsOver => _emptyMask == 0 || _nullMoves == 2;

        private ulong _blackBoard;
        private ulong _whiteBoard;

        private ref ulong _playerBoard
        {
            get
            {
                if (_player == 0)
                    return ref _blackBoard;
                else
                    return ref _whiteBoard;
            }
        }
        private ref ulong _opponentBoard
        {
            get
            {
                if (_player == 1)
                    return ref _blackBoard;
                else
                    return ref _whiteBoard;
            }
        }

        private ulong _emptyMask => (~_whiteBoard) & (~_blackBoard);
        private sbyte _nullMoves;

        public BitOthello(int player = BLACK)
        {
            _blackBoard = 0UL;
            _whiteBoard = 0UL;

            ulong center = BitBoard.GetPosition(BOARD_SIZE / 2 - 1, BOARD_SIZE / 2 - 1);
            _blackBoard.SetPositions(center);
            _blackBoard.SetPositions(center << 1 + BOARD_SIZE);

            _whiteBoard.SetPositions(center << 1);
            _whiteBoard.SetPositions(center << BOARD_SIZE);

            // _emptyCount = BOARD_SIZE * BOARD_SIZE - 4;
            _nullMoves = 0;
        }

        public BitOthello(string state, int player = BLACK) : this(player)
        {
            _blackBoard = 0UL;
            _whiteBoard = 0UL;
            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    if (state[i * BOARD_SIZE + j] == 'X')
                        _blackBoard.SetPositions(BitBoard.GetPosition(i, j));
                    if (state[i * BOARD_SIZE + j] == 'O')
                        _whiteBoard.SetPositions(BitBoard.GetPosition(i, j));
                }
            }
            _player = (sbyte)(state[^2] - '0');
            _nullMoves = (sbyte)(state[^1] - '0');
        }

        private bool IsCapturePossible(ulong position)
        {
            ulong opponentNeighbors = position.Expand8() & _opponentBoard;
            while (opponentNeighbors > 0)
            {
                ulong mask = opponentNeighbors.PopNextPosition();
                int offset = mask.Index() - position.Index();

                do
                {
                    mask.ShiftToNeighbor(offset);
                    if (!_opponentBoard.Contains(mask))
                        break;
                } while (mask > 0);

                if (_playerBoard.Contains(mask))
                    return true;
            }
            return false;
        }

        public List<IMove> GetMoves()
        {
            List<BitOthelloMove> moves = new List<BitOthelloMove>();

            // empty positions next to opponent discs
            ulong perimeter = _opponentBoard.Expand8() & _emptyMask;
            while (perimeter > 0)
            {
                ulong position = perimeter.PopNextPosition();
                if (IsCapturePossible(position))
                {
                    moves.Add(new BitOthelloMove(position, _nullMoves));
                }
            }

            if (moves.Count == 0)
                moves.Add(BitOthelloMove.NullMove(_nullMoves));
            return moves.Cast<IMove>().ToList();
        }

        public IMove GetRandomMove()
        {
            throw new NotImplementedException();
            // TODO Get random bit and check if its in perimeter first
        }

        private ulong GetCapturesFromPosition(ulong position)
        {
            ulong capturesMask = 0UL;

            ulong opponentNeighbors = position.Expand8() & _opponentBoard;
            while (opponentNeighbors > 0)
            {
                ulong mask = opponentNeighbors.PopNextPosition();
                ulong captureMask = mask;
                int offset = mask.Index() - position.Index();

                do
                {
                    mask.ShiftToNeighbor(offset);
                    if (!_opponentBoard.Contains(mask))
                        break;
                    captureMask |= mask;
                } while (mask > 0);

                if (_playerBoard.Contains(mask))
                    capturesMask |= captureMask;
            }
            return capturesMask;
        }

        public void MakeMove(IMove m, bool updateMove = true)
        {
            BitOthelloMove move = (BitOthelloMove)m;
            if (move.IsNull)
            {
                ++_nullMoves;
            }
            else
            {
                // --_emptyCount;
                ulong flipMask = GetCapturesFromPosition(move.Position);
                _playerBoard.SetPositions(move.Position | flipMask);
                _opponentBoard.ClearPositions(flipMask);
                move.FlipMask = flipMask;
                _nullMoves = 0;
            }
            _player = (sbyte)(1 - _player);
        }

        public void UndoMove(IMove m)
        {
            BitOthelloMove move = (BitOthelloMove)m;
            _player = (sbyte)(1 - _player);
            if (move.IsNull)
            {
                --_nullMoves;
            }
            else
            {
                // ++_emptyCount;
                _playerBoard.ClearPositions(move.Position | move.FlipMask);
                _opponentBoard.SetPositions(move.FlipMask);
                _nullMoves = move.NullMoves;
            }
        }

        public float Evaluate()
        {
            int blackCount = BitOperations.PopCount(_blackBoard);
            int whiteCount = BitOperations.PopCount(_whiteBoard);
            float value = blackCount - whiteCount;

            if (IsOver)
            {
                value = MathF.Sign(value);
            }
            else
            {
                value = value / (BOARD_SIZE * BOARD_SIZE);
            }

            return value * (_player == 0 ? 1 : -1);
        }

        public IGame Copy()
        {
            BitOthello newGame = new BitOthello(_player);
            newGame._nullMoves = _nullMoves;
            newGame._blackBoard = _blackBoard;
            newGame._whiteBoard = _whiteBoard;
            return newGame;
        }

        public void Display(bool showMoves = false)
        {
            char[,] symbols = new char[BOARD_SIZE, BOARD_SIZE];
            ulong tmp = _blackBoard;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                int i = idx / BOARD_SIZE;
                int j = idx % BOARD_SIZE;
                symbols[i, j] = 'X';
            }
            tmp = _whiteBoard;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                int i = idx / BOARD_SIZE;
                int j = idx % BOARD_SIZE;
                symbols[i, j] = 'O';
            }
            var moves = showMoves ? GetMoves().Cast<BitOthelloMove>().ToList() : null;
            if (moves != null)
            {
                foreach (var move in moves)
                {
                    if (move.Index < 0)
                        continue;
                    int i = move.Index / BOARD_SIZE;
                    int j = move.Index % BOARD_SIZE;
                    symbols[i, j] = '.';
                }
            }


            Console.Write("  ");
            for (int j = 0; j < BOARD_SIZE; ++j)
            {
                Console.Write((char)('a' + j) + " ");
            }
            Console.WriteLine();

            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                Console.Write($"{i + 1} ");
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    if (symbols[i, j] == '\0')
                        Console.Write("  ");
                    else
                        Console.Write(symbols[i, j] + " ");
                }
                Console.WriteLine();
            }
        }
    }
}