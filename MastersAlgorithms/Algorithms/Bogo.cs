using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Bogo : IAlgorithm
    {
        public IMove GetMove(IGame game)
        {
            return game.GetRandomMove();
        }

        public string GetDebugInfo()
        {
            return "Bogo Debug Not Implemented";
        }
    }
}