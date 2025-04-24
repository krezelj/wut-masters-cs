using System.Diagnostics;
using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class MinimaxFast : IAlgorithm
    {
        private record struct Transposition
        (
            ulong zKey,
            // IMove move,
            float evaluation,
            int depth,
            int nodeType
        );
        private Transposition[] _transpositions = new Transposition[0x400000];


        private const int MAX_VAL = 10_000_000;

        private long _nodes = 0;
        private long _cacheHits = 0;
        private int _depth;
        private bool _verbose;

        private Stopwatch _sw;
        private float _time;
        private float _value;
        private IGame? _game;
        private IMove? _bestMoveInRoot;

        public MinimaxFast(int depth, bool verbose = false)
        {
            _sw = new Stopwatch();
            _depth = depth;
            _verbose = verbose;
        }

        public IMove? GetMove(IGame game)
        {
            _nodes = 0;
            _cacheHits = 0;
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

            ulong zKey = _game.zKey;
            ref Transposition tMatch = ref _transpositions[zKey & 0x3FFFFF];
            if (tMatch.zKey == zKey && !isRoot && tMatch.depth >= depth)
            {
                bool cacheHit = false;
                if (tMatch.nodeType == 1)
                    cacheHit = true;
                else if (tMatch.nodeType == 0 && tMatch.evaluation <= alpha)
                    cacheHit = true;
                else if (tMatch.nodeType == 2 && tMatch.evaluation >= beta)
                    cacheHit = true;
                if (cacheHit)
                {
                    _cacheHits++;
                    return tMatch.evaluation;
                }
            }

            var moves = _game.GetMoves();
            // IMove? currentBestMove = null;
            float currentValue = -MAX_VAL;
            float newValue;
            float startAlpha = alpha;
            foreach (var move in moves)
            {
                _game.MakeMove(move);
                newValue = -Search(depth - 1, ply + 1, -beta, -alpha);
                _game.UndoMove(move);

                if (newValue > currentValue)
                {
                    currentValue = newValue;
                    // currentBestMove = move;
                    alpha = MathF.Max(currentValue, alpha);
                    if (isRoot)
                        _bestMoveInRoot = move;
                    if (alpha >= beta)
                        break; // prune

                }
            }

            tMatch = new(
                zKey,
                // currentBestMove!,
                currentValue,
                depth,
                currentValue >= beta ? 2 : currentValue <= startAlpha ? 0 : 1);

            return currentValue;
        }

        public string GetDebugInfo()
        {
            return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | Cache Hits {3,7} | Eval {4}",
                _nodes, _nodes / _time, _time, _cacheHits, _value);
        }
    }
}