namespace MastersAlgorithms.Games
{
    public class OthelloMove(int i, int j, int nullMoves, int boardSize) : IMove
    {
        public int Index => IsNull ? -1 : (i * boardSize + j);
        public int BoardSize => boardSize;
        public int I => i;
        public int J => j;
        public List<(int, int)>? Captures;
        public int NullMoves => nullMoves;
        public bool IsNull => I < 0;

        public static OthelloMove NullMove(int nullMoves)
        {
            return new OthelloMove(-1, -1, nullMoves, -1);
        }
    }

    public class Othello : IGame
    {
        public static int PossibleResultsCount => 3;
        public int PossibleMovesCount => _boardSize * _boardSize + 1;
        private const int BLACK = 0;
        private const int WHITE = 1;


        private int _player = BLACK;
        public int Player => _player;
        private int _opponent => 1 - _player;
        private int _nullMoves = 0;
        private int _boardSize;
        private bool _isEmpty(int i, int j) => !_whiteBoard[i, j] && !_blackBoard[i, j];
        private int _emptyCount;
        public bool IsOver => _emptyCount == 0 || _nullMoves == 2;

        public int Result => throw new NotImplementedException();

        private bool[,] _blackBoard;
        private bool[,] _whiteBoard;

        private bool[,] _playerBoard => _player == 0 ? _blackBoard : _whiteBoard;
        private bool[,] _opponentBoard => _player == 1 ? _blackBoard : _whiteBoard;

        public ulong zKey => throw new NotImplementedException(); // TODO implment updating zKey

        public Othello(int boardSize, int player = BLACK)
        {
            _player = player;

            _boardSize = boardSize;
            _blackBoard = new bool[boardSize, boardSize];
            _whiteBoard = new bool[boardSize, boardSize];

            int center = boardSize / 2;
            _blackBoard[center - 1, center - 1] = true;
            _blackBoard[center, center] = true;
            _whiteBoard[center, center - 1] = true;
            _whiteBoard[center - 1, center] = true;

            _emptyCount = _boardSize * _boardSize - 4;
        }

        public Othello(string state, int player = BLACK) : this((int)Math.Sqrt(state.Length - 2), player)
        {
            _emptyCount = 0;
            for (int i = 0; i < _boardSize; ++i)
            {
                for (int j = 0; j < _boardSize; ++j)
                {
                    if (state[i * _boardSize + j] == '.')
                        ++_emptyCount;
                    _blackBoard[i, j] = state[i * _boardSize + j] == 'X';
                    _whiteBoard[i, j] = state[i * _boardSize + j] == 'O';
                }
            }
            _player = state[^2] - '0';
            _nullMoves = state[^1] - '0';
        }

        private bool IsCapturePossible(int i, int j)
        {
            if (!_isEmpty(i, j))
                return false;

            var opponentDirections = Utils.GetNeighborDiffs(i, j, _boardSize, _boardSize, ignoreLimits: true);
            foreach ((int di, int dj) in opponentDirections)
            {
                int p = i + di;
                int q = j + dj;
                if (!Utils.InLimits(p, q, _boardSize, _boardSize) || !_opponentBoard[p, q])
                    continue;
                while (true)
                {
                    p += di;
                    q += dj;

                    if (!Utils.InLimits(p, q, _boardSize, _boardSize) || !_opponentBoard[p, q])
                        break;
                }

                if (Utils.InLimits(p, q, _boardSize, _boardSize) && _playerBoard[p, q])
                    return true;
            }

            return false;
        }

        public IMove[] GetMoves()
        {
            List<OthelloMove> moves = new List<OthelloMove>();

            for (int i = 0; i < _boardSize; ++i)
            {
                for (int j = 0; j < _boardSize; ++j)
                {
                    if (IsCapturePossible(i, j))
                        moves.Add(new OthelloMove(i, j, _nullMoves, _boardSize));
                }
            }
            if (moves.Count == 0)
                moves.Add(OthelloMove.NullMove(_nullMoves));
            return moves.Cast<IMove>().ToArray();
        }

        public IMove GetRandomMove()
        {
            // to avoid infinite loop, educated guess, seems to work well
            int MAX_TRIES = _boardSize * _boardSize;

            // if there is enough empty space, we can try random moves
            bool CAN_TRY = _emptyCount > 3;

            ulong mask = 0; // !!! Assuming max board size is 8x8
            for (int t = 0; t < MAX_TRIES && CAN_TRY; ++t)
            {
                int idx = Utils.RNG.Next(0, _boardSize * _boardSize);
                if ((mask & (1UL << idx)) != 0)
                    continue;
                int i = idx / _boardSize;
                int j = idx % _boardSize;
                if (IsCapturePossible(i, j))
                    return new OthelloMove(i, j, _nullMoves, _boardSize);
                mask |= 1UL << idx;
            }

            var moves = GetMoves();
            return moves[Utils.RNG.Next(moves.Length)];
        }

        public void SortMoves(ref IMove[] moves, int moveIndex)
        {
            throw new NotImplementedException();
        }

        public IMove GetMoveFromString(string m)
        {
            // TODO This should return a FULL move includding flip mask
            throw new NotImplementedException();
        }

        private List<(int, int)> GetCapturesFromPosition(int i, int j)
        {
            List<(int, int)>? captures = new List<(int, int)>(); ;
            var opponentDirections = Utils.GetNeighborDiffs(i, j, _boardSize, _boardSize, ignoreLimits: true);
            foreach ((int di, int dj) in opponentDirections)
            {
                List<(int, int)> capture = new List<(int, int)>();

                int p = i + di;
                int q = j + dj;
                if (!Utils.InLimits(p, q, _boardSize, _boardSize) || !_opponentBoard[p, q])
                    continue;

                while (true)
                {
                    capture.Add((p, q));
                    p += di;
                    q += dj;

                    if (!Utils.InLimits(p, q, _boardSize, _boardSize) || !_opponentBoard[p, q])
                        break;
                }
                if (Utils.InLimits(p, q, _boardSize, _boardSize) && _playerBoard[p, q])
                    captures.AddRange(capture);
            }
            return captures;
        }

        private void MakeCapturesFromPosition(int i, int j)
        {
            var opponentDirections = Utils.GetNeighborDiffs(i, j, _boardSize, _boardSize, ignoreLimits: true);
            foreach ((int di, int dj) in opponentDirections)
            {
                int lineLength = 1;
                int p = i + di;
                int q = j + dj;
                if (!Utils.InLimits(p, q, _boardSize, _boardSize) || !_opponentBoard[p, q])
                    continue;

                while (true)
                {
                    p += di;
                    q += dj;
                    if (!Utils.InLimits(p, q, _boardSize, _boardSize) || !_opponentBoard[p, q])
                        break;
                    ++lineLength;
                }
                if (!Utils.InLimits(p, q, _boardSize, _boardSize) || !_playerBoard[p, q])
                    continue;

                while (lineLength > 0)
                {
                    p -= di;
                    q -= dj;
                    --lineLength;
                    _playerBoard[p, q] = true;
                    _opponentBoard[p, q] = false;
                }
            }
        }

        private void FlipPositions(List<(int, int)> captures, bool[,] toTrue, bool[,] toFalse)
        {
            foreach ((int i, int j) in captures)
            {
                toTrue[i, j] = true;
                toFalse[i, j] = false;
            }
        }

        public void MakeMove(IMove m, bool updateMove = true)
        {
            OthelloMove move = (OthelloMove)m;
            if (move.IsNull)
            {
                ++_nullMoves;
            }
            else
            {
                --_emptyCount;
                _playerBoard[move.I, move.J] = true;
                if (updateMove)
                {
                    var captures = GetCapturesFromPosition(move.I, move.J);
                    FlipPositions(captures, _playerBoard, _opponentBoard);
                    if (updateMove)
                        move.Captures = captures;
                }
                else
                {
                    MakeCapturesFromPosition(move.I, move.J);
                }

                _nullMoves = 0;
            }
            _player = 1 - _player;
        }

        public void UndoMove(IMove m)
        {
            OthelloMove move = (OthelloMove)m;
            _player = 1 - _player;
            if (move.IsNull)
            {
                --_nullMoves;
            }
            else
            {
                ++_emptyCount;
                _playerBoard[move.I, move.J] = false;
                FlipPositions(move.Captures!, _opponentBoard, _playerBoard);
                _nullMoves = move.NullMoves;
            }
        }

        public float NaiveEvaluate() => throw new NotImplementedException();
        public float MobilityEvaluate() => throw new NotImplementedException();
        public float RandomEvaluate() => throw new NotImplementedException();

        public float Evaluate()
        {
            float value = 0;
            for (int i = 0; i < _boardSize; ++i)
            {
                for (int j = 0; j < _boardSize; ++j)
                {
                    if (_blackBoard[i, j])
                        ++value;
                    else if (_whiteBoard[i, j])
                        --value;
                }
            }

            if (IsOver)
            {
                value = MathF.Sign(value);
            }
            else
            {
                value = value / (_boardSize * _boardSize);
            }

            return value * (_player == 0 ? 1 : -1);
        }

        public IGame Copy(bool disableZobrist = false)
        {
            throw new NotImplementedException(); // TODO handle disableZobrist

            // Othello newGame = new Othello(_boardSize, _player);
            // newGame._nullMoves = _nullMoves;
            // newGame._emptyCount = _emptyCount;

            // for (int i = 0; i < _boardSize; ++i)
            // {
            //     for (int j = 0; j < _boardSize; ++j)
            //     {
            //         newGame._blackBoard[i, j] = _blackBoard[i, j];
            //         newGame._whiteBoard[i, j] = _whiteBoard[i, j];
            //     }
            // }

            // return newGame;
        }

        public float[] GetObservation(ObservationMode mode = ObservationMode.FLAT)
        {
            throw new NotImplementedException();
        }

        public bool[] GetActionMasks(out IMove[] moves)
        {
            throw new NotImplementedException();
        }

        public void Display(bool showMoves = false)
        {
            Console.Write("  ");
            for (int j = 0; j < _boardSize; ++j)
            {
                Console.Write((char)('a' + j) + " ");
            }
            Console.WriteLine();

            var moves = showMoves ? GetMoves().Cast<OthelloMove>().ToList() : null;

            for (int i = 0; i < _boardSize; ++i)
            {
                Console.Write($"{i + 1} ");
                for (int j = 0; j < _boardSize; ++j)
                {
                    if (_blackBoard[i, j])
                        Console.Write("X ");
                    else if (_whiteBoard[i, j])
                        Console.Write("O ");
                    else if (showMoves && moves!.Any(m => m.I == i && m.J == j))
                        Console.Write(". ");
                    else
                        Console.Write("  ");
                }
                Console.WriteLine();
            }
        }

        public override string ToString()
        {
            char[] chars = new char[_boardSize * _boardSize + 2];
            for (int i = 0; i < _boardSize; ++i)
            {
                for (int j = 0; j < _boardSize; ++j)
                {
                    if (_blackBoard[i, j])
                        chars[i * _boardSize + j] = 'X';
                    else if (_whiteBoard[i, j])
                        chars[i * _boardSize + j] = 'O';
                    else
                        chars[i * _boardSize + j] = '.';
                }
            }
            chars[^2] = (char)(_player + '0');
            chars[^1] = (char)(_nullMoves + '0');
            return new string(chars);
        }

    }
}