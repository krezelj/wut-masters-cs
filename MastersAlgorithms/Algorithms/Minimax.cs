#if DEBUG
using System.Diagnostics;
#endif

using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Minimax<T>
    {
#if DEBUG
        private long _nodes = 0;
#endif
        private int _depth;

        private IGame<T> _game;
        private T _bestMoveInRoot;

        public Minimax(int depth)
        {
            _depth = depth;
        }

        public T GetMove(IGame<T> game)
        {
            _game = game;
#if DEBUG
            var sw = new Stopwatch();
            sw.Start();
#endif
            Search(_depth, 0, int.MinValue, int.MaxValue);
#if DEBUG
            Console.WriteLine($"Nodes: {_nodes} ({_nodes / (float)sw.ElapsedMilliseconds} kN/s)");
#endif
            return _bestMoveInRoot;
        }

        private int Search(int depth, int ply, int alpha, int beta)
        {
#if DEBUG
            _nodes++;
#endif
            bool isRoot = ply == 0;

            if (depth == 0 || _game.IsOver)
                return _game.Evaluate();

            var moves = _game.GetMoves();
            // TODO -- Order moves

            int current_value = int.MinValue;
            int new_value;
            foreach (var move in moves)
            {
                _game.MakeMove(move);
                new_value = Search(depth - 1, ply + 1, -beta, -alpha);
                _game.UndoMove(move);

                if (new_value > current_value)
                {
                    current_value = new_value;
                    alpha = Math.Max(current_value, alpha);
                    if (isRoot)
                        _bestMoveInRoot = move;
                    // if (alpha > beta)
                    //     break; // prune

                }
            }

            return current_value;
        }
    }
}