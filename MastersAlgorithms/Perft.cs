
using System.Diagnostics;
using MastersAlgorithms.Games;

namespace MastersAlgorithms
{
    public class Perft
    {
        private int _maxDepth;
        private bool _reportUniqueGames;
        private HashSet<string>? _uniqueGames;
        private ulong _leafNodes;
        private IGame? _game;

        public Perft(int maxDepth, bool reportUniqueGames = true)
        {
            _maxDepth = maxDepth;
            _reportUniqueGames = reportUniqueGames;
        }

        public void Run(IGame game)
        {
            _game = game;
            var sw = new Stopwatch();
            for (int depth = 1; depth <= _maxDepth; ++depth)
            {
                _uniqueGames = new HashSet<string>();
                _leafNodes = 0;
                sw.Restart();
                Search(depth);
                sw.Stop();

                float kns = _leafNodes / (float)sw.ElapsedTicks * Stopwatch.Frequency / 1000;
                Console.WriteLine("depth {0,2} | {1,11} | {2,8}ms | {3,8}kN/s | {4,10} Unique |",
                    depth, _leafNodes, sw.ElapsedMilliseconds, MathF.Round(kns, 2), _uniqueGames.Count);
            }
        }

        private void Search(int depth)
        {
            if (depth == 0 || _game!.IsOver)
            {
                if (depth == 0 && _reportUniqueGames)
                    _uniqueGames!.Add(_game!.ToString()!);

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

    public class EngineComparator
    {
        private int _maxDepth;
        private IGame? _engine1;
        private IGame? _engine2;

        private Stack<IMove> _moveHistory1;
        private Stack<IMove> _moveHistory2;

        public EngineComparator(int maxDepth)
        {
            _maxDepth = maxDepth;
            _moveHistory1 = new Stack<IMove>();
            _moveHistory2 = new Stack<IMove>();
        }

        public void Compare(IGame engine1, IGame engine2)
        {
            _engine1 = engine1;
            _engine2 = engine2;

            try
            {
                Search(_maxDepth);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                _engine2.GetMoves();
                while (_moveHistory1.Count > 0)
                {
                    var move1 = _moveHistory1.Pop();
                    Console.WriteLine(move1.Index);
                    _engine1.Display(showMoves: true);
                    _engine1.UndoMove(move1);

                    var move2 = _moveHistory2.Pop();
                    Console.WriteLine(move2.Index);
                    _engine2.Display(showMoves: true);
                    _engine2.UndoMove(move2);
                }
                _engine1.Display(showMoves: true);
                _engine2.Display(showMoves: true);
                return;
            }
            Console.WriteLine("Comparison Successful!");
        }

        private void Search(int depth)
        {
            if (depth == 0 || _engine1!.IsOver || _engine2!.IsOver)
                return;

            var moves1 = _engine1.GetMoves();
            var moves2 = _engine2.GetMoves();

            if (moves1.Length != moves2.Length)
                throw new Exception("Moves do not match!");

            Array.Sort(moves1, (x, y) => x.Index - y.Index);
            Array.Sort(moves2, (x, y) => x.Index - y.Index);
            for (int i = 0; i < moves1.Length; i++)
            {
                if (moves1[i].Index != moves2[i].Index)
                    throw new Exception("Moves do not match!");
            }

            // theoretically moves match (?)
            for (int i = 0; i < moves1.Length; i++)
            {
                _engine1.MakeMove(moves1[i]);
                _engine2.MakeMove(moves2[i]);
                _moveHistory1.Push(moves1[i]);
                _moveHistory2.Push(moves2[i]);

                Search(depth - 1);

                _engine1.UndoMove(moves1[i]);
                _engine2.UndoMove(moves2[i]);
                _moveHistory1.Pop();
                _moveHistory2.Pop();
            }
        }
    }
}