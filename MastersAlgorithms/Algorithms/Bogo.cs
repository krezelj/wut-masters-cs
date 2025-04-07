using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Bogo<T> : IAlgorithm<T>
    {
        public T GetMove(IGame<T> game)
        {
            return game.GetRandomMove();
        }
    }
}