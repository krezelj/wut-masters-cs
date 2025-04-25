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

        public MCTS(int maxIters, Func<Node, float> estimator, bool verbose = false)
        {
            _root = null;
            _maxIters = maxIters;
            _estimator = estimator;
            _verbose = verbose;
        }

        public IMove GetMove(IGame game)
        {
            var sw = new Stopwatch();
            sw.Start();

            _root = new Node(game, null);
            _root.Expand();
            BuildTree();

            int bestIdx = _root.GetBestChildIndex((Node n) => n.VisitCount);
            float value = _root.Children![bestIdx].ValueSum / _root.Children[bestIdx].VisitCount;
            if (_verbose)
                Console.WriteLine($"Nodes: {_nodes} ({_nodes / (float)sw.ElapsedMilliseconds} kN/s)\tEvaluation: {value}\t");

            _nodes = 0;
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
            throw new NotImplementedException();
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