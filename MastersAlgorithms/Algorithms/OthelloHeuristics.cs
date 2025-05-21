using System.Numerics;
using MastersAlgorithms.Games;
using MathNet.Numerics.Random;

namespace MastersAlgorithms.Algorithms
{
    public abstract class OthelloHeuristic : IAlgorithm
    {
        protected static readonly int BOARD_SIZE = 8;
        protected static readonly float ENDGAME_THRESHOLD = 0.8f;
        protected static readonly ulong CORNER_MASK = (0x1UL) | (0x1UL << 7) | (0x1UL << 56) | (0x1UL << 63);
        protected float _bestEvaluation;
        private bool _verbose;

        public abstract string GetDebugInfo();

        public OthelloHeuristic(bool verbose)
        {
            _verbose = verbose;
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

        private bool CheckEndGame(IGame game)
        {
            ulong occupiedMask = ~(game as BitOthello)!.EmptyMask;
            if ((float)BitOperations.PopCount(occupiedMask) / (BOARD_SIZE * BOARD_SIZE) >= ENDGAME_THRESHOLD)
                return true;

            if ((occupiedMask & CORNER_MASK) == CORNER_MASK)
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

        public override string GetDebugInfo()
        {
            return string.Format("Eval {0,5:F3}", _bestEvaluation);
        }

        protected override float GetBaseEvaluation(IGame game)
        {
            return game.Evaluate(); // default BitOthello evaluation is the same as positional player's
        }
    }

    public class MobilityHeuristic : OthelloHeuristic
    {

        private static readonly float W1 = 10;
        private static readonly float W2 = 1;

        public MobilityHeuristic(bool verbose = false) : base(verbose) { }

        public override string GetDebugInfo()
        {
            return "MobilityHeuristic debug not implemented";
        }

        protected override float GetBaseEvaluation(IGame _game)
        {
            var game = (_game as BitOthello)!;
            float cornerDiff = GetCornerDiff(game);

            float mp = game.GetMoves().Length;

            game.SwitchPlayers();
            float mo = game.GetMoves().Length;
            game.SwitchPlayers();

            float mobilityFactor = (mp - mo) / (mp + mo);

            return W1 * cornerDiff + W2 * mobilityFactor;
        }

        private float GetCornerDiff(BitOthello game)
        {
            int playerCorners = BitOperations.PopCount(game.PlayerBoard & CORNER_MASK);
            int opponentCorners = BitOperations.PopCount(game.OpponentBoard & CORNER_MASK);
            return playerCorners - opponentCorners;
        }
    }
}