using System.Diagnostics;
using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{


    public class MCTS : IAlgorithm
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

            public Node(IGame game, Node? parent = null)
            {
                Game = game;
                Parent = parent;
                Children = null;
                Expanded = false;
                VisitCount = 0;
                ValueSum = 0;
            }

            public void Expand()
            {
                Expanded = true;
                var moves = Game.GetMoves();

                Children = new Node[moves.Length];
                int populated = 0;
                foreach (var move in moves)
                {
                    IGame newGame = Game.Copy();
                    newGame.MakeMove(move, false);
                    Children[populated++] = new Node(newGame, this);
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
        private Func<Node, float> _estimator;

        private bool _verbose;

        private Stopwatch _sw;
        private float _time;
        private float _value;
        private Node? _bestChild;

        public MCTS(int maxIters, Func<Node, float> estimator, bool verbose = false)
        {
            _sw = new Stopwatch();
            _root = null;
            _maxIters = maxIters;
            _estimator = estimator;
            _verbose = verbose;
        }

        public IMove GetMove(IGame game)
        {
            _nodes = 0;

            _sw.Restart();

            // zobrist is not used with MCTS but is computationally expensive
            // so disable it
            game = game.Copy(disableZobrist: true);
            _root = new Node(game, null);
            _root.Expand();
            BuildTree();

            int bestIdx = _root.GetBestChildIndex((Node n) => n.VisitCount);
            _bestChild = _root.Children![bestIdx];
            _value = _bestChild.ValueSum / _bestChild.VisitCount;

            _time = _sw.ElapsedMilliseconds;
            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            return game.GetMoves()[bestIdx];
        }

        public void BuildTree()
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
                    current.Expand();
                    // TODO shouldn't this be just random?
                    // unless we use game evaluation
                    current = current.GetBestChild(_estimator);
                    current.VisitCount++;
                }

                // rollout
                IGame terminalState = Rollout(current.Game.Copy());
                float value = terminalState.Evaluate();
                if (terminalState.Player == current.Game.Player)
                    value = -value;
                value = MathF.Sign(value);

                // backtrack
                Backtrack(current, value);
            }
        }

        private Node Select()
        {
            Node current = _root!;
            while (current.Expanded)
            {
                _nodes++;
                current.VisitCount++;
                current = current.GetBestChild(_estimator);
            }
            current.VisitCount++;
            return current;
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
            return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | ValueSum {3,6} | VisitCount {4,6} | Eval {5}",
                _nodes, _nodes / _time, _time, _bestChild!.ValueSum, _bestChild!.VisitCount, _value);
        }

        #region ESTIMATORS

        public static Func<Node, float> GetEstimatorByName(string name)
        {
            switch (name)
            {
                case "ucb":
                    return UCB;
                default:
                    throw new ArgumentException($"Invalid estimator name: {name}");
            }
        }

        public static float UCB(Node node)
        {
            if (node.VisitCount == 0)
                return float.MaxValue;

            float ucb = node.ValueSum / node.VisitCount + MathF.Sqrt(2 * MathF.Log(node!.Parent!.VisitCount) / node.VisitCount);
            return ucb;
        }
        #endregion

    }
}