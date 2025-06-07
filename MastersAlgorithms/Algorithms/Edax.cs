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
        static ConcurrentQueue<string> stderrLines = new ConcurrentQueue<string>();
        Thread _stderrThread;
        StreamWriter _stdin;
        StreamReader _stdout;
        private int _lastMoveCounter;
        private string _output;
        private string _errorMessage;
        private bool _verbose;

        public Edax(int level, string directory, bool bookUsage = false, bool verbose = false)
        {
            _output = "";
            _errorMessage = "";
            _lastMoveCounter = int.MaxValue;
            _verbose = verbose;

            string exePath = Path.Join(directory, "wEdax-x86-64-v3.exe");

            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = exePath;
            info.Arguments = $"--verbose 0 --level {level} --book-usage {(bookUsage ? "on" : "off")}";
            info.WorkingDirectory = directory;
            info.UseShellExecute = false;
            info.RedirectStandardInput = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.CreateNoWindow = true;

            var process = Process.Start(info)!;

            _stdin = process.StandardInput;
            _stdin.AutoFlush = true;
            _stdout = process.StandardOutput;

            _stderrThread = new Thread(() =>
            {
                string? line;
                while ((line = process.StandardError.ReadLine()) != null)
                {
                    stderrLines.Enqueue(line);
                }
            });
            _stderrThread.IsBackground = true;
            _stderrThread.Start();
        }

        public string GetDebugInfo()
        {
            return $"out {_output} | err {_errorMessage}";
        }

        public IMove? GetMove(IGame g)
        {
            var game = (g as BitOthello)!;
            bool canContinue = true;
            if (_lastMoveCounter >= game.MoveCounter) // new game has started
                SetGame(game);
            else // old game still going, update using the last move
                canContinue = TryWriteMove(game.LastMove!);
            _lastMoveCounter = game.MoveCounter;

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
                var moves = game.GetMoves();
                if (moves[0].Index != BitOthelloMove.NullIndex)
                    throw new Exception("Edax could not continue, but the only move left is not null. " +
                    $"state: {game} last move: {(game.LastMove as BitOthelloMove)!.Algebraic()}");

                stderrLines.TryDequeue(out _errorMessage!);
                move = moves[0];
            }

            if (_verbose)
                Console.WriteLine(GetDebugInfo());

            return move;
        }

        void SetGame(BitOthello game)
        {
            StringBuilder command = new StringBuilder("setboard ");
            command.Append(game.ToString()[..^2]);
            command.Append($" {(game.Player == BitOthello.BLACK ? 'X' : 'O')}");
            _stdin.WriteLine(command.ToString());
            // _ = _stdout.ReadLine()!; // ignore prompt
        }

        bool TryWriteMove(IMove move)
        {
            _stdin.WriteLine((move as BitOthelloMove)!.Algebraic());
            if (CheckForWarnings())
                return false;

            _ = _stdout.ReadLine()!; // ignore prompt
            _ = _stdout.ReadLine()!; // ignore whitespace
            _ = _stdout.ReadLine()!; // ignore feedback
            return true;
        }

        bool TryReadMove(out IMove? move)
        {
            move = null;

            _stdin.WriteLine("go");

            while (true)
            {
                _output = _stdout.ReadLine()!;
                if (_output.Length <= 1) // clear prompts and white spaces
                    continue;
                break;
            }

            if (_output == "*** Game Over ***")
                return false;

            move = BitOthelloMove.FromAlgebraic(_output[^2..]);
            return true;
        }

        bool CheckForWarnings()
        {
            Thread.Sleep(1);
            return stderrLines.TryDequeue(out _errorMessage!);
        }
    }
}