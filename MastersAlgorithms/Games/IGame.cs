namespace MastersAlgorithms.Games
{
    public enum ObservationMode { FLAT, IMAGE };

    public interface IGame
    {
        static virtual int PossibleResultsCount { get; }

        public int PossibleMovesCount { get; }
        public int Player { get; }

        public IMove? LastMove { get; }
        public int MoveCounter { get; }

        bool IsOver { get; }

        public int Result { get; }

        public ulong zKey { get; }

        IMove[] GetMoves();

        IMove GetRandomMove();

        IMove[] SortMoves(IMove[] moves, int moveIndex);

        void MakeMove(IMove move, bool updateMove = true);

        void UndoMove(IMove move);

        float NaiveEvaluate();
        float MobilityEvaluate();
        float RandomEvaluate();
        float Evaluate();

        IGame Copy(bool disableZobrist = false);

        float[] GetObservation(ObservationMode mode = ObservationMode.FLAT);

        bool[] GetActionMasks(out IMove[] moves);

        void Display(bool showMoves = true);

        IMove GetMoveFromString(string m);

        IMove GetMoveFromAction(int action);
    }
}