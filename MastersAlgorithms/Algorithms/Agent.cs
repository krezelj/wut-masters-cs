using MastersAlgorithms.ActorCritic;
using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Agent : IAlgorithm
    {
        public ActorCriticPolicy Policy;
        public ObservationMode Mode;
        public bool Deterministic;

        public Agent(string modelDirectory, ObservationMode mode, bool deterministic)
        {
            Policy = new ActorCriticPolicy(modelDirectory);
            Mode = mode;
            Deterministic = deterministic;
        }

        public string GetDebugInfo()
        {
            return "Agent DebugInfo not implemented yet";
        }

        public IMove? GetMove(IGame game)
        {
            if (Deterministic)
                return GetDeterministicMove(game);
            else
                return GetStochasticMove(game);
        }

        public IMove GetStochasticMove(IGame game)
        {
            var actionMasks = game.GetActionMasks(out IMove[] moves);
            var probs = Policy.GetMaskedProbs(game.GetObservation(Mode), actionMasks);
            var sampledIndex = Utils.Sample(probs);
            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i].Index == sampledIndex)
                {
                    return moves[i];
                }
            }
            throw new Exception($"Sampled index {sampledIndex} not in valid moves!");
        }

        public IMove GetDeterministicMove(IGame game)
        {
            var actionMasks = game.GetActionMasks(out IMove[] moves);
            var probs = Policy.GetMaskedProbs(game.GetObservation(Mode), actionMasks);
            int idxMax = Utils.ArgMax(probs);
            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i].Index == idxMax)
                {
                    return moves[i];
                }
            }
            throw new Exception($"Sampled index {idxMax} not in valid moves!");
        }
    }
}