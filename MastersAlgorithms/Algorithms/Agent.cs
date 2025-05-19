using MastersAlgorithms.ActorCritic;
using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Agent : IAlgorithm
    {
        public ActorCriticPolicy Policy;
        public ObservationMode ActorMode;
        public ObservationMode CriticMode;
        public bool Deterministic;
        public string Device;
        public int ExpectedBatchCount;
        private IGame? _game;
        private bool _verbose;

        public Agent(
            string modelDirectory,
            ObservationMode actorMode = ObservationMode.FLAT,
            ObservationMode criticMode = ObservationMode.FLAT,
            bool deterministic = true,
            string device = "cpu",
            int expectedBatchCount = 1,
            bool verbose = false)
        {
            Device = device;
            ExpectedBatchCount = expectedBatchCount;
            Policy = new ActorCriticPolicy(modelDirectory, Device, ExpectedBatchCount);
            ActorMode = actorMode;
            Deterministic = deterministic;
            _verbose = verbose;
        }

        public string GetDebugInfo()
        {
            float value = Policy.GetValue(_game!.GetObservation(CriticMode))[0];
            return string.Format("Eval {0,5:F3}", value);
        }

        public IMove? GetMove(IGame game)
        {
            _game = game;
            if (_verbose)
                Console.WriteLine(GetDebugInfo());
            if (Deterministic)
                return GetDeterministicMove(game);
            else
                return GetStochasticMove(game);
        }

        public IMove GetStochasticMove(IGame game)
        {
            var actionMasks = game.GetActionMasks(out IMove[] moves);
            var probs = Policy.GetMaskedProbs(game.GetObservation(ActorMode), actionMasks);
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
            var probs = Policy.GetMaskedProbs(game.GetObservation(ActorMode), actionMasks);
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