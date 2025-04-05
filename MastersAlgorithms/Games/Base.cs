namespace MastersAlgorithms.Games
{
    public struct OthelloMove(int i, int j, List<(int, int)>? captures, int nullMoves)
    {
        public int I => i;
        public int J => j;
        public List<(int, int)>? Captures => captures;
        public int NullMoves => nullMoves;
        public bool IsNull => I < 0;

        public static OthelloMove NullMove()
        {
            return new OthelloMove(-1, -1, null, 0);
        }
    }


    public interface IGame<T>
    {
        List<T> GetMoves();

        T GetRandomMove();

        void MakeMove(T move);

        void UndoMove(T move);

        bool IsOver { get; }
    }
}