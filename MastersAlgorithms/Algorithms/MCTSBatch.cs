
using System.Diagnostics;
using MastersAlgorithms.Games;

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

            public void UpdateChildrenPriors(float[] probs, IMove[] moves)
            {
                int arrayIndex = 0;
                foreach (var move in moves)
                {
                    Children![arrayIndex++].PriorProbabiltity = probs[move.Index];
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

            public Node GetChildByMoveIndex(int moveIdx, IMove[]? moves = null)
            {
                if (moves == null)
                    moves = Game.GetMoves();

                int arrayIndex = 0;
                for (; arrayIndex < moves.Length; arrayIndex++)
                    if (moves[arrayIndex].Index == moveIdx)
                        break;

                return Children![arrayIndex];
            }

        }

        private long _nodes;
        private Node? _root;
        private Stopwatch _sw;
        private float _time;
        private float _value;
        private float[]? _rootValueArray;
        private Node? _bestChild;
        private int _lastMoveCounter = int.MaxValue;
        private int _initialRootVisits;

        readonly int _maxIters;
        readonly Func<Node[], Node> _simulationPolicy;
        readonly Func<IGame[], (float[][] probs, IMove[][] moves, float[] values)> _probsAndValueFunc;
        readonly int _batchSize;
        readonly float _lambda;
        readonly float _noiseAlpha;
        readonly float _noiseWeight;
        readonly int _nVirtual;
        readonly bool _preserveSubtree;
        readonly bool _verbose;

        public MCTSBatch(
            int maxIters,
            Func<Node[], Node> simulationPolicy,
            Func<IGame[], (float[][], IMove[][], float[])> probsAndValueFunc,
            int batchSize = 1,
            float lambda = 0.5f,
            float noiseAlpha = 0.9f,
            float noiseWeight = 0.25f,
            int nVirtual = 5,
            bool preserveSubtree = false,
            bool verbose = false
        )
        {
            _sw = new Stopwatch();
            _maxIters = maxIters;
            _simulationPolicy = simulationPolicy;
            _probsAndValueFunc = probsAndValueFunc;
            _batchSize = batchSize;
            _lambda = lambda;
            _noiseAlpha = noiseAlpha;
            _noiseWeight = noiseWeight;
            _nVirtual = nVirtual;
            _preserveSubtree = preserveSubtree;
            _verbose = verbose;
        }

        public string GetDebugInfo()
        {
            return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | ValueSum {3,8:F2} | VisitCount {4,4}"
                                + " | RVisits {5,5} | Root Eval {6,6:F3} | Eval {7,6:F3}",
                _nodes, _nodes / _time, _time, _bestChild!.ValueSum, _bestChild!.VisitCount,
                _initialRootVisits, _rootValueArray![0], _value);
        }

        public IMove? GetMove(IGame game)
        {
            _nodes = 0;
            _sw.Restart();

            // zobrist is not used with MCTS but is computationally expensive
            // so disable it
            game = game.Copy(disableZobrist: true);

            InitialiseRoot(game);
            _lastMoveCounter = game.MoveCounter;
            _initialRootVisits = _root!.VisitCount;

            for (int i = 0; i < _maxIters; i++)
            {
                BuildTree();
            }

            int bestIdx = _root!.GetMostVisitedChildIdx();
            _bestChild = _root.Children![bestIdx];
            _value = _bestChild.ValueSum / _bestChild.VisitCount;

            _time = _sw.ElapsedMilliseconds;
            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            IMove[] moves = game.GetMoves();
            IMove bestMove = moves[bestIdx];
            if (_preserveSubtree)
                _root = _root.GetChildByMoveIndex(bestMove.Index, moves);
            return bestMove;
        }

        private void InitialiseRoot(IGame game)
        {
            // TODO WARNING _lastMoveCounter > game.MoveCounter
            // assumes that a game will never start from the same (or larger) moveCounter state
            // then the previous game finished. With large enough n_random_moves this *could*
            // be the case. Possible fix -- reset move counter after game generation and count
            // only real moves
            if (_root == null || !_preserveSubtree || _lastMoveCounter > game.MoveCounter)
                _root = new Node(game);
            else
            {
                _root = _root.GetChildByMoveIndex(game.LastMove!.Index);
                _root.Parent = null;
            }


            (var probs, var moves, _rootValueArray) = _probsAndValueFunc([_root.Game]);
            var actionMasks = game.GetActionMasks(out _);
            var noisyProbs = Utils.AddDirichletNoise(probs[0], _noiseAlpha, _noiseWeight, actionMasks);

            if (!_preserveSubtree || _root.Children == null)
                _root.Expand(noisyProbs, moves[0]);
            else
                _root.UpdateChildrenPriors(noisyProbs, moves[0]);
            // _root.VisitCount++;
        }

        private void BuildTree()
        {
            Node[] currentBatch = Select();
            var stateBatch = GetStateBatch(currentBatch);
            var output = _probsAndValueFunc(stateBatch);

            Expand(currentBatch, output.probs, output.moves);
            var valueBatch = Evaluate(currentBatch, output.values);
            Backtrack(currentBatch, valueBatch);
        }

        private Node[] Select()
        {
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
                    current.ValueSum -= _nVirtual;
                    if (current.IsTerminal)
                        break;
                }
                currentBatch[i] = current;
            }
            return currentBatch;
        }

        private void Expand(Node[] nodeBatch, float[][] priorsBatch, IMove[][] movesBatch)
        {
            for (int i = 0; i < _batchSize; i++)
            {
                if (nodeBatch[i].IsTerminal)
                    continue;
                nodeBatch[i].Expand(priorsBatch[i], movesBatch[i]);
            }
        }

        private float[] Evaluate(Node[] nodeBatch, float[] valueEstimates)
        {
            float[] rolloutValues = new float[nodeBatch.Length];
            for (int i = 0; i < _batchSize; i++)
            {
                if (_lambda == 0 && !nodeBatch[i].IsTerminal)
                    continue;
                IGame terminalState = Rollout(nodeBatch[i].Game.Copy());
                float rolloutValue = terminalState.Evaluate();
                if (terminalState.Player == nodeBatch[i].Game.Player)
                    rolloutValue = -rolloutValue;
                rolloutValues[i] = MathF.Sign(rolloutValue);
            }

            float[] values = new float[nodeBatch.Length];
            for (int i = 0; i < _batchSize; i++)
            {
                if (nodeBatch[i].IsTerminal)
                    values[i] = rolloutValues[i];
                else
                    values[i] = (1.0f - _lambda) * -valueEstimates[i] + _lambda * rolloutValues[i];
            }
            return values;
        }

        private IGame Rollout(IGame game)
        {
            while (!game.IsOver)
            {
                _nodes++;
                IMove move = game.GetRandomMove();
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
                    current.ValueSum += _nVirtual;

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
            int batchSize = 1,
            float lambda = 0.5f,
            float noiseAlpha = 0.9f,
            float noiseWeight = 0.25f,
            int nVirtual = 5,
            bool preserveSubtree = false,
            float c = 5f,
            float temperature = 10.0f,
            bool deterministicSelection = true,
            bool verbose = false)
        {
            AgentControllerBatch ac = new AgentControllerBatch(agent, c, temperature, deterministicSelection);
            return new MCTSBatch(
                maxIters: maxIters,
                simulationPolicy: ac.SimulationPolicy,
                probsAndValueFunc: ac.ProbsAndValueFunc,
                batchSize: batchSize,
                lambda: lambda,
                noiseAlpha: noiseAlpha,
                noiseWeight: noiseWeight,
                nVirtual: nVirtual,
                preserveSubtree: preserveSubtree,
                verbose: verbose
            );
        }

    }

    public class AgentControllerBatch
    {
        private Agent _agent;
        private float _c;
        private float _temperature;
        private bool _deterministicSelection;

        public AgentControllerBatch(
            Agent agent,
            float c = 5.0f,
            float temperature = 10.0f,
            bool deterministicSelection = true)
        {
            _agent = agent;
            _c = c;
            _temperature = temperature;
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

        public (float[][] probs, IMove[][] moves, float[] values) ProbsAndValueFunc(IGame[] states)
        {
            int stateCount = states.Length;
            int nPossibleMoves = states[0].PossibleMovesCount;

            IMove[][] moves = new IMove[stateCount][];
            bool[] actionMasks = new bool[nPossibleMoves * stateCount];
            for (int i = 0; i < stateCount; i++)
            {
                bool[] currentActionMasks = states[i].GetActionMasks(out moves[i]);
                Array.Copy(currentActionMasks, 0, actionMasks, i * nPossibleMoves, nPossibleMoves);
            }

            float[] obs = Utils.GetFlatObservations(states, _agent.ActorMode);
            var output = _agent.Policy.GetMaskedProbsAndValues(obs, actionMasks, batchCount: stateCount, t: _temperature);

            float[][] probs = new float[stateCount][];
            for (int i = 0; i < stateCount; i++)
            {
                probs[i] = new float[nPossibleMoves];
                Array.Copy(output.probs, i * nPossibleMoves, probs[i], 0, nPossibleMoves);
            }

            return (probs, moves, output.values);
        }

        private float GetNodeQU(MCTSBatch.Node node)
        {
            float Q = node.ValueSum / (node.VisitCount + 1);
            float U = _c * node.PriorProbabiltity * MathF.Sqrt(node.Parent!.VisitCount) / (1 + node.VisitCount);
            return Q + U;
        }

    }
}