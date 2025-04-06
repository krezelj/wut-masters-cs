using System.Diagnostics;

using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Minimax<T>
    {
        private const int MAX_VAL = 10_000;

        private long _nodes = 0;
        private int _depth;

        private IGame<T>? _game;
        private T? _bestMoveInRoot;

        public Minimax(int depth)
        {
            _depth = depth;
        }

        public T? GetMove(IGame<T> game)
        {
            _game = game;
            var sw = new Stopwatch();
            sw.Start();
            float value = Search(_depth, 0, -MAX_VAL, MAX_VAL);
            Console.WriteLine($"Nodes: {_nodes} ({_nodes / (float)sw.ElapsedMilliseconds} kN/s)\tEvaluation: {value}\t");

            _nodes = 0;
            return _bestMoveInRoot;
        }

        private float Search(int depth, int ply, float alpha, float beta)
        {
            _nodes++;

            if (depth == 0 || _game!.IsOver)
                return _game!.Evaluate();
            bool isRoot = ply == 0;

            var moves = _game.GetMoves();
            float current_value = -MAX_VAL;
            float new_value;
            foreach (var move in moves)
            {
                _game.MakeMove(move);
                new_value = -Search(depth - 1, ply + 1, -beta, -alpha);
                _game.UndoMove(move);

                if (new_value > current_value)
                {
                    current_value = new_value;
                    alpha = MathF.Max(current_value, alpha);
                    if (isRoot)
                        _bestMoveInRoot = move;
                    if (alpha >= beta)
                        break; // prune

                }
            }

            return current_value;
        }
    }
}