using System.Numerics;

namespace MastersAlgorithms.Games
{

    public class BitOthelloMove(ulong position, sbyte nullMoves) : IMove
    {
        public const int NullIndex = 64;
        public int Index => IsNull ? NullIndex : position.Index();
        public ulong Position => position;
        public bool IsNull => position == 0;
        public sbyte NullMoves => nullMoves;
        public ulong FlipMask = 0;

        public static BitOthelloMove NullMove(sbyte nullMoves)
        {
            return new BitOthelloMove(0UL, nullMoves);
        }

        public string Algebraic()
        {
            if (IsNull)
                return "pa";

            char col = (char)('a' + Index % BitOthello.BOARD_SIZE);
            char row = (char)('1' + Index / BitOthello.BOARD_SIZE);
            return $"{col}{row}";
        }

        public static BitOthelloMove FromAlgebraic(string s)
        {
            // assume upper case for performance
            // s = s.ToLower();
            if (s == "PA")
                return NullMove(0);

            int index = (s[0] - 'A') + (s[1] - '1') * BitOthello.BOARD_SIZE;
            ulong position = 1UL << index;
            return new BitOthelloMove(position, 0);
        }

        public override string ToString()
        {
            return $"{Index},{nullMoves},{FlipMask}";
        }
    }

    public class BitOthello : IGame
    {
        public static int PossibleResultsCount => 3;
        public int PossibleMovesCount => BOARD_SIZE * BOARD_SIZE + 1;

        private static readonly ulong[] _weightMasks = {
            0b0000000000000000001111000011110000111100001111000000000000000000UL, // -1
            0b0000000000111100010000100100001001000010010000100011110000000000UL, // -2
            0b0001100000000000000000001000000110000001000000000000000000011000UL, // 5
            0b0010010000000000100000010000000000000000100000010000000000100100UL, // 10
            0b0100001010000001000000000000000000000000000000001000000101000010UL, // -20
            0b0000000001000010000000000000000000000000000000000100001000000000UL, // -50
            0b1000000100000000000000000000000000000000000000000000000010000001UL, // 100
        };
        private static readonly int[] _weightValues = {
            -1, -2, 5, 10, -20, -50, 100
        };

        public const sbyte BLACK = 0;
        public const sbyte WHITE = 1;
        private const sbyte EMPTY = 2;
        public const sbyte BOARD_SIZE = 8;
        public static readonly ulong CORNER_MASK = (0x1UL) | (0x1UL << 7) | (0x1UL << 56) | (0x1UL << 63);

        private sbyte _player = BLACK;
        public int Player => _player;
        private IMove? _lastMove = null;
        public IMove? LastMove => _lastMove;
        private int _moveCounter = 0;
        public int MoveCounter => _moveCounter;
        private sbyte _opponent => (sbyte)(1 - _player);

        public bool IsOver => EmptyMask == 0 || _nullMoves == 2;
        public int MaterialDiff
        {
            get
            {
                int blackCount = BitOperations.PopCount(_blackBoard);
                int whiteCount = BitOperations.PopCount(_whiteBoard);
                return blackCount - whiteCount;
            }
        }

        public int Result
        {
            get
            {
                if (!IsOver)
                    return -1;
                int materialDiff = MaterialDiff;
                if (materialDiff == 0)
                    return PossibleResultsCount - 1; // draw
                if (materialDiff > 0)
                    return BLACK; // black won
                return WHITE; // white won
            }
        }

        private ulong _blackBoard;
        private ulong _whiteBoard;

        public ref ulong PlayerBoard
        {
            get
            {
                if (_player == BLACK)
                    return ref _blackBoard;
                else
                    return ref _whiteBoard;
            }
        }
        public ref ulong OpponentBoard
        {
            get
            {
                if (_player == WHITE)
                    return ref _blackBoard;
                else
                    return ref _whiteBoard;
            }
        }

        public ulong EmptyMask => (~_whiteBoard) & (~_blackBoard);
        private sbyte _nullMoves;

        private bool _useZobrist;
        private ZobristHash? _zobristHash;
        public ulong zKey => _useZobrist ? _zobristHash!.Key : throw new Exception("ZobristHash is not used.");

        public BitOthello() : this(BLACK, true) { } // dummy parameterless constructor;

        public BitOthello(sbyte player = BLACK, bool useZobrist = true)
        {
            _player = player;

            _blackBoard = 0UL;
            _whiteBoard = 0UL;

            ulong center = BitBoard.GetPosition(BOARD_SIZE / 2 - 1, BOARD_SIZE / 2 - 1);
            _whiteBoard.SetPositions(center);
            _whiteBoard.SetPositions(center << 1 + BOARD_SIZE);

            _blackBoard.SetPositions(center << 1);
            _blackBoard.SetPositions(center << BOARD_SIZE);

            _useZobrist = useZobrist;
            if (_useZobrist)
            {
                _zobristHash = new ZobristHash(nTypes: 3, nPositions: BOARD_SIZE * BOARD_SIZE);
                SetZobristHash();
            }
            _nullMoves = 0;
        }

        public BitOthello(string state, sbyte player = BLACK, bool useZobrist = true) : this(player, useZobrist)
        {
            _blackBoard = 0UL;
            _whiteBoard = 0UL;
            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    if (state[i * BOARD_SIZE + j] == 'X')
                        _blackBoard.SetPositions(BitBoard.GetPosition(i, j));
                    if (state[i * BOARD_SIZE + j] == 'O')
                        _whiteBoard.SetPositions(BitBoard.GetPosition(i, j));
                }
            }
            _player = (sbyte)(state[^2] - '0');
            _nullMoves = (sbyte)(state[^1] - '0');
            if (_useZobrist)
            {
                SetZobristHash();
            }
        }

        private void SetZobristHash()
        {
            _zobristHash!.ResetKey();
            _zobristHash!.UpdatePosition(BLACK, _blackBoard);
            _zobristHash!.UpdatePosition(WHITE, _whiteBoard);
            _zobristHash!.UpdatePosition(EMPTY, EmptyMask);
            if (_player == WHITE)
                _zobristHash!.UpdatePlayer();
        }

        private bool IsCapturePossible(ulong position)
        {
            ulong opponentNeighbors = position.Expand8() & OpponentBoard;
            while (opponentNeighbors > 0)
            {
                ulong mask = opponentNeighbors.PopNextPosition();
                int offset = mask.Index() - position.Index();

                do
                {
                    mask.ShiftToNeighbor(offset);
                    if (!OpponentBoard.Contains(mask))
                        break;
                } while (mask > 0);

                if (PlayerBoard.Contains(mask))
                    return true;
            }
            return false;
        }

        public IMove[] GetMoves()
        {
            // empty positions next to opponent discs
            ulong perimeter = OpponentBoard.Expand8() & EmptyMask;
            ulong moveMask = 0UL;
            while (perimeter > 0)
            {
                ulong position = perimeter.PopNextPosition();
                if (IsCapturePossible(position))
                {
                    moveMask |= position;
                }
            }

            if (moveMask == 0)
                return [BitOthelloMove.NullMove(_nullMoves)];

            var moves = new IMove[BitOperations.PopCount(moveMask)];
            int i = 0;
            while (moveMask > 0)
            {
                moves[i++] = new BitOthelloMove(moveMask.PopNextPosition(), _nullMoves);
            }
            return moves;
        }

        public IMove GetRandomMove()
        {
            ulong perimeter = OpponentBoard.Expand8() & EmptyMask;

            while (perimeter > 0)
            {
                ulong mask = perimeter;
                int setBitCount = BitOperations.PopCount(mask);
                int randomIndex = Utils.RNG.Next(setBitCount);

                for (int i = 0; i <= randomIndex - 1; ++i)
                {
                    mask.PopNext();
                }

                ulong position = mask.PopNextPosition();
                if (IsCapturePossible(position))
                    return new BitOthelloMove(position, _nullMoves);
                perimeter.ClearPositions(position);
            }

            // all perimeter checks failed -- no valid move
            return BitOthelloMove.NullMove(_nullMoves);
        }

        public IMove[] SortMoves(IMove[] moves, int moveIndex)
        {
            var moveScores = new int[moves.Length];
            for (int i = 0; i < moves.Length; ++i)
            {
                if (moves[i].Index == moveIndex)
                {
                    moveScores[i] = -1_000_000;
                    continue;
                }
                for (int j = 0; j < _weightMasks.Length; ++j)
                {
                    ulong mask = (moves[i] as BitOthelloMove)!.Position & _weightMasks[j];
                    if (mask > 0)
                    {
                        moveScores[i] -= _weightValues[j];
                        break;
                    }
                }
            }

            Array.Sort(moveScores, moves);

            return moves;
        }

        public IMove GetMoveFromString(string m)
        {
            string[] data = m.Split(',');
            int index = int.Parse(data[0]);
            sbyte nullMoves = sbyte.Parse(data[1]);
            if (index == BitOthelloMove.NullIndex)
                return BitOthelloMove.NullMove(nullMoves);

            ulong position = 1UL << index;
            BitOthelloMove move = new BitOthelloMove(position, nullMoves);
            move.FlipMask = ulong.Parse(data[2]);
            return move;
        }

        public IMove GetMoveFromAction(int action)
        {
            if (action == BitOthelloMove.NullIndex)
                return BitOthelloMove.NullMove(_nullMoves);

            ulong position = 1UL << action;
            BitOthelloMove move = new BitOthelloMove(position, _nullMoves);
            return move;
        }

        private ulong GetCapturesFromPosition(ulong position)
        {
            ulong capturesMask = 0UL;

            ulong opponentNeighbors = position.Expand8() & OpponentBoard;
            while (opponentNeighbors > 0)
            {
                ulong mask = opponentNeighbors.PopNextPosition();
                ulong captureMask = mask;
                int offset = mask.Index() - position.Index();

                do
                {
                    mask.ShiftToNeighbor(offset);
                    if (!OpponentBoard.Contains(mask))
                        break;
                    captureMask |= mask;
                } while (mask > 0);

                if (PlayerBoard.Contains(mask))
                    capturesMask |= captureMask;
            }
            return capturesMask;
        }

        public void MakeMove(IMove m, bool updateMove = true)
        {
            _lastMove = m;
            _moveCounter++;

            BitOthelloMove move = (BitOthelloMove)m;
            if (move.IsNull)
            {
                ++_nullMoves;
            }
            else
            {
                // if the move already has the flip mask set (e.g. if the move
                // was obtained through GetMoveFromString)
                // then the mask must be non zero because all non null moves
                // MUST flip disks, if the flip mask is zero and the move is not null
                // then it must not have been updated.
                // This trick is used so as not to add additional flag to BitOthelloMove
                // such as `isUpdated`.
                ulong flipMask;
                if (move.FlipMask == 0)
                    flipMask = GetCapturesFromPosition(move.Position);
                else
                    flipMask = move.FlipMask;
                PlayerBoard.SetPositions(move.Position | flipMask);
                OpponentBoard.ClearPositions(flipMask);

                // update zobrist hash
                if (_useZobrist)
                {
                    _zobristHash!.UpdatePosition(_player, move.Position | flipMask);
                    _zobristHash!.UpdatePosition(_opponent, flipMask);
                    _zobristHash!.UpdatePosition(EMPTY, move.Position);
                }


                move.FlipMask = flipMask;
                _nullMoves = 0;
            }
            SwitchPlayers();
            if (_useZobrist)
            {
                _zobristHash!.UpdatePlayer();
            }
        }

        public void UndoMove(IMove m)
        {
            _lastMove = null;
            _moveCounter--;

            BitOthelloMove move = (BitOthelloMove)m;
            SwitchPlayers();
            if (_useZobrist)
            {
                _zobristHash!.UpdatePlayer();
            }
            if (move.IsNull)
            {
                --_nullMoves;
            }
            else
            {
                // ++_emptyCount;
                PlayerBoard.ClearPositions(move.Position | move.FlipMask);
                OpponentBoard.SetPositions(move.FlipMask);

                // update zobrist hash
                if (_useZobrist)
                {
                    _zobristHash!.UpdatePosition(_player, move.Position | move.FlipMask);
                    _zobristHash!.UpdatePosition(_opponent, move.FlipMask);
                    _zobristHash!.UpdatePosition(EMPTY, move.Position);
                }

                _nullMoves = move.NullMoves;
            }
        }

        public float NaiveEvaluate()
        {
            float value = MathF.Sign(MaterialDiff) * 1e6f;
            return value * (_player == BLACK ? 1 : -1);
        }

        public float MobilityEvaluate()
        {
            if (IsOver)
                return NaiveEvaluate();

            int blackCorners = BitOperations.PopCount(_blackBoard & CORNER_MASK);
            int whiteCorners = BitOperations.PopCount(_whiteBoard & CORNER_MASK);
            float cornerDiff = blackCorners - whiteCorners;

            float mobilityBlack, mobilityWhite;
            if (_player == BLACK)
            {
                mobilityBlack = GetMoves().Length;
                SwitchPlayers();
                mobilityWhite = GetMoves().Length;
                SwitchPlayers();
            }
            else
            {
                SwitchPlayers();
                mobilityBlack = GetMoves().Length;
                SwitchPlayers();
                mobilityWhite = GetMoves().Length;
            }


            float mobilityFactor = (mobilityBlack - mobilityWhite) / (mobilityBlack + mobilityWhite);

            const float W1 = 10, W2 = 1;
            float value = W1 * cornerDiff + W2 * mobilityFactor;
            return value * (_player == BLACK ? 1 : -1);
        }

        public float RandomEvaluate()
        {
            if (IsOver)
                return NaiveEvaluate();

            float value = (float)Utils.RNG.NextDouble() * 2 - 1;
            return value * (_player == BLACK ? 1 : -1);
        }

        public float Evaluate()
        {
            if (IsOver)
                return NaiveEvaluate();

            float value = 0f;
            for (int i = 0; i < _weightValues.Length; ++i)
            {
                ulong mask = _blackBoard & _weightMasks[i];
                value += _weightValues[i] * BitOperations.PopCount(mask);

                mask = _whiteBoard & _weightMasks[i];
                value -= _weightValues[i] * BitOperations.PopCount(mask);
            }

            return value * (_player == BLACK ? 1 : -1);
        }

        public IGame Copy(bool disableZobrist = false)
        {
            BitOthello newGame = new BitOthello(
                player: _player,
                useZobrist: disableZobrist ? false : _useZobrist);
            newGame._nullMoves = _nullMoves;
            newGame._blackBoard = _blackBoard;
            newGame._whiteBoard = _whiteBoard;
            newGame._lastMove = _lastMove;
            newGame._moveCounter = _moveCounter;
            if (!disableZobrist)
                newGame.SetZobristHash();
            return newGame;
        }

        public float[] GetObservation(ObservationMode mode = ObservationMode.FLAT)
        {
            switch (mode)
            {
                case ObservationMode.FLAT:
                    return GetFlatObservation();
                case ObservationMode.IMAGE:
                default:
                    throw new NotImplementedException();
            }
        }

        private float[] GetFlatObservation()
        {
            float[] obs = new float[2 * BOARD_SIZE * BOARD_SIZE];
            const int offset = BOARD_SIZE * BOARD_SIZE;
            var mask = PlayerBoard;
            while (mask > 0)
            {
                obs[mask.PopNextIndex()] = 1.0f;
            }
            mask = OpponentBoard;
            while (mask > 0)
            {
                obs[mask.PopNextIndex() + offset] = 1.0f;
            }
            return obs;
        }

        public bool[] GetActionMasks(out IMove[] moves)
        {
            bool[] mask = new bool[PossibleMovesCount];
            moves = GetMoves();

            foreach (IMove move in moves)
            {
                mask[move.Index] = true;
            }
            return mask;
        }

        public void Display(bool showMoves = false)
        {
            char[,] symbols = new char[BOARD_SIZE, BOARD_SIZE];
            ulong tmp = _blackBoard;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                int i = idx / BOARD_SIZE;
                int j = idx % BOARD_SIZE;
                symbols[i, j] = 'X';
            }
            tmp = _whiteBoard;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                int i = idx / BOARD_SIZE;
                int j = idx % BOARD_SIZE;
                symbols[i, j] = 'O';
            }
            var moves = showMoves ? GetMoves().Cast<BitOthelloMove>().ToList() : null;
            if (moves != null)
            {
                foreach (var move in moves)
                {
                    if (move.Index == BitOthelloMove.NullIndex)
                        continue;
                    int i = move.Index / BOARD_SIZE;
                    int j = move.Index % BOARD_SIZE;
                    symbols[i, j] = '.';
                }
            }


            Console.Write("  ");
            for (int j = 0; j < BOARD_SIZE; ++j)
            {
                Console.Write((char)('a' + j) + " ");
            }
            Console.WriteLine();

            for (int i = 0; i < BOARD_SIZE; ++i)
            {
                Console.Write($"{i + 1} ");
                for (int j = 0; j < BOARD_SIZE; ++j)
                {
                    if (symbols[i, j] == '\0')
                        Console.Write("  ");
                    else
                        Console.Write(symbols[i, j] + " ");
                }
                Console.WriteLine();
            }
        }

        public void SwitchPlayers()
        {
            _player = (sbyte)(1 - _player);
        }

        public override string ToString()
        {
            char[] chars = new char[BOARD_SIZE * BOARD_SIZE + 2];

            ulong tmp = _blackBoard;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                chars[idx] = 'X';
            }
            tmp = _whiteBoard;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                chars[idx] = 'O';
            }
            tmp = EmptyMask;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                chars[idx] = '.';
            }

            chars[^2] = (char)(_player + '0');
            chars[^1] = (char)(_nullMoves + '0');
            return new string(chars);
        }
    }
}