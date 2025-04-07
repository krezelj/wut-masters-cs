namespace MastersAlgorithms.Games
{
    public interface IGame<T>
    {
        public int Player { get; }

        List<T> GetMoves();

        T GetRandomMove();

        void MakeMove(T move, bool updateMove = true);

        void UndoMove(T move);

        float Evaluate();

        bool IsOver { get; }

        IGame<T> Copy();
    }
}