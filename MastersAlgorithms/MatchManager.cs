using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MastersAlgorithms.Algorithms;
using MastersAlgorithms.Games;

namespace MastersAlgorithms
{
    public class MatchManager//<TGame> where TGame : IGame, new()
    {
        private struct NewGameData(IGame game, bool isMirrored)
        {
            public readonly IGame Game => game;
            public readonly bool IsMirrored => isMirrored;
        }

        public IAlgorithm[] Players;
        public Type GameType;
        public int NGames;
        public bool MirrorGames;
        public int NRandomMoves;
        public int[,] Results;

        public MatchManager(
            IAlgorithm[] players,
            Type gameType,
            int nGames,
            bool mirrorGames,
            int nRandomMoves)
        {
            Players = players;
            GameType = gameType;
            NGames = nGames;
            MirrorGames = mirrorGames;
            NRandomMoves = nRandomMoves;

            // BindingFlags.Static | BindingFlags.Public
            int possibleResultsCount = (int)GameType.GetProperty("PossibleResultsCount")!.GetValue(null)!;
            Results = new int[possibleResultsCount, Players.Length];
        }

        public string GetDebugInfo()
        {
            return string.Join(",", Results.Cast<int>().ToArray());
        }

        public void Run()
        {
            foreach (var gameData in GetGames())
            {
                int firstPlayerIndex = 0;
                if (gameData.IsMirrored)
                    firstPlayerIndex = (firstPlayerIndex + 1) % Players.Length;
                var gameRunner = new GameRunner(gameData.Game, Players, firstPlayerIndex);
                gameRunner.Run();
                ++Results[gameData.Game.Result, firstPlayerIndex];
            }
        }

        private IEnumerable<NewGameData> GetGames()
        {
            int totalGames = NGames * (MirrorGames ? 2 : 1);
            IGame? gameCopy = null;
            for (int i = 0; i < totalGames; i++)
            {
                if (gameCopy != null)
                {
                    yield return new NewGameData(gameCopy, true);
                    gameCopy = null;
                    continue;
                }

                // else if gameCopy was null
                IGame nextGame = GenerateGame();
                if (MirrorGames)
                {
                    gameCopy = nextGame.Copy();
                }
                yield return new NewGameData(nextGame, false);
            }
        }

        private IGame GenerateGame()
        {
            //IGame game = new TGame();
            IGame game = (IGame)Activator.CreateInstance(GameType)!;
            for (int i = 0; i < NRandomMoves; ++i)
            {
                game.MakeMove(game.GetRandomMove());
            }
            return game;
        }
    }
}