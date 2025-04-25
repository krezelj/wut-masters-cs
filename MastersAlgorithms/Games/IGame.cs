namespace MastersAlgorithms.Games
{
    public interface IGame
    {
        public int Player { get; }

        bool IsOver { get; }

        public ulong zKey { get; }

        IMove[] GetMoves();

        IMove GetRandomMove();

        void MakeMove(IMove move, bool updateMove = true);

        void UndoMove(IMove move);

        float Evaluate();

        IGame Copy();

        void Display(bool showMoves = true);

        IMove GetMoveFromString(string m);
    }
}