using System.Diagnostics;

using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Minimax : IAlgorithm
    {
        private const int MAX_VAL = 10_000_000;

        private long _nodes = 0;
        private int _depth;
        private bool _verbose;

        private Stopwatch _sw;
        private float _time;
        private float _value;
        private IGame? _game;
        private IMove? _bestMoveInRoot;

        public Minimax(int depth, bool verbose = false)
        {
            _sw = new Stopwatch();
            _depth = depth;
            _verbose = verbose;
        }

        public IMove? GetMove(IGame game)
        {
            _nodes = 0;
            _game = game;
            _sw.Restart();
            _value = Search(_depth, 0, -MAX_VAL, MAX_VAL);
            _time = _sw.ElapsedMilliseconds;
            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            return _bestMoveInRoot;
        }

        private float Search(int depth, int ply, float alpha, float beta)
        {
            _nodes++;

            if (depth == 0 || _game!.IsOver)
                return _game!.Evaluate();
            bool isRoot = ply == 0;

            var moves = _game.GetMoves();
            float currentValue = -MAX_VAL;
            float newValue;
            foreach (var move in moves)
            {
                _game.MakeMove(move);
                newValue = -Search(depth - 1, ply + 1, -beta, -alpha);
                _game.UndoMove(move);

                if (newValue > currentValue)
                {
                    currentValue = newValue;
                    alpha = MathF.Max(currentValue, alpha);
                    if (isRoot)
                        _bestMoveInRoot = move;
                    if (alpha >= beta)
                        break; // prune

                }
            }

            return currentValue;
        }

        public string GetDebugInfo()
        {
            return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | Eval {3}",
                _nodes, _nodes / _time, _time, _value);
        }
    }
}