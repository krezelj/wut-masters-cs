using System.Diagnostics;
using MastersAlgorithms.ActorCritic;
using MastersAlgorithms.Games;
using Microsoft.ML.OnnxRuntime;

namespace MastersAlgorithms.Algorithms
{


    public class MCTSHybrid : IAlgorithm
    {
        public class Node
        {
            public IGame Game;
            public Node? Parent;
            public Node[]? Children;
            public bool Expanded;
            public bool IsTerminal => Game.IsOver;

            public int VisitCount;
            public float ValueSum;

            public float PriorProbability;

            public Node(IGame game, Node? parent, float priorProbability)
            {
                Game = game;
                Parent = parent;
                Children = null;
                Expanded = false;
                VisitCount = 0;
                ValueSum = 0;
                PriorProbability = priorProbability;
            }

            public void Expand(Func<IGame, (float[], IMove[])> priorFunc)
            {
                Expanded = true;
                (float[] probs, IMove[] moves) = priorFunc(Game);

                Children = new Node[moves.Length];
                int populated = 0;
                foreach (var move in moves)
                {
                    IGame newGame = Game.Copy();
                    newGame.MakeMove(move, false);
                    Children[populated++] = new Node(newGame, this, probs[move.Index]);
                }
            }

            public Node GetBestChild(Func<Node, float> estimator)
            {
                int bestIdx = GetBestChildIndex(estimator);
                return Children![bestIdx];
            }

            public int GetBestChildIndex(Func<Node, float> estimator)
            {
                float maxValue = float.MinValue;
                int bestIdx = 0;
                for (int i = 0; i < Children!.Length; i++)
                {
                    float value = estimator(Children[i]);
                    if (value > maxValue)
                    {
                        maxValue = value;
                        bestIdx = i;
                    }
                }
                return bestIdx;
            }
        }

        private long _nodes;
        private Node? _root;
        private int _maxIters;

        private bool _verbose;

        private Stopwatch _sw;
        private float _time;
        private float _value;
        private Node? _bestChild;

        private readonly Func<Node, float> _simulationPolicy;
        private readonly Func<IGame, (float[], IMove[])> _priorFunc;
        private readonly Func<IGame, IMove> _rolloutPolicy;
        private readonly Func<IGame, float> _valueEstimator;
        private readonly float _lambda;

        public MCTSHybrid(
            int maxIters,
            Func<Node, float> simulationPolicy,
            Func<IGame, (float[], IMove[])> priorFunc,
            Func<IGame, IMove> rolloutPolicy,
            Func<IGame, float> valueEstimator,
            float lambda,
            bool verbose = false)
        {
            _sw = new Stopwatch();
            _root = null;
            _maxIters = maxIters;
            _simulationPolicy = simulationPolicy;
            _priorFunc = priorFunc;
            _rolloutPolicy = rolloutPolicy;
            _valueEstimator = valueEstimator;
            _lambda = lambda;
            _verbose = verbose;
        }

        public IMove GetMove(IGame game)
        {
            _nodes = 0;

            _sw.Restart();

            // zobrist is not used with MCTS but is computationally expensive
            // so disable it
            game = game.Copy(disableZobrist: true);
            _root = new Node(game, null, 0);
            _root.Expand(_priorFunc);
            BuildTree();

            int bestIdx = _root.GetBestChildIndex(n => n.VisitCount);
            _bestChild = _root.Children![bestIdx];
            _value = _bestChild.ValueSum / _bestChild.VisitCount;

            _time = _sw.ElapsedMilliseconds;
            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            return game.GetMoves()[bestIdx];
        }

        private void BuildTree()
        {
            for (int iter = 0; iter < _maxIters; iter++)
            {
                // select
                Node? current = Select();
                if (current == null)
                    throw new Exception("Selected node is null!");

                // expand
                if (current.VisitCount == 2 && !current.IsTerminal)
                {
                    current.Expand(_priorFunc);
                    // TODO shouldn't this be just random?
                    // unless we use game evaluation
                    current = current.GetBestChild(_simulationPolicy);
                    current.VisitCount++;
                }

                // rollout
                float leafValue = EvaluateLeaf(current);

                // backtrack
                Backtrack(current, leafValue);
            }
        }

        private Node Select()
        {
            Node current = _root!;
            while (current.Expanded)
            {
                _nodes++;
                current.VisitCount++;
                current = current.GetBestChild(_simulationPolicy);
                if (current.IsTerminal)
                    break;
            }
            current.VisitCount++;
            return current;
        }

        private float EvaluateLeaf(Node leaf)
        {
            float rolloutValue;
            if (_lambda == 0)
            {
                rolloutValue = 0.0f;
            }
            else
            {
                IGame terminalState = Rollout(leaf.Game.Copy());
                rolloutValue = terminalState.Evaluate();
                if (terminalState.Player == leaf.Game.Player)
                    rolloutValue = -rolloutValue;
                rolloutValue = MathF.Sign(rolloutValue);
            }

            float estimatorValue;
            if (_lambda == 1.0f)
            {
                estimatorValue = 0.0f;
            }
            else
            {
                estimatorValue = -_valueEstimator(leaf.Game);
            }
            float value = (1.0f - _lambda) * estimatorValue + _lambda * rolloutValue;
            return value;
        }

        private IGame Rollout(IGame game)
        {
            while (!game.IsOver)
            {
                _nodes++;
                // IMove move = game.GetRandomMove();
                IMove move = _rolloutPolicy(game);
                game.MakeMove(move, false);
            }
            return game;
        }

        private void Backtrack(Node? current, float value)
        {
            while (current != null)
            {
                current.ValueSum += value;
                value = -value;
                current = current.Parent;
            }
        }

        public string GetDebugInfo()
        {
            return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | ValueSum {3,6:F3} | VisitCount {4,6} | Eval {5}",
                _nodes, _nodes / _time, _time, _bestChild!.ValueSum, _bestChild!.VisitCount, _value);
        }

        #region ESTIMATORS

        public static float UCB(Node node)
        {
            if (node.VisitCount == 0)
                return float.MaxValue;

            float ucb = node.ValueSum / node.VisitCount + MathF.Sqrt(2 * MathF.Log(node!.Parent!.VisitCount) / node.VisitCount);
            return ucb;
        }

        public static IMove RandomRollout(IGame game)
        {
            return game.GetRandomMove();
        }

        public static MCTSHybrid GetDefaultMCTS(int maxIters, bool verbose = false)
        {
            return new MCTSHybrid(
                maxIters: maxIters,
                simulationPolicy: UCB,
                priorFunc: g =>
                {
                    var moves = g.GetMoves();
                    return (new float[g.PossibleMovesCount], moves);
                },
                rolloutPolicy: RandomRollout,
                valueEstimator: g => 0.0f,
                lambda: 1.0f,
                verbose: verbose
            );
        }

        public static MCTSHybrid GetAgentMCTS(
            int maxIters,
            Agent agent,
            float lambda = 0.5f,
            float c = 5f,
            bool useRandomRollout = false,
            bool verbose = false)
        {
            AgentController ac = new AgentController(agent, c);
            return new MCTSHybrid(
                maxIters: maxIters,
                simulationPolicy: ac.SimulationPolicy,
                priorFunc: ac.PriorFunc,
                rolloutPolicy: useRandomRollout ? RandomRollout : ac.RolloutPolicy,
                valueEstimator: ac.ValueEstimator,
                lambda: lambda,
                verbose: verbose
            );
        }

        #endregion

    }

    public class AgentController
    {
        private Agent _agent;
        private float _c;

        public AgentController(Agent agent, float c = 5.0f)
        {
            _agent = agent;
            _c = c;
        }

        public float SimulationPolicy(MCTSHybrid.Node node)
        {
            float Q = node.ValueSum / (node.VisitCount + 1);
            float U = _c * node.PriorProbability * MathF.Sqrt(node.Parent!.VisitCount) / (1 + node.VisitCount);
            return Q + U;
        }

        public (float[], IMove[]) PriorFunc(IGame game)
        {
            var actionMasks = game.GetActionMasks(out IMove[] moves);
            return (_agent.Policy.GetMaskedProbs(game.GetObservation(_agent.ActorMode), actionMasks), moves);
        }

        public IMove RolloutPolicy(IGame game)
        {
            return _agent.GetStochasticMove(game);
        }

        public float ValueEstimator(IGame game)
        {
            return _agent.Policy.GetValue(game.GetObservation(_agent.CriticMode))[0];
        }
    }
}