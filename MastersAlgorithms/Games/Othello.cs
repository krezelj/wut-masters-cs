namespace MastersAlgorithms.Games
{
    public class Othello : IGame<OthelloMove>
    {
        private const int BLACK = 0;
        private const int WHITE = 1;


        private int _player = BLACK;
        private int _opponent => 1 - _player;
        private int _nullMoves = 0;
        private int _boardSize;
        private (int, int) _shape => (_boardSize, _boardSize);
        private bool _isEmpty(int i, int j) => !_whiteBoard[i, j] && !_blackBoard[i, j];
        private int _emptyCount;
        public bool IsOver => _emptyCount == 0 || _nullMoves == 2;

        private bool[,] _blackBoard;
        private bool[,] _whiteBoard;

        private bool[,] _playerBoard => _player == 0 ? _blackBoard : _whiteBoard;
        private bool[,] _opponentBoard => _player == 1 ? _blackBoard : _whiteBoard;

        public Othello(int boardSize, int player = 0)
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

        public Othello(string state, int player = 0) : this((int)Math.Sqrt(state.Length - 1), player)
        {
            _emptyCount = 0;
            for (int i = 0; i < _boardSize; i++)
            {
                for (int j = 0; j < _boardSize; j++)
                {
                    if (state[i * _boardSize + j] == '.')
                        _emptyCount++;
                    _blackBoard[i, j] = state[i * _boardSize + j] == 'X';
                    _whiteBoard[i, j] = state[i * _boardSize + j] == 'O';
                }
            }
            _player = state[^2] - '0';
            _nullMoves = state[^1] - '0';
        }

        private List<(int, int)>? GetCapturesFromPosition(int i, int j)
        {
            if (!_isEmpty(i, j))
                return null;

            var opponentDirections = Utils.GetNeighborDiffs(i, j, _shape, (x, y) => _opponentBoard[x, y]);
            List<(int, int)> captures = new List<(int, int)>();
            foreach ((int di, int dj) in opponentDirections)
            {
                int p = i + di;
                int q = j + dj;
                List<(int, int)> possibleCaptures = new List<(int, int)>();
                while (true)
                {
                    if (!Utils.InLimits(p, q, _shape))
                        break;
                    if (!_opponentBoard[p, q])
                        break;
                    possibleCaptures.Add((p, q));

                    p += di;
                    q += dj;
                }

                if (Utils.InLimits(p, q, _shape) && _playerBoard[p, q])
                    captures.AddRange(possibleCaptures);
            }
            return captures.Count > 0 ? captures : null;
        }

        public List<OthelloMove> GetMoves()
        {
            List<OthelloMove> moves = new List<OthelloMove>();

            for (int i = 0; i < _boardSize; i++)
            {
                for (int j = 0; j < _boardSize; j++)
                {
                    var captures = GetCapturesFromPosition(i, j);
                    if (captures != null)
                        moves.Add(new OthelloMove(i, j, captures, _nullMoves));
                }
            }
            if (moves.Count == 0)
                moves.Add(OthelloMove.NullMove());
            return moves;
        }

        public OthelloMove GetRandomMove()
        {
            return OthelloMove.NullMove();
        }

        public void MakeMove(OthelloMove move)
        {
            if (move.IsNull)
            {
                _nullMoves++;
            }
            else
            {
                _emptyCount--;
                _playerBoard[move.I, move.J] = true;
                foreach ((int i, int j) in move.Captures!)
                {
                    _opponentBoard[i, j] = false;
                    _playerBoard[i, j] = true;
                }
                _nullMoves = 0;
            }
            _player = 1 - _player;
        }

        public void UndoMove(OthelloMove move)
        {
            _player = 1 - _player;
            if (move.IsNull)
            {
                _nullMoves--;
            }
            else
            {
                _emptyCount++;
                _playerBoard[move.I, move.J] = false;
                foreach ((int i, int j) in move.Captures!)
                {
                    _opponentBoard[i, j] = true;
                    _playerBoard[i, j] = false;
                }
                _nullMoves = move.NullMoves;
            }
        }

        public int Evaluate()
        {
            int value = 0;
            for (int i = 0; i < _boardSize; i++)
            {
                for (int j = 0; j < _boardSize; j++)
                {
                    if (_blackBoard[i, j])
                        value++;
                    else if (_whiteBoard[i, j])
                        value--;
                }
            }
            return value * (_player == 0 ? 1 : -1);
        }

        public void Display(bool showMoves = false)
        {
            Console.Write("  ");
            for (int j = 0; j < _boardSize; j++)
            {
                Console.Write((char)('a' + j) + " ");
            }
            Console.WriteLine();

            var moves = showMoves ? GetMoves() : null;

            for (int i = 0; i < _boardSize; i++)
            {
                Console.Write($"{i + 1} ");
                for (int j = 0; j < _boardSize; j++)
                {
                    if (_blackBoard[i, j])
                        Console.Write("X ");
                    else if (_whiteBoard[i, j])
                        Console.Write("O ");
                    else if (showMoves && moves!.Any(m => m.I == i && m.J == j))
                        Console.Write("+ ");
                    else
                        Console.Write(". ");
                }
                Console.WriteLine();
            }
        }

        public override string ToString()
        {
            char[] chars = new char[_boardSize * _boardSize + 1];
            for (int i = 0; i < _boardSize; i++)
            {
                for (int j = 0; j < _boardSize; j++)
                {
                    if (_blackBoard[i, j])
                        chars[i * _boardSize + j] = 'X';
                    else if (_whiteBoard[i, j])
                        chars[i * _boardSize + j] = 'O';
                    else
                        chars[i * _boardSize + j] = '.';
                }
            }
            chars[^2] = (char)(_nullMoves + '0');
            chars[^1] = (char)(_nullMoves + '0');
            return new string(chars);
        }

    }
}