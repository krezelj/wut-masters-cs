using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public interface IAlgorithm<T>
    {
        T? GetMove(IGame<T> game);
    }
}