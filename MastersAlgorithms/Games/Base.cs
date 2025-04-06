namespace MastersAlgorithms.Games
{
    public class OthelloMove(int i, int j, int nullMoves)
    {
        public int I => i;
        public int J => j;
        public List<(int, int)>? Captures;
        public int NullMoves => nullMoves;
        public bool IsNull => I < 0;

        public static OthelloMove NullMove()
        {
            return new OthelloMove(-1, -1, 0);
        }
    }


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