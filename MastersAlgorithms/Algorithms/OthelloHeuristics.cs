using System.Numerics;
using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public abstract class OthelloHeuristic : IAlgorithm
    {
        protected static readonly float ENDGAME_THRESHOLD = 0.8f;
        protected float _bestEvaluation;
        private bool _verbose;

        public OthelloHeuristic(bool verbose)
        {
            _verbose = verbose;
        }

        public virtual string GetDebugInfo()
        {
            return string.Format("Eval {0,5:F3}", _bestEvaluation);
        }

        public virtual IMove? GetMove(IGame game)
        {
            IMove? bestMove = null;
            _bestEvaluation = float.MinValue;
            foreach (IMove move in game.GetMoves())
            {
                game.MakeMove(move);
                float evaluation = -GetEvaluation(game);
                game.UndoMove(move);

                if (evaluation > _bestEvaluation)
                {
                    _bestEvaluation = evaluation;
                    bestMove = move;
                }
            }

            if (_verbose)
                Console.WriteLine(GetDebugInfo());
            return bestMove;
        }

        protected virtual float GetEvaluation(IGame game)
        {
            if (CheckEndGame(game))
                return (game as BitOthello)!.MaterialDiff;
            return GetBaseEvaluation(game);
        }

        private bool CheckEndGame(IGame _game)
        {
            var game = (_game as BitOthello)!;
            ulong occupiedMask = ~game!.EmptyMask;
            if ((float)BitOperations.PopCount(occupiedMask) / (BitOthello.BOARD_SIZE * BitOthello.BOARD_SIZE) >= ENDGAME_THRESHOLD)
                return true;

            if ((occupiedMask & BitOthello.CORNER_MASK) == BitOthello.CORNER_MASK)
                return true; // all corners occupied
            return false;
        }

        protected abstract float GetBaseEvaluation(IGame game);
    }

    ///
    /// Implemented according to https://doi.org/10.1016/j.cor.2006.10.004
    /// 
    public class PositionalHeuristic : OthelloHeuristic
    {

        public PositionalHeuristic(bool verbose = false) : base(verbose) { }

        protected override float GetBaseEvaluation(IGame game)
        {
            return game.Evaluate(); // default BitOthello evaluation is the same as positional player's
        }
    }

    public class MobilityHeuristic : OthelloHeuristic
    {
        public MobilityHeuristic(bool verbose = false) : base(verbose) { }

        protected override float GetBaseEvaluation(IGame game)
        {
            return game.MobilityEvaluate();
        }
    }
}