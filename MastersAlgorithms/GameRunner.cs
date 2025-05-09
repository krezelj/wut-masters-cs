using System.Diagnostics.Contracts;
using MastersAlgorithms.Algorithms;
using MastersAlgorithms.Games;

namespace MastersAlgorithms
{
    public class GameRunner
    {
        public IGame Game;
        public IAlgorithm[] Players;
        int _playerIndex;
        IAlgorithm _currentPlayer => Players[_playerIndex];

        public GameRunner(IGame game, IAlgorithm[] players, int firstPlayerIndex)
        {
            Game = game;
            Players = players;
            _playerIndex = firstPlayerIndex;
        }

        public void Run()
        {
            while (true)
            {
                var move = _currentPlayer.GetMove(Game);
                Game.MakeMove(move!);
                AdvancePlayers();

                if (Game.IsOver)
                    break;
            }
        }

        private void AdvancePlayers()
        {
            _playerIndex = (_playerIndex + 1) % Players.Length;
        }

        public string GetDebugInfo()
        {
            return "GameRunner DebugInfo not implemented yet";
        }

    }
}