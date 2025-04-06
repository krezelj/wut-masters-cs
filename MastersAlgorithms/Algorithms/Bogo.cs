using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Bogo<T>
    {
        public T GetMove(IGame<T> game)
        {
            return game.GetRandomMove();
        }
    }
}