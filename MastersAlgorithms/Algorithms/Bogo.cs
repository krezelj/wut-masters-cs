using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Bogo : IAlgorithm
    {
        bool _verbose;
        IMove _lastMove = null!;

        public Bogo(bool verbose = false)
        {
            _verbose = verbose;
        }

        public IMove GetMove(IGame game)
        {
            _lastMove = game.GetRandomMove();
            if (_verbose)
                Console.WriteLine(GetDebugInfo());
            return _lastMove;
        }

        public string GetDebugInfo()
        {
            return $"Bogo plays {_lastMove.Index}";
        }
    }
}