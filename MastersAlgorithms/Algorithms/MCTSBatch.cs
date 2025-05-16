
using System.Data;
using System.Diagnostics;
using System.Transactions;
using MastersAlgorithms.Games;
using MathNet.Numerics.Distributions;

namespace MastersAlgorithms.Algorithms
{
    public class MCTSBatch : IAlgorithm
    {
        public class Node
        {
            public IGame Game;
            public Node? Parent;
            public Node[]? Children;
            public bool Expanded;
            public bool IsTerminal => Game!.IsOver;
            public bool IsRoot => Parent == null;
            public int VisitCount;
            public float ValueSum;
            public float PriorProbabiltity;

            public Node(IGame game, Node? parent = null, float priorProbabiltity = 0.0f)
            {
                Game = game;
                Parent = parent;
                PriorProbabiltity = priorProbabiltity;

                Expanded = false;
                VisitCount = 0;
                ValueSum = 0;
            }

            public void Expand(float[] probs, IMove[] moves)
            {
                Expanded = true;
                Children = new Node[moves.Length];
                int populated = 0;
                foreach (var move in moves)
                {
                    IGame newGame = Game.Copy();
                    newGame.MakeMove(move, false);
                    Children[populated++] = new Node(newGame, this, probs[move.Index]);
                }
            }

            public Node GetBestChild(Func<Node[], Node> policy)
            {
                return policy(Children!);
            }
            public int GetMostVisitedChildIdx()
            {
                int max = 0;
                int idx = -1;
                for (int i = 0; i < Children!.Length; i++)
                {
                    if (Children[i].VisitCount > max)
                    {
                        max = Children[i].VisitCount;
                        idx = i;
                    }
                }
                return idx;
            }
        }

        private long _nodes;
        private Node? _root;
        private Stopwatch _sw;
        private float _time;
        private float _value;
        private Node? _bestChild;

        readonly int _maxIters;
        readonly Func<Node[], Node> _simulationPolicy;
        readonly Func<IGame[], (float[][], IMove[][])> _priorFunc;
        readonly Func<IGame, IMove> _rolloutPolicy; // TODO make batched?
        readonly Func<IGame[], float[]> _valueEstimator;
        readonly int _batchSize;
        readonly float _lambda;
        readonly float _noiseAlpha;
        readonly float _noiseWeight;
        readonly int _nVirtual;
        readonly bool _verbose;

        public MCTSBatch(
            int maxIters,
            Func<Node[], Node> simulationPolicy,
            Func<IGame[], (float[][], IMove[][])> priorFunc,
            Func<IGame, IMove> rolloutPolicy,
            Func<IGame[], float[]> valueEstimator,
            int batchSize = 1,
            float lambda = 0.5f,
            float noiseAlpha = 0.9f,
            float noiseWeight = 0.25f,
            int nVirtual = 5,
            bool verbose = false
        )
        {
            _sw = new Stopwatch();
            _maxIters = maxIters;
            _simulationPolicy = simulationPolicy;
            _priorFunc = priorFunc;
            _rolloutPolicy = rolloutPolicy;
            _valueEstimator = valueEstimator;
            _batchSize = batchSize;
            _lambda = lambda;
            _noiseAlpha = noiseAlpha;
            _noiseWeight = noiseWeight;
            _nVirtual = nVirtual;
            _verbose = verbose;
        }

        public string GetDebugInfo()
        {
            return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | ValueSum {3,6:F3} | VisitCount {4,6} | Eval {5}",
                _nodes, _nodes / _time, _time, _bestChild!.ValueSum, _bestChild!.VisitCount, _value);
        }

        public IMove? GetMove(IGame game)
        {
            _nodes = 0;
            _sw.Restart();

            // zobrist is not used with MCTS but is computationally expensive
            // so disable it
            game = game.Copy(disableZobrist: true);

            _root = new Node(game);
            (var probs, var moves) = _priorFunc([_root.Game]);
            var actionMasks = game.GetActionMasks(out _);
            // _priors = Utils.AddDirichletNoise(_priors, _noiseAlpha, _noiseWeight, actionMasks);
            var noisyProbs = Utils.AddDirichletNoise(probs[0], _noiseAlpha, _noiseWeight, actionMasks);
            _root.Expand(noisyProbs, moves[0]);
            // _root.Expand(probs[0], moves[0]);
            _root.VisitCount++;

            for (int i = 0; i < _maxIters; i++)
            {
                BuildTree();
            }

            int bestIdx = _root.GetMostVisitedChildIdx();
            _bestChild = _root.Children![bestIdx];
            _value = _bestChild.ValueSum / _bestChild.VisitCount;

            _time = _sw.ElapsedMilliseconds;
            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            return game.GetMoves()[bestIdx];
        }

        private void BuildTree()
        {
            Node[] currentBatch = Select();
            var stateBatch = GetStateBatch(currentBatch);
            Expand(currentBatch, stateBatch);
            var valueBatch = Evaluate(currentBatch, stateBatch);
            Backtrack(currentBatch, valueBatch);
        }

        private Node[] Select()
        {
            // TODO Consider adding something like AlphaZero
            // When the node is selected add X loses to it
            // to discourage from it being selected in subsequent iterations
            // in backup, remove those loses
            Node[] currentBatch = new Node[_batchSize];
            for (int i = 0; i < _batchSize; i++)
            {
                Node current = _root!;
                current.VisitCount += _nVirtual;

                while (current.Expanded)
                {
                    _nodes++;
                    current = current.GetBestChild(_simulationPolicy);

                    // add virtual losses to discourage from exploring this node
                    current.VisitCount += _nVirtual;
                    if (current.IsTerminal)
                        break;
                }
                currentBatch[i] = current;
            }
            return currentBatch;
        }

        private void Expand(Node[] nodeBatch, IGame[] stateBatch)
        {
            (var priorsBatch, var movesBatch) = _priorFunc(stateBatch);
            for (int i = 0; i < _batchSize; i++)
            {
                if (nodeBatch[i].IsTerminal)
                    continue;
                nodeBatch[i].Expand(priorsBatch[i], movesBatch[i]);
            }
        }

        private float[] Evaluate(Node[] nodeBatch, IGame[] stateBatch)
        {
            float[] rolloutValues = new float[nodeBatch.Length];
            if (_lambda > 0)
            {
                for (int i = 0; i < _batchSize; i++)
                {
                    IGame terminalState = Rollout(nodeBatch[i].Game.Copy());
                    float rolloutValue = terminalState.Evaluate();
                    if (terminalState.Player == nodeBatch[i].Game.Player)
                        rolloutValue = -rolloutValue;
                    rolloutValues[i] = MathF.Sign(rolloutValue);
                }
            }

            float[] valueEstimates = new float[nodeBatch.Length];
            if (_lambda < 1.0f)
                valueEstimates = _valueEstimator(stateBatch);

            float[] values = new float[nodeBatch.Length];
            for (int i = 0; i < _batchSize; i++)
            {
                values[i] = (1.0f - _lambda) * -valueEstimates[i] + _lambda * rolloutValues[i];
            }
            return values;
            // // TODO include rollout
        }

        private IGame Rollout(IGame game)
        {
            while (!game.IsOver)
            {
                _nodes++;
                IMove move = game.GetRandomMove();
                // IMove move = _rolloutPolicy(game);
                game.MakeMove(move, false);
            }
            return game;
        }

        private void Backtrack(Node[] nodeBatch, float[] valueBatch)
        {
            for (int i = 0; i < _batchSize; i++)
            {
                Node? current = nodeBatch[i];
                float value = valueBatch[i];
                while (current != null)
                {
                    // remove virtual losses
                    current.VisitCount -= _nVirtual;

                    current.ValueSum += value;
                    current.VisitCount += 1;
                    value = -value;
                    current = current.Parent;
                }
            }
        }

        private IGame[] GetStateBatch(Node[] nodeBatch)
        {
            IGame[] stateBatch = new IGame[_batchSize];
            for (int i = 0; i < _batchSize; i++)
            {
                stateBatch[i] = nodeBatch[i].Game;
            }
            return stateBatch;
        }

        public static MCTSBatch GetAgentMCTS(
            int maxIters,
            Agent agent,
            float lambda = 0.5f,
            float c = 5f,
            bool deterministicSelection = true,
            bool verbose = false)
        {
            AgentControllerBatch ac = new AgentControllerBatch(agent, c, deterministicSelection);
            return new MCTSBatch(
                maxIters: maxIters,
                simulationPolicy: ac.SimulationPolicy,
                priorFunc: ac.PriorFunc,
                rolloutPolicy: ac.RolloutPolicy,
                valueEstimator: ac.ValueEstimator,
                lambda: lambda,
                verbose: verbose
            );
        }

    }

    public class AgentControllerBatch
    {
        private Agent _agent;
        private ObservationMode _mode;
        private float _c;
        private bool _deterministicSelection;

        public AgentControllerBatch(Agent agent, float c = 5.0f, bool deterministicSelection = true)
        {
            _agent = agent;
            _mode = _agent.Mode;
            _c = c;
            _deterministicSelection = deterministicSelection;
        }

        public MCTSBatch.Node SimulationPolicy(MCTSBatch.Node[] nodes)
        {
            int nodeCount = nodes.Length;
            float[] quValues = new float[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                quValues[i] = GetNodeQU(nodes[i]);
            }
            int idx;
            if (_deterministicSelection)
                idx = Utils.ArgMax(quValues);
            else
                idx = Utils.Sample(Utils.Softmax(quValues));
            return nodes[idx];
        }

        public (float[][], IMove[][]) PriorFunc(IGame[] states)
        {
            int stateCount = states.Length;
            int nPossibleMoves = states[0].PossibleMovesCount;

            float[] obs = Utils.GetFlatObservations(states, _mode);
            IMove[][] moves = new IMove[stateCount][];

            bool[] actionMasks = new bool[nPossibleMoves * stateCount];
            for (int i = 0; i < stateCount; i++)
            {
                bool[] currentActionMasks = states[i].GetActionMasks(out moves[i]);
                Array.Copy(currentActionMasks, 0, actionMasks, i * nPossibleMoves, nPossibleMoves);
            }

            var probsFlat = _agent.Policy.GetMaskedProbs(obs, actionMasks, batchCount: stateCount);
            float[][] probs = new float[stateCount][];
            for (int i = 0; i < stateCount; i++)
            {
                probs[i] = new float[nPossibleMoves];
                Array.Copy(probsFlat, i * nPossibleMoves, probs[i], 0, nPossibleMoves);
            }

            return (probs, moves);
        }

        public IMove RolloutPolicy(IGame game)
        {
            throw new NotImplementedException();
            // return _agent.GetStochasticMove(game);
        }

        public float[] ValueEstimator(IGame[] states)
        {
            int stateCount = states.Length;
            float[] obs = Utils.GetFlatObservations(states, _mode);
            return _agent.Policy.GetValue(obs, batchCount: stateCount);
        }

        private float GetNodeQU(MCTSBatch.Node node)
        {
            float Q = node.ValueSum / (node.VisitCount + 1);
            float U = _c * node.PriorProbabiltity * MathF.Sqrt(node.Parent!.VisitCount) / (1 + node.VisitCount);
            return Q + U;
        }

    }
}