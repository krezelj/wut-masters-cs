using System.Diagnostics;
using System.Diagnostics.Contracts;
using MastersAlgorithms.Algorithms;
using MastersAlgorithms.Games;

namespace MastersAlgorithms
{
    public class GameRunner
    {
        public IGame Game;
        public IAlgorithm[] Players;
        int _currentPlayerIndex;
        IAlgorithm _currentPlayer => Players[_currentPlayerIndex];
        Stopwatch _sw;
        public float[] PlayerTimes;
        public int[] PlayerMoveCount;

        public GameRunner(IGame game, IAlgorithm[] players, int firstPlayerIndex)
        {
            Game = game;
            Players = players;
            _currentPlayerIndex = firstPlayerIndex;
            _sw = new Stopwatch();
            PlayerTimes = new float[players.Length];
            PlayerMoveCount = new int[players.Length];
        }

        public void Run()
        {
            while (true)
            {
                _sw.Restart();
                var move = _currentPlayer.GetMove(Game);
                PlayerTimes[_currentPlayerIndex] += _sw.ElapsedTicks;
                PlayerMoveCount[_currentPlayerIndex]++;

                Game.MakeMove(move!);
                AdvancePlayers();

                if (Game.IsOver)
                    break;
            }
        }

        private void AdvancePlayers()
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % Players.Length;
        }

        public string GetDebugInfo()
        {
            var meanTimes = string.Join(",",
                Enumerable.Range(0, Players.Length).
                Select(i => PlayerTimes[i] / (PlayerMoveCount[i] * Stopwatch.Frequency) * 1000)
            );
            return meanTimes;
        }

    }
}