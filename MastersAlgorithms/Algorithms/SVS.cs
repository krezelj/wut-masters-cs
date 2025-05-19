using System.Diagnostics;
using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class SVS : IAlgorithm
    {
        long _nodes;
        Stopwatch _sw;
        IGame? _game;
        float _time;
        float _value;
        float _priorMax;
        int _priorMaxIdx;
        int _bestIdx;
        readonly bool _verbose;
        readonly int _rolloutsPerMove;
        readonly int _rolloutDepth;
        readonly float _noiseAlpha;
        readonly float _noiseWeight;
        readonly int _nVirtual;
        readonly Func<IGame, (float[], IMove[])> _priorFunc;
        readonly Func<IGame[], float[]> _valueEstimator;
        float[]? _priors;
        int _nRollouts;
        IGame[]? _children;
        IGame[]? _stateBuffer;
        int[]? _sampledIdxs;
        int[]? _visitCounts;
        float[]? _valueSums;

        public SVS(
            Func<IGame, (float[], IMove[])> priorFunc,
            Func<IGame[], float[]> valueEstimator,
            int rolloutsPerMove = 10,
            int rolloutDepth = 5,
            float noiseAlpha = 0.3f,
            float noiseWeight = 0.25f,
            int nVirtual = 0,
            bool verbose = false
        )
        {
            _sw = new Stopwatch();

            _priorFunc = priorFunc;
            _valueEstimator = valueEstimator;

            _rolloutsPerMove = rolloutsPerMove;
            _rolloutDepth = rolloutDepth;
            _noiseAlpha = noiseAlpha;
            _noiseWeight = noiseWeight;
            _nVirtual = nVirtual;
            _verbose = verbose;
        }

        public string GetDebugInfo()
        {
            var currentStateValue = _valueEstimator([_game!])[0];
            return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | PMax {3,4:F2}"
                + " | PMaxEval {4,5:F3} | PBest {5,4:F2} | NEval {6,5:F3} | Eval {7,5:F3}",
                _nodes, _nodes / _time, _time, _priorMax,
                _valueSums![_priorMaxIdx] / _visitCounts![_priorMaxIdx],
                _priors![_bestIdx],
                currentStateValue, _value);
        }

        public IMove? GetMove(IGame game)
        {
            _nodes = 0;
            _sw.Restart();

            _game = game.Copy(disableZobrist: true);

            (_priors, IMove[] moves) = _priorFunc(_game);
            if (moves.Length == 1)
                return moves[0];
            var actionMasks = _game.GetActionMasks(out _);
            _priors = Utils.AddDirichletNoise(_priors, _noiseAlpha, _noiseWeight, actionMasks);
            var _nonZeroPriors = new float[moves.Length];
            for (int i = 0, j = 0; i < _priors.Length; i++)
            {
                if (_priors[i] == 0)
                    continue;
                _nonZeroPriors[j++] = _priors[i];
            }
            _priors = _nonZeroPriors;
            _priorMax = _priors.Max();
            _priorMaxIdx = Utils.ArgMax(_priors);

            _valueSums = new float[moves.Length];
            _children = new IGame[moves.Length];
            _visitCounts = new int[moves.Length];
            for (int i = 0; i < moves.Length; i++)
            {
                _children[i] = _game.Copy();
                _children[i].MakeMove(moves[i], false);

                _visitCounts[i] = _nVirtual;
                _valueSums[i] = -_nVirtual;
            }

            _nRollouts = _rolloutsPerMove * moves.Length;
            _sampledIdxs = new int[_nRollouts];
            _stateBuffer = new IGame[_nRollouts];

            Search();
            CalculateValues();
            _bestIdx = GetBestMoveIdx();

            _time = _sw.ElapsedMilliseconds;
            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            return moves[_bestIdx];
        }

        private void Search()
        {
            int batchSize = 0;
            for (int i = 0; i < _nRollouts; i++)
            {
                int sampledIdx = Utils.Sample(_priors!);
                _visitCounts![sampledIdx]++;
                IGame leaf = Rollout(_children![sampledIdx].Copy());

                if (!leaf.IsOver)
                {
                    _sampledIdxs![i] = sampledIdx;
                    _stateBuffer![batchSize++] = leaf;
                }
                else
                {
                    float value = leaf.Evaluate();
                    if (leaf.Player != _game!.Player)
                        value = -value;
                    value = MathF.Sign(value);
                    _valueSums![sampledIdx] += value;
                }
            }
            Array.Resize(ref _stateBuffer, batchSize);
            Array.Resize(ref _sampledIdxs, batchSize);
        }

        private IGame Rollout(IGame game)
        {
            for (int i = 0; i < _rolloutDepth; i++)
            {
                _nodes++;
                if (game.IsOver)
                    break;

                IMove move = game.GetRandomMove();
                game.MakeMove(move, false);
            }
            return game;
        }

        private void CalculateValues()
        {
            if (_stateBuffer!.Length == 0)
                return;
            float[] values = _valueEstimator(_stateBuffer!);
            for (int i = 0; i < _sampledIdxs!.Length; i++)
            {
                int sampledIdx = _sampledIdxs[i];
                float multiplier = 1.0f;
                if (_stateBuffer[i].Player != _game!.Player)
                    multiplier = -1.0f;
                _valueSums![sampledIdx] += values[i] * multiplier;
            }
        }

        private int GetBestMoveIdx()
        {
            _value = float.MinValue;
            int bestIdx = -1;
            for (int i = 0; i < _valueSums!.Length; i++)
            {
                float value = _valueSums[i] / (_visitCounts![i] + 1);
                if (value > _value)
                {
                    _value = value;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        public static SVS GetAgentSVS(
            Agent agent,
            int rolloutsPerMove = 10,
            int rolloutDepth = 5,
            float noiseAlpha = 0.3f,
            float noiseWeight = 0.25f,
            int nVirtual = 0,
            bool verbose = false)
        {
            AgentControllerSVS ac = new AgentControllerSVS(agent);
            return new SVS(
                priorFunc: ac.PriorFunc,
                valueEstimator: ac.ValueEstimator,
                rolloutsPerMove: rolloutsPerMove,
                rolloutDepth: rolloutDepth,
                noiseAlpha: noiseAlpha,
                noiseWeight: noiseWeight,
                nVirtual: nVirtual,
                verbose: verbose
            );
        }

    }


    public class AgentControllerSVS
    {
        private Agent _agent;

        public AgentControllerSVS(Agent agent)
        {
            _agent = agent;
        }

        public (float[], IMove[]) PriorFunc(IGame state)
        {
            float[] obs = state.GetObservation(_agent.ActorMode);
            bool[] actionMasks = state.GetActionMasks(out IMove[] moves);
            float[] probs = _agent.Policy.GetMaskedProbs(obs, actionMasks);
            // float[] probs = new float[moves.Length];
            return (probs, moves);
        }

        public float[] ValueEstimator(IGame[] states)
        {
            int stateCount = states.Length;
            float[] obs = Utils.GetFlatObservations(states, _agent.CriticMode);
            return _agent.Policy.GetValue(obs, batchCount: stateCount);
        }
    }

}