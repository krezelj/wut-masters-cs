using System.Diagnostics;
using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class MinimaxFast : IAlgorithm
    {
        private record struct Transposition
        (
            ulong zKey,
            int moveIndex,
            float evaluation,
            int depth,
            int nodeType
        );
        private Transposition[] _transpositions = new Transposition[0x400000];


        private const int MAX_VAL = 10_000_000;

        private long _nodes = 0;
        private long _prunes = 0;
        private long _cacheHits = 0;
        private int _depth;
        private Func<IGame, float> _evalFunc;
        private Func<IGame, IMove[], int, IMove[]> _sortFunc;
        private bool _verbose;

        private Stopwatch _sw;
        private float _time;
        private float _value;
        private IGame? _game;
        private IMove? _bestMoveInRoot;

        public MinimaxFast(
            int depth,
            Func<IGame, float> evalFunc,
            Func<IGame, IMove[], int, IMove[]> sortFunc,
            bool verbose = false)
        {
            _sw = new Stopwatch();
            _depth = depth;
            _evalFunc = evalFunc;
            _sortFunc = sortFunc;
            _verbose = verbose;
        }

        public IMove? GetMove(IGame game)
        {
            _nodes = 0;
            _prunes = 0;
            _cacheHits = 0;
            _game = game;
            _sw.Restart();

            var moves = _game.GetMoves();
            if (moves.Length == 1)
            {
                _bestMoveInRoot = moves[0];
            }
            else
            {
                for (int currentDepth = 1; currentDepth <= _depth; ++currentDepth)
                {
                    _value = Search(currentDepth, 0, -MAX_VAL, MAX_VAL);
                }
            }

            _time = _sw.ElapsedMilliseconds;
            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            return _bestMoveInRoot;
        }

        private float Search(int depth, int ply, float alpha, float beta)
        {
            _nodes++;

            if (depth == 0 || _game!.IsOver)
                return _evalFunc(_game!);
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
            int currentBestMoveIndex = -1;

            moves = _sortFunc(_game, moves, tMatch.moveIndex);

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
                    currentBestMoveIndex = move.Index;
                    alpha = MathF.Max(currentValue, alpha);
                    if (isRoot)
                        _bestMoveInRoot = move;
                    if (alpha >= beta)
                    {
                        _prunes++;
                        break;
                    }

                }
            }

            tMatch = new(
                zKey,
                currentBestMoveIndex,
                currentValue,
                depth,
                currentValue >= beta ? 2 : currentValue <= startAlpha ? 0 : 1);

            return currentValue;
        }

        public string GetDebugInfo()
        {
            return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | Cache Hits {3,7} | Prunes {4,7} | Eval {5}",
                _nodes, _nodes / _time, _time, _cacheHits, _prunes, _value);
        }

        public static MinimaxFast GetAgentMinimax(
            int depth,
            Agent agent,
            float temperature = 10.0f,
            float probThreshold = 0.01f,
            bool verbose = false)
        {
            AgentControllerMinimax ac = new AgentControllerMinimax(agent, temperature, probThreshold);
            return new MinimaxFast(
                depth: depth,
                evalFunc: ac.Evaluate,
                sortFunc: ac.SortMoves,
                verbose: verbose
            );
        }
    }

    public class AgentControllerMinimax
    {
        private Agent _agent;
        private float _temperature;
        private float _probThreshold;

        public AgentControllerMinimax(Agent agent, float temperature = 1.0f, float probThreshold = 0.01f)
        {
            _agent = agent;
            _temperature = temperature;
            _probThreshold = probThreshold;
        }

        public float Evaluate(IGame game)
        {
            if (game.IsOver)
                return game.Evaluate();
            float[] obs = Utils.GetFlatObservations([game], _agent.CriticMode);
            float value = _agent.Policy.GetValues(obs, batchCount: 1)[0];
            return value;
        }

        public IMove[] SortMoves(IGame game, IMove[] moves, int moveIndex)
        {
            float[] logits = _agent.Policy.GetLogits(game.GetObservation(mode: _agent.CriticMode));
            float[] moveLogits = new float[moves.Length];
            for (int i = 0; i < moves.Length; i++)
            {
                moveLogits[i] = logits[moves[i].Index];
            }
            float[] moveProbs = Utils.Softmax(moveLogits, t: _temperature);

            IMove[] prunedMoves = new IMove[moves.Length];
            float[] prunedProbs = new float[moves.Length];
            int idx = 0;
            for (int i = 0; i < moves.Length; i++)
            {
                if (moveProbs[i] < _probThreshold)
                    continue;
                prunedMoves[idx] = moves[i];
                prunedProbs[idx] = -moveProbs[i]; // so that the sorting is descending
                idx++;
            }
            Array.Resize(ref prunedMoves, idx);
            Array.Resize(ref prunedProbs, idx);

            Array.Sort(prunedProbs, prunedMoves);
            return prunedMoves;
        }

    }
}