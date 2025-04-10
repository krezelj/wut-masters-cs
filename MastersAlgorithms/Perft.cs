
using System.Diagnostics;
using MastersAlgorithms.Games;

namespace MastersAlgorithm
{
    public class Perft
    {
        private int _maxDepth;
        private ulong _leafNodes;
        private IGame? _game;

        public Perft(int maxDepth)
        {
            _maxDepth = maxDepth;
        }

        public void Run(IGame game)
        {
            _game = game;
            var sw = new Stopwatch();
            for (int depth = 1; depth <= _maxDepth; ++depth)
            {
                _leafNodes = 0;
                sw.Restart();
                Search(depth);
                sw.Stop();

                float kns = _leafNodes / (float)sw.ElapsedMilliseconds;
                Console.WriteLine("depth {0,2} | {1,11} | {2,8}ms | {3,6}kN/s |",
                    depth, _leafNodes, sw.ElapsedMilliseconds, MathF.Round(kns, 2));
            }
        }

        private void Search(int depth)
        {
            if (depth == 0 || _game!.IsOver)
            {
                _leafNodes++;
                return;
            }

            var moves = _game!.GetMoves();
            foreach (var move in moves)
            {
                _game.MakeMove(move);
                Search(depth - 1);
                _game.UndoMove(move);
            }
        }
    }
}