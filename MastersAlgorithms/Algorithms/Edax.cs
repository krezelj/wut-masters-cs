using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MastersAlgorithms.Games;

namespace MastersAlgorithms.Algorithms
{
    public class Edax : IAlgorithm
    {
        Thread _stderrThread = null!;
        Process _process = null!;
        ProcessStartInfo _processInfo = null!;
        StreamWriter _stdin = null!;
        StreamReader _stdout = null!;
        private int _lastMoveCounter;
        private string _output;
        private string _errorMessage;
        private bool _stateful;
        private bool _verbose;
        private BitOthello _game = null!;

        public Edax(int level, string directory, bool bookUsage = false, bool stateful = true, bool verbose = false)
        {
            _output = "";
            _errorMessage = "";
            _lastMoveCounter = int.MaxValue;
            _stateful = stateful;
            _verbose = verbose;

            string exePath = Path.Join(directory, "wEdax-x86-64-v3.exe");

            _processInfo = new ProcessStartInfo();
            _processInfo.FileName = exePath;
            _processInfo.Arguments = $"--verbose 0 --level {level} --book-usage {(bookUsage ? "on" : "off")}";
            _processInfo.WorkingDirectory = directory;
            _processInfo.UseShellExecute = false;
            _processInfo.RedirectStandardInput = true;
            _processInfo.RedirectStandardOutput = true;
            _processInfo.RedirectStandardError = true;
            _processInfo.CreateNoWindow = true;

            InitProcess(_processInfo);
        }

        public string GetDebugInfo()
        {
            return $"out {_output} | err {_errorMessage}";
        }

        public IMove? GetMove(IGame g)
        {
            _errorMessage = "";
            _game = (g as BitOthello)!;
            bool canContinue = true;
            if (!_stateful || _lastMoveCounter >= _game.MoveCounter)
                SetGame();
            else // old game still going, update using the last move
                canContinue = TryWriteMove(_game.LastMove!);
            _lastMoveCounter = _game.MoveCounter;

            IMove? move = null;
            if (canContinue)
            {
                canContinue = TryReadMove(out move);
            }
            if (!canContinue)
            {
                // this will most likely happen if the game has ended in two pass moves
                // Edax will finish the game as soon as there is one pass move
                // and the current player has no non-pass moves
                // Edax will refuse making the second passing move and will throw and error
                // to the stderr, TryWriteMove will catch that and return false

                // in this case the game is already pretty much finished so we can just
                // return the only move available which *should* be the null move
                var moves = _game.GetMoves();
                if (moves[0].Index != BitOthelloMove.NullIndex)
                    throw new Exception("Edax could not continue, but the only move left is not null. " +
                    $"state: {_game} last move: {(_game.LastMove as BitOthelloMove)!.Algebraic()}");

                _output = "Move Skipped";
                move = moves[0];
            }

            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            return move;
        }

        void InitProcess(ProcessStartInfo info)
        {
            _process = Process.Start(info)!;

            _stdin = _process.StandardInput;
            _stdin.AutoFlush = true;
            _stdout = _process.StandardOutput;

            _stderrThread = new Thread(() =>
            {
                while (true)
                {
                    _process.StandardError.ReadLine();
                }
            });
            _stderrThread.IsBackground = true;
            _stderrThread.Start();
        }

        void SetGame()
        {
            string command = $"setboard {_game.ToString()[..^2]} {(_game.Player == BitOthello.BLACK ? 'X' : 'O')}";
            _stdin.WriteLine(command);
        }

        bool TryWriteMove(IMove move)
        {
            _stdin.WriteLine((move as BitOthelloMove)!.Algebraic());
            if (CheckForEarlyGameOver())
                return false;

            return true;
        }

        bool TryReadMove(out IMove? move)
        {
            move = null;

            _stdin.WriteLine("go");

            while (true)
            {
                _output = _stdout.ReadLine()!;
                if (_output.Length <= 2)
                    continue;
                if (_output.Contains("Game Over"))
                    return false;
                if (!_output.StartsWith("Edax plays"))
                    continue;
                break;
            }

            move = BitOthelloMove.FromAlgebraic(_output[^2..]);
            return true;
        }

        bool CheckForEarlyGameOver()
        {
            IGame game = _game.Copy(true);
            var moves = game.GetMoves();
            if (moves[0].Index != BitOthelloMove.NullIndex)
                return false; // not a null move, game is still going

            game.MakeMove(moves[0], false); // make that null move to update the game state
            moves = game.GetMoves();
            bool earlyGameOver = moves[0].Index == BitOthelloMove.NullIndex;
            game.UndoMove(moves[0]); // undo the null move
            return earlyGameOver;
        }

        //bool CheckForWarnings()
        //{
        //    // Thread.Sleep(1);
        //    return stderrLines.TryDequeue(out _errorMessage!);
        //}
    }
}