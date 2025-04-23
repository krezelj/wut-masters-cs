using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public interface IAlgorithm
    {
        IMove? GetMove(IGame game);

        string GetDebugInfo();
    }
}