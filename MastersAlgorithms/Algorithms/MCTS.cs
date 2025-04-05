using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{


    public class MCTS<T>
    {
        public class Node
        {
            public IGame<T> Game;
            public Node? Parent;
            public Node[]? Children;
            public bool Expanded;
            public bool IsTerminal => Game.IsOver;

            public int VisitCount;
            public float ValueSum;

            public Node(IGame<T> game, Node? parent = null)
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

                Children = new Node[moves.Count];
                int populated = 0;
                foreach (var move in moves)
                {
                    IGame<T> newGame = Game.Copy();
                    newGame.MakeMove(move);
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

        public MCTS(int maxIters, Func<Node, float> estimator)
        {
            _root = null;
            _maxIters = maxIters;
            _estimator = estimator;
        }

        public T GetMove(IGame<T> game)
        {
            _root = new Node(game, null);
            _root.Expand();

            BuildTree();

            return game.GetMoves()[_root.GetBestChildIndex((Node n) => n.VisitCount)];
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
                float value = Rollout(current.Game);

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

        private float Rollout(IGame<T> game)
        {
            while (!game.IsOver)
            {
                _nodes++;
                T move = game.GetRandomMove();
                game.MakeMove(move);
            }
            return game.Evaluate();
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

        #region ESTIMATORS
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