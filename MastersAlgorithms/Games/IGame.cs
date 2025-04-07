namespace MastersAlgorithms.Games
{
    public interface IGame
    {
        public int Player { get; }

        List<IMove> GetMoves();

        IMove GetRandomMove();

        void MakeMove(IMove move, bool updateMove = true);

        void UndoMove(IMove move);

        float Evaluate();

        bool IsOver { get; }

        IGame Copy();
    }
}