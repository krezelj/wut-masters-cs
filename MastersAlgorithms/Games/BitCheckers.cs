using System.Diagnostics;
using System.Numerics;

namespace MastersAlgorithms.Games
{
    public class BitCheckersMove(
        ulong startPosition,
        ulong endPosition,
        bool isForced,
        bool isCapture,
        bool isPromotion,
        sbyte previousState,
        int previousMovesWithoutActivity) : IMove
    {
        public const int NullIndex = 128;
        public int Index
        {
            get
            {
                if (IsNull)
                    return NullIndex;
                int startIdx = startPosition.Index();
                int offset = (startIdx >> 2) % 2;
                int diff = endPosition.Index() - startIdx + offset;
                switch (diff)
                {
                    case -4:
                    case -9:
                    case -8:
                        return startIdx * 4;
                    case -3:
                    case -7:
                    case -6:
                        return startIdx * 4 + 1;
                    case 4:
                    case 7:
                    case 8:
                        return startIdx * 4 + 2;
                    case 5:
                    case 9:
                    case 10:
                        return startIdx * 4 + 3;
                    default:
                        throw new NotImplementedException($"Invalid move index: {ToString()}");
                }
            }
        }

        public ulong StartPosition => startPosition;
        public ulong EndPosition => endPosition;
        public bool IsForced => isForced;
        public bool IsCapture => isCapture;
        public bool IsKingCapture = false;
        public ulong CaptureMask = 0UL;
        public bool IsPromotion => isPromotion;
        public bool IsNull => startPosition == 0;
        public sbyte PreviousState => previousState;
        public int PreviousMovesWithoutActivity => previousMovesWithoutActivity;

        public static BitCheckersMove NullMove(sbyte state, int movesWithoutActivity)
        {
            return new BitCheckersMove(
                startPosition: 0UL,
                endPosition: 0UL,
                isForced: false,
                isCapture: false,
                isPromotion: false,
                previousState: state,
                previousMovesWithoutActivity: movesWithoutActivity);
        }

        public override string ToString()
        {
            return $"{Index},{startPosition},{endPosition},{isForced},{isCapture},{IsKingCapture},{CaptureMask},"
                + $"{isPromotion},{previousState},{previousMovesWithoutActivity}";
        }
    }

    public class BitCheckers : IGame
    {
        public const sbyte BLACK = 0;
        public const sbyte WHITE = 1;
        public const sbyte KING = 2;
        public const sbyte EMPTY = 3;
        public const sbyte NONE = 127;
        public const sbyte BOARD_SIZE = 8;
        private const ulong MASK = 0xFFFFFFFF00000000UL;
        private const ulong L3 = 0x00000000_00E0E0E0UL;
        private const ulong L5 = 0x00000000_07070707UL;
        private const ulong R3 = 0x00000000_07070700UL;
        private const ulong R5 = 0x00000000_E0E0E0E0UL;
        private const sbyte MOVES_LIMIT = 30;

        public int PossibleMovesCount => 129; // 32 positions x 4 directions + 1 null move
        public static int PossibleResultsCount => 3;

        private sbyte _player = BLACK;
        public int Player => _player;
        private sbyte _opponent => (sbyte)(1 - _player);

        private IMove? _lastMove = null;
        public IMove? LastMove => _lastMove;

        private int _moveCounter = 0;
        public int MoveCounter => _moveCounter;

        public bool IsOver
        {
            get
            {
                int whiteCount = BitOperations.PopCount(_whiteBoard);
                if (whiteCount == 0)
                    return true; // black won

                int blackCount = BitOperations.PopCount(_blackBoard);
                if (blackCount == 0)
                    return true; // white won

                if (!AnyLegalMoves())
                    return true; // the opponent won

                // draws
                if (_movesWithoutActivity >= MOVES_LIMIT)
                    return true;

                // 1 kings vs 1 king
                if (blackCount == 1 && whiteCount == 1 && BitOperations.PopCount(_kings) == 2)
                    return true;

                return false;
            }
        }

        public int Result
        {
            get
            {
                if (!IsOver)
                    return -1;

                int whiteCount = BitOperations.PopCount(_whiteBoard);
                if (whiteCount == 0)
                    return BLACK; // black won

                int blackCount = BitOperations.PopCount(_blackBoard);
                if (blackCount == 0)
                    return WHITE; // white won

                if (!AnyLegalMoves())
                    return _opponent; // current player has no moves, opponent won

                // we know the game is over but both players still have pieces
                // on board so it must be a draw
                return PossibleResultsCount - 1;
            }
        }

        private ulong _whiteBoard;
        private ulong _blackBoard;
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
        private ulong _kings;
        private ulong _forcedStartPosition;
        private int _movesWithoutActivity;
        private sbyte _capturingPlayer;

        public ulong EmptyMask => (~_whiteBoard) & (~_blackBoard) & (~MASK);
        private bool _useZobrist;
        private ZobristHash? _zobristHash;
        public ulong zKey => _useZobrist ? _zobristHash!.Key : throw new Exception("ZobristHash is not used.");

        public BitCheckers() : this(BLACK, true) { } // dummy parameterless constructor;

        public BitCheckers(sbyte player = BLACK, bool useZobrist = true)
        {
            _player = player;

            // white starts on top
            _blackBoard = 0xFFF00000UL;
            _whiteBoard = 0x00000FFFUL;

            _useZobrist = useZobrist;
            if (_useZobrist)
            {
                // black, white, king, empty
                _zobristHash = new ZobristHash(nTypes: 4, nPositions: BOARD_SIZE * BOARD_SIZE);
                SetZobristHash();
            }
        }

        private void SetZobristHash()
        {
            _zobristHash!.ResetKey();
            _zobristHash!.UpdatePosition(BLACK, _blackBoard);
            _zobristHash!.UpdatePosition(WHITE, _whiteBoard);
            _zobristHash!.UpdatePosition(KING, _kings);
            _zobristHash!.UpdatePosition(EMPTY, EmptyMask);
            if (_player == WHITE)
                _zobristHash!.UpdatePlayer();
        }

        public IGame Copy(bool disableZobrist = false)
        {
            BitCheckers newGame = new BitCheckers(
                player: _player,
                useZobrist: disableZobrist ? false : _useZobrist
            );
            newGame._whiteBoard = _whiteBoard;
            newGame._blackBoard = _blackBoard;
            newGame._kings = _kings;
            newGame._movesWithoutActivity = _movesWithoutActivity;
            newGame._capturingPlayer = _capturingPlayer;
            newGame._forcedStartPosition = _forcedStartPosition;
            newGame._lastMove = _lastMove;
            newGame._moveCounter = _moveCounter;
            if (newGame._useZobrist)
                newGame.SetZobristHash();
            return newGame;
        }

        private ulong GetJumpersMask(ulong playerPieces, ulong opponentPieces, ulong kings, ulong empty)
        {
            // based on https://3dkingdoms.com/checkers/bitboards.htm

            ulong playerKings = playerPieces & kings;
            ulong mask = 0UL;

            ulong tmp = (empty << 4) & opponentPieces; // opponents next to empty spaces
            mask |= (((tmp & L3) << 3) | ((tmp & L5) << 5)) & playerPieces; // players next to those opponents

            // now in a second direction
            tmp = (((empty & L3) << 3) | ((empty & L5) << 5)) & opponentPieces;
            mask |= (tmp << 4) & playerPieces;

            if (playerKings > 0)
            {
                // now in reverse if player kings exist
                tmp = (empty >> 4) & opponentPieces;
                mask |= (((tmp & R3) >> 3) | ((tmp & R5) >> 5)) & playerKings;

                tmp = (((empty & R3) >> 3) | ((empty & R5) >> 5)) & opponentPieces;
                mask |= (tmp >> 4) & playerKings;
            }

            return mask;
        }

        private ulong GetMoversMask(ulong playerPieces, ulong kings, ulong empty)
        {
            // based on https://3dkingdoms.com/checkers/bitboards.htm

            ulong playerKings = playerPieces & kings;

            ulong mask = (empty << 4) & playerPieces;
            mask |= ((empty & L3) << 3) & playerPieces;
            mask |= ((empty & L5) << 5) & playerPieces;
            if (playerKings > 0)
            {
                mask |= (empty >> 4) & playerKings;
                mask |= ((empty & R3) >> 3) & playerKings;
                mask |= ((empty & R5) >> 5) & playerKings;
            }
            return mask;
        }

        private ulong GetMoverTargets(ulong position, ulong kings, ulong empty)
        {
            bool isKing = (position & kings) > 0;
            ulong mask = ((position >> 4) | ((position & R3) >> 3) | ((position & R5) >> 5)) & empty;
            if (isKing)
                mask |= ((position << 4) | ((position & L3) << 3) | ((position & L5) << 5)) & empty;
            return mask;
        }

        private ulong GetJumperTargets(ulong position, ulong opponentPieces, ulong kings, ulong empty)
        {
            bool isKing = (position & kings) > 0;
            ulong mask = 0UL;

            ulong tmp = (((position & R3) >> 3) | ((position & R5) >> 5)) & opponentPieces;
            mask |= (tmp >> 4) & empty;

            tmp = (position >> 4) & opponentPieces;
            mask |= (((tmp & R3) >> 3) | ((tmp & R5) >> 5)) & empty;

            if (isKing)
            {
                tmp = (((position & L3) << 3) | ((position & L5) << 5)) & opponentPieces;
                mask |= (tmp << 4) & empty;

                tmp = (position << 4) & opponentPieces;
                mask |= (((tmp & L3) << 3) | ((tmp & L5) << 5)) & empty;
            }
            return mask;
        }

        private bool IsPromotion(ulong startPosition, ulong endPosition, ulong kings)
        {
            if ((startPosition & kings) > 0)
                return false; // already a king
            return (endPosition & 0b1111UL) > 0; // top row from player perspective
        }

        public bool AnyLegalMoves()
        {
            bool opponentIsCapturing = _capturingPlayer == _opponent;
            if (opponentIsCapturing)
                return true;

            bool playerIsCapturing = _capturingPlayer == _player;
            if (playerIsCapturing)
                return true;

            ulong playerPieces = PlayerBoard;
            ulong opponentPieces = OpponentBoard;
            ulong kings = _kings;
            ulong empty = EmptyMask;
            bool doMirror = _player == WHITE;
            if (doMirror)
            {
                playerPieces = Mirror(playerPieces);
                opponentPieces = Mirror(opponentPieces);
                kings = Mirror(kings);
                empty = Mirror(empty);
            }

            // we do not need to check for captures first here
            // since either captures are possible (and we will check them next)
            // or they are not possible in which case we have to check normal moves anyway
            // but since normal moves are almost always possible its faster to check this first
            // and return early
            if (GetMoversMask(playerPieces, kings, empty) > 0)
                return true; // any normal moves possible
            if (GetJumpersMask(playerPieces, opponentPieces, kings, empty) > 0)
                return true; // captures possible

            return false;
        }

        public IMove[] GetMoves()
        {
            bool opponentIsCapturing = _capturingPlayer == _opponent;
            if (opponentIsCapturing)
            {
                // opponent is mid capture, force null move
                return [BitCheckersMove.NullMove(_capturingPlayer, _movesWithoutActivity)];
            }

            ulong playerPieces = PlayerBoard;
            ulong opponentPieces = OpponentBoard;
            ulong kings = _kings;
            ulong empty = EmptyMask;

            bool doMirror = _player == WHITE;
            bool playerIsCapturing = _capturingPlayer == _player;

            if (doMirror)
            {
                playerPieces = Mirror(playerPieces);
                opponentPieces = Mirror(opponentPieces);
                kings = Mirror(kings);
                empty = Mirror(empty);
            }

            BitCheckersMove[] moves = new BitCheckersMove[PossibleMovesCount];
            int legalMovesCount = 0;

            // if the current player is mid-capture, the start position is forced (as determined by the
            // end position of their previous move)
            ulong startPositions;
            if (playerIsCapturing)
                startPositions = doMirror ? Mirror(_forcedStartPosition) : _forcedStartPosition;
            else
                startPositions = GetJumpersMask(playerPieces, opponentPieces, kings, empty);


            if (startPositions > 0)
            {
                while (startPositions > 0)
                {
                    ulong startPosition = startPositions.PopNextPosition();
                    ulong endPositions = GetJumperTargets(startPosition, opponentPieces, kings, empty);
                    while (endPositions > 0)
                    {
                        ulong endPosition = endPositions.PopNextPosition();
                        moves[legalMovesCount++] = new BitCheckersMove(
                            doMirror ? Mirror(startPosition) : startPosition,
                            doMirror ? Mirror(endPosition) : endPosition,
                            isForced: playerIsCapturing,
                            isCapture: true,
                            isPromotion: IsPromotion(startPosition, endPosition, kings),
                            previousState: _capturingPlayer,
                            previousMovesWithoutActivity: _movesWithoutActivity);
                    }
                }
                Array.Resize(ref moves, legalMovesCount);
                return moves;
            }

            // if no captures possible, check for normal moves
            startPositions = GetMoversMask(playerPieces, kings, empty);
            while (startPositions > 0)
            {
                ulong startPosition = startPositions.PopNextPosition();
                ulong endPositions = GetMoverTargets(startPosition, kings, empty);
                while (endPositions > 0)
                {
                    ulong endPosition = endPositions.PopNextPosition();
                    moves[legalMovesCount++] = new BitCheckersMove(
                        doMirror ? Mirror(startPosition) : startPosition,
                        doMirror ? Mirror(endPosition) : endPosition,
                        isForced: false,
                        isCapture: false,
                        isPromotion: IsPromotion(startPosition, endPosition, kings),
                        previousState: _capturingPlayer,
                        previousMovesWithoutActivity: _movesWithoutActivity);
                }
            }
            Array.Resize(ref moves, legalMovesCount);
            return moves;
        }

        public IMove GetRandomMove()
        {
            // throw new NotImplementedException();
            var moves = GetMoves();
            return moves[Utils.RNG.Next(moves.Length)];
        }

        public ulong GetCaptureMask(BitCheckersMove move)
        {
            int startIdx = move.StartPosition.Index();
            int endIdx = move.EndPosition.Index();
            int captureIdx = ((startIdx + endIdx) >> 1) + 1;
            captureIdx -= (startIdx >> 2) % 2;
            return 1UL << captureIdx;
        }

        public void MakeMove(IMove m, bool updateMove = true)
        {
            _lastMove = m;
            _moveCounter++;

            BitCheckersMove move = (BitCheckersMove)m;
            // if null move, only update players
            if (move.IsNull)
            {
                SwitchPlayers();
                if (_useZobrist)
                    _zobristHash!.UpdatePlayer();
                return;
            }

            // handle general move updates
            PlayerBoard.TogglePositions(move.StartPosition | move.EndPosition);
            if ((_kings & move.StartPosition) > 0)
            {
                _kings.TogglePositions(move.StartPosition | move.EndPosition);
            }
            if (move.IsPromotion)
            {
                _kings.SetPositions(move.EndPosition);
            }

            // handle capturing
            bool multiJump = false;
            if (move.IsCapture)
            {
                // if move is capture, but capture mask is 0 then it was never calculated
                if (move.CaptureMask == 0)
                {
                    move.CaptureMask = GetCaptureMask(move);
                    move.IsKingCapture = (move.CaptureMask & _kings) > 0;
                }
                ulong captureMask = move.CaptureMask;
                OpponentBoard.ClearPositions(captureMask);
                _kings.ClearPositions(captureMask);

                bool doMirror = _player == WHITE;
                multiJump = GetJumperTargets(
                    doMirror ? Mirror(move.EndPosition) : move.EndPosition,
                    doMirror ? Mirror(OpponentBoard) : OpponentBoard,
                    doMirror ? Mirror(_kings) : _kings,
                    doMirror ? Mirror(EmptyMask) : EmptyMask) > 0;
            }

            // handle multi jump sequences
            if (multiJump)
            {
                _capturingPlayer = _player;
                _forcedStartPosition = move.EndPosition;
            }
            else
            {
                _forcedStartPosition = 0UL;
                _capturingPlayer = NONE;
            }

            // handle state updates
            if (!move.IsCapture && !move.IsPromotion)
                _movesWithoutActivity++;
            else
                _movesWithoutActivity = 0;
            SwitchPlayers();
            if (_useZobrist)
            {
                _zobristHash!.UpdatePlayer();
                _zobristHash!.UpdatePosition(BLACK, _blackBoard);
                _zobristHash!.UpdatePosition(WHITE, _whiteBoard);
                _zobristHash!.UpdatePosition(KING, _kings);
                _zobristHash!.UpdatePosition(EMPTY, EmptyMask);
            }
        }

        public void UndoMove(IMove m)
        {
            _lastMove = null;
            _moveCounter--;

            BitCheckersMove move = (BitCheckersMove)m;
            SwitchPlayers(); // now, _player is the one that was making the move we are undoing
            if (move.IsNull)
            {
                if (_useZobrist)
                    _zobristHash!.UpdatePlayer();
                return;
            }

            // handle general move updates
            PlayerBoard.TogglePositions(move.StartPosition | move.EndPosition);
            if ((_kings & move.EndPosition) > 0)
            {
                _kings.TogglePositions(move.StartPosition | move.EndPosition);
            }
            if (move.IsPromotion)
            {
                _kings.ClearPositions(move.StartPosition);
            }

            // handle capturing
            if (move.IsCapture)
            {
                OpponentBoard.SetPositions(move.CaptureMask);
                if (move.IsKingCapture)
                    _kings.SetPositions(move.CaptureMask);
            }

            // handle forced moves
            _capturingPlayer = move.PreviousState;
            _forcedStartPosition = move.IsForced ? move.StartPosition : 0UL;

            // handle state updates
            _movesWithoutActivity = move.PreviousMovesWithoutActivity;
            if (_useZobrist)
            {
                _zobristHash!.UpdatePlayer();
                _zobristHash!.UpdatePosition(BLACK, _blackBoard);
                _zobristHash!.UpdatePosition(WHITE, _whiteBoard);
                _zobristHash!.UpdatePosition(KING, _kings);
                _zobristHash!.UpdatePosition(EMPTY, EmptyMask);
            }
        }

        public float Evaluate()
        {
            int whiteCount = BitOperations.PopCount(_whiteBoard);
            int blackCount = BitOperations.PopCount(_blackBoard);
            float value = 0.0f;
            if (IsOver)
            {
                if (whiteCount == 0)
                    value = 1e6f;
                if (blackCount == 0)
                    value = -1e6f;
                if (!AnyLegalMoves())
                    value = _player == BLACK ? -1e6f : 1e6f; // opponent won
                return value * (_player == BLACK ? 1 : -1);
            }

            int whiteKingCount = BitOperations.PopCount(_whiteBoard & _kings);
            int blackKingCount = BitOperations.PopCount(_blackBoard & _kings);

            value = blackCount + 3 * blackKingCount - whiteCount - 3 * whiteKingCount;
            return value * (_player == BLACK ? 1 : -1);
        }

        public float MobilityEvaluate()
        {
            throw new NotImplementedException();
        }

        public float NaiveEvaluate()
        {
            throw new NotImplementedException();
        }

        public float RandomEvaluate()
        {
            throw new NotImplementedException();
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

        public IMove GetMoveFromAction(int action)
        {
            throw new NotImplementedException();
        }

        public IMove GetMoveFromString(string m)
        {
            // 0 -- index
            // 1 -- startPosition
            // 2 -- endPositon
            // 3 -- isForced
            // 4 -- isCapture
            // 5 -- isKingCapture
            // 6 -- captureMask
            // 7 -- isPromotion
            // 8 -- prevState
            // 9 -- prevMovesWithoutActivity

            string[] data = m.Split(',');
            int index = int.Parse(data[0]);
            sbyte previousState = sbyte.Parse(data[^2]);
            sbyte previousMovesWithoutActivity = sbyte.Parse(data[^1]);
            if (index == BitCheckersMove.NullIndex)
                return BitCheckersMove.NullMove(previousState, previousMovesWithoutActivity);

            ulong startPosition = ulong.Parse(data[1]);
            ulong endPosition = ulong.Parse(data[2]);
            bool isForced = bool.Parse(data[3]);
            bool isCapture = bool.Parse(data[4]);
            bool isKingCapture = bool.Parse(data[5]);
            ulong captureMask = ulong.Parse(data[6]);
            bool isPromotion = bool.Parse(data[7]);
            BitCheckersMove move = new BitCheckersMove(
                startPosition,
                endPosition,
                isForced,
                isCapture,
                isPromotion,
                previousState,
                previousMovesWithoutActivity);
            move.CaptureMask = captureMask;
            move.IsKingCapture = isKingCapture;
            return move;
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
            // 3 layers, player, opponent, kings all seen from the perspective of the current player
            float[] obs = new float[3 * 4 * BOARD_SIZE];
            const int offset = 4 * BOARD_SIZE; // only 32 positions can be occupied
            var mask = PlayerBoard;
            if (_player == WHITE)
                mask = Mirror(mask);
            while (mask > 0)
            {
                obs[mask.PopNextIndex()] = 1.0f;
            }

            mask = OpponentBoard;
            if (_player == WHITE)
                mask = Mirror(mask);
            while (mask > 0)
            {
                obs[mask.PopNextIndex() + offset] = 1.0f;
            }

            mask = _kings;
            if (_player == WHITE)
                mask = Mirror(mask);
            while (mask > 0)
            {
                obs[mask.PopNextIndex() + 2 * offset] = 1.0f;
            }
            return obs;
        }

        public IMove[] SortMoves(IMove[] moves, int moveIndex)
        {
            // couldn't be bothered to implement this and
            // it's not really necessary for the thesis
            return moves;
        }

        public void SwitchPlayers()
        {
            _player = (sbyte)(1 - _player);
        }

        public void Display(bool showMoves = true)
        {
            char[,] symbols = new char[BOARD_SIZE, BOARD_SIZE];
            ulong tmp = _blackBoard;
            while (tmp > 0)
            {
                ulong position = tmp.PopNextPosition();
                int idx = position.Index();
                idx = idx * 2 + 1 - ((idx >> 2) % 2);

                int i = idx / BOARD_SIZE;
                int j = idx % BOARD_SIZE;
                symbols[i, j] = (position & _kings) > 0 ? 'X' : 'x';
            }
            tmp = _whiteBoard;
            while (tmp > 0)
            {
                ulong position = tmp.PopNextPosition();
                int idx = position.Index();
                idx = idx * 2 + 1 - ((idx >> 2) % 2);

                int i = idx / BOARD_SIZE;
                int j = idx % BOARD_SIZE;
                symbols[i, j] = (position & _kings) > 0 ? 'O' : 'o';
            }
            tmp = EmptyMask;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                idx = idx * 2 + 1 - ((idx >> 2) % 2);

                int i = idx / BOARD_SIZE;
                int j = idx % BOARD_SIZE;
                symbols[i, j] = '.';
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

        public ulong Mirror(ulong mask)
        {
            return mask.ReverseBits() >> 32;
        }

        public override string ToString()
        {
            char[] chars = new char[BOARD_SIZE * BOARD_SIZE + 1];

            ulong tmp = _blackBoard;
            while (tmp > 0)
            {
                ulong position = tmp.PopNextPosition();
                int idx = position.Index();
                chars[idx] = (position & _kings) > 0 ? 'X' : 'x';
            }
            tmp = _whiteBoard;
            while (tmp > 0)
            {
                ulong position = tmp.PopNextPosition();
                int idx = position.Index();
                chars[idx] = (position & _kings) > 0 ? 'O' : 'o';
            }
            tmp = EmptyMask;
            while (tmp > 0)
            {
                int idx = tmp.PopNextPosition().Index();
                chars[idx] = '.';
            }

            chars[^1] = (char)(_player + '0');
            return new string(chars);
        }
    }
}