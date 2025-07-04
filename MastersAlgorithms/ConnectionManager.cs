using System.Globalization;
using System.Text;
using MastersAlgorithms.Algorithms;
using MastersAlgorithms.Games;

namespace MastersAlgorithms
{
    class ConnectionManager
    {
        private string[] _args;
        private Dictionary<string, IGame> _games;
        private Dictionary<string, IAlgorithm> _algorithms;
        private bool _verbose;

        private static readonly char[] _chars =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

        public ConnectionManager(string[] args)
        {
            _args = args;
            _games = new Dictionary<string, IGame>();
            _algorithms = new Dictionary<string, IAlgorithm>();
            _verbose = GetSwitch("verbose");
        }

        private string Get(string key, string? defaultValue = null)
        {
            var index = Array.IndexOf(_args, "--" + key);

            if (index >= 0 && _args.Length > index)
            {
                return _args[index + 1];
            }
            else if (defaultValue != null)
            {
                return defaultValue;
            }

            throw new KeyNotFoundException($"{key} not found in options");
        }

        private bool GetSwitch(string key)
        {
            return _args.Contains("--" + key);
        }

        private string GetHash()
        {
            const int LENGTH = 10;
            while (true)
            {
                var result = new StringBuilder(LENGTH);
                for (int i = 0; i < LENGTH; ++i)
                {
                    result.Append(_chars[Utils.RNG.Next(_chars.Length)]);
                }
                var hash = result.ToString();
                if (!_games.ContainsKey(hash) && !_algorithms.ContainsKey(hash))
                    return hash;
            }
        }

        public void Run()
        {
            while (true)
            {
                string input = Console.ReadLine()!; // todo check when null
                if (input == "exit")
                    return;
                _args = input.Split(' ');
                try
                {
                    string response = RunCommand();
                    Console.WriteLine(response);
                }
                catch (Exception e)
                {
                    Console.WriteLine("exception");
                    Console.WriteLine(e);
                    Console.WriteLine("end");
                }
            }
        }

        private string RunCommand()
        {
            string command = Get("command");
            switch (command)
            {
                case "addGame":
                    return AddGame();
                case "removeGame":
                    _games.Remove(Get("hashName"));
                    break;
                case "addAlgorithm":
                    return AddAlgorithm();
                case "removeAlgorithm":
                    _algorithms.Remove(Get("hashName"));
                    break;
                case "getMove":
                    return GetMove();
                case "getMoves":
                    return GetMoves();
                case "getRandomMove":
                    return GetRandomMove();
                case "makeMove":
                    return MakeMove();
                case "undoMove":
                    UndoMove();
                    break;
                case "isOver":
                    return _games[Get("game")].IsOver.ToString();
                case "result":
                    return _games[Get("game")].Result.ToString();
                case "evaluate":
                    return _games[Get("game")].Evaluate().ToString();
                case "getString":
                    return _games[Get("game")].ToString()!;
                case "copy":
                    return Copy();
                case "runGame":
                    return RunGame();
                case "runMatch":
                    return RunMatch();
                default:
                    throw new ArgumentException($"Unkown Command: {command}");
            }
            return "OK";
        }

        private string AddGame()
        {
            string gameName = Get("name");
            string state = Get("state", "");
            string hashName = GetHash();
            IGame game = GetGameByName(gameName, state);
            _games.Add(hashName, game);
            return hashName;
        }

        private IGame GetGameByName(string name, string state = "")
        {
            switch (name)
            {
                case "checkers":
                    if (state != "")
                        throw new NotImplementedException("BitChecers has no constructor which accepts `state`");
                    else
                        return new BitCheckers(useZobrist: GetSwitch("useZobrist"));
                case "connectFour":
                    if (state != "")
                        throw new NotImplementedException("BitConnectFour has no constructor which accepts `state`");
                    else
                        return new BitConnectFour(useZobrist: GetSwitch("useZobrist"));
                case "othello":
                    if (state != "")
                        return new BitOthello(state, useZobrist: GetSwitch("useZobrist"));
                    else
                        return new BitOthello(useZobrist: GetSwitch("useZobrist"));
                default:
                    throw new ArgumentException($"Invalid game name: {name}");
            }
        }

        private Type GetGameTypeByName(string name)
        {
            switch (name)
            {
                case "checkers":
                    return typeof(BitCheckers);
                case "connectFour":
                    return typeof(BitConnectFour);
                case "othello":
                    return typeof(BitOthello);
                default:
                    throw new ArgumentException($"Invalid game name: {name}");
            }
        }

        private string AddAlgorithm()
        {
            string algorithmName = Get("name");
            string hashName = GetHash();
            IAlgorithm algorithm = GetAlgorithmByName(algorithmName);
            _algorithms.Add(hashName, algorithm);
            return hashName;
        }

        private IAlgorithm GetAlgorithmByName(string name)
        {
            switch (name)
            {
                case "bogo":
                    return new Bogo();
                case "edax":
                    return new Edax(
                        level: int.Parse(Get("level")),
                        directory: Get("modelDirectory", Path.Join("external")),
                        bookUsage: GetSwitch("bookUsage"),
                        stateful: GetSwitch("stateful"),
                        verbose: GetSwitch("verbose")
                    );
                case "minimax":
                    return new MinimaxFast(
                        depth: int.Parse(Get("depth")),
                        evalFunc: GetEvalFunc(),
                        sortFunc: (game, moves, idx) => game.SortMoves(moves, idx),
                        verbose: false
                    );
                case "mcts":
                    return MCTSHybrid.GetDefaultMCTS(maxIters: int.Parse(Get("maxIters")), verbose: false);
                case "agent":
                    return GetAgent();
                case "mctsBatch":
                    return MCTSBatch.GetAgentMCTS(
                        maxIters: int.Parse(Get("maxIters")),
                        agent: GetAgent(),
                        batchSize: int.Parse(Get("batchSize")),
                        lambda: float.Parse(Get("lam", "0.0"), CultureInfo.InvariantCulture),
                        noiseAlpha: float.Parse(Get("noiseAlpha", "0.9"), CultureInfo.InvariantCulture),
                        noiseWeight: float.Parse(Get("noiseWeight", "0.25"), CultureInfo.InvariantCulture),
                        nVirtual: int.Parse(Get("nVirtual", "1")),
                        preserveSubtree: GetSwitch("preserveSubtree"),
                        c: float.Parse(Get("cPuct", "2.0"), CultureInfo.InvariantCulture),
                        temperature: float.Parse(Get("temperature", "10.0"), CultureInfo.InvariantCulture),
                        deterministicSelection: !GetSwitch("stochasticSelection"),
                        verbose: false
                    );
                case "minimaxHybrid":
                    return MinimaxFast.GetAgentMinimax(
                        depth: int.Parse(Get("depth")),
                        agent: GetAgent(),
                        temperature: float.Parse(Get("temperature", "10.0"), CultureInfo.InvariantCulture),
                        probThreshold: float.Parse(Get("floatThreshold", "0.01"), CultureInfo.InvariantCulture),
                        verbose: false
                    );
                default:
                    throw new ArgumentException($"Invalid algorithm name: {name}");
            }
        }

        private Func<IGame, float> GetEvalFunc()
        {
            string evalFuncName = Get("evalFuncName", "standard");
            switch (evalFuncName)
            {
                case "standard":
                    return g => g.Evaluate();
                case "naive":
                    return g => g.NaiveEvaluate();
                case "mobility":
                    return g => g.MobilityEvaluate();
                case "random":
                    return g => g.RandomEvaluate();
                default:
                    throw new ArgumentException($"Invalid eval func name: {evalFuncName}");
            }
        }

        private Agent GetAgent()
        {
            return new Agent(
                modelDirectory: Get("modelDirectory", Path.Join("models")),
                actorMode: Utils.GetObservationModeByName(Get("actorMode", "flat")),
                criticMode: Utils.GetObservationModeByName(Get("criticMode", "flat")),
                deterministic: GetSwitch("deterministic"),
                device: Get("device", "cpu"),
                expectedBatchCount: int.Parse(Get("expectedBatchCount", "1")),
                unified: GetSwitch("unified")
            );
        }

        private string GetMove()
        {
            string gameHashName = Get("game", "");
            IGame game;
            if (gameHashName != "")
            {
                game = _games[Get("game")];
            }
            else
            {
                game = GetGameByName(Get("name"), Get("state"));
            }
            IMove move = _algorithms[Get("algorithm")].GetMove(game)!;
            string debugMsg = _verbose ? _algorithms[Get("algorithm")].GetDebugInfo() : "";
            return $"{move};{debugMsg}";
        }

        private string GetMoves()
        {
            var moves = _games[Get("game")].GetMoves();
            return string.Join(";", moves.Select(m => m.ToString()));
        }

        private string GetRandomMove()
        {
            return _games[Get("game")].GetRandomMove().ToString()!;
        }

        private string MakeMove()
        {
            IMove move = _games[Get("game")].GetMoveFromString(Get("move"));
            _games[Get("game")].MakeMove(move);
            return move.ToString()!;
        }

        private void UndoMove()
        {
            IMove move = _games[Get("game")].GetMoveFromString(Get("move"));
            _games[Get("game")].UndoMove(move);
        }

        private string Copy()
        {
            string hashName = GetHash();
            var gameCopy = _games[Get("game")].Copy();
            _games.Add(hashName, gameCopy);
            return hashName;
        }

        private string RunGame()
        {
            var game = _games[Get("game")];
            var players = Get("players").Split(";").Select(h => _algorithms[h]).ToArray();
            int firstPlayerIndex = int.Parse(Get("firstPlayerIdx", "0"));
            var gameRunner = new GameRunner(game, players, firstPlayerIndex);
            gameRunner.Run();
            return gameRunner.GetDebugInfo();
        }

        private string RunMatch()
        {
            var gameType = GetGameTypeByName(Get("gameType"));
            var players = Get("players").Split(";").Select(h => _algorithms[h]).ToArray();
            int nGames = int.Parse(Get("nGames"));
            bool mirrorGames = GetSwitch("mirrorGames");
            int nRandomMoves = int.Parse(Get("nRandomMoves"));

            var mm = new MatchManager(players, gameType, nGames, mirrorGames, nRandomMoves);
            mm.Run();
            return mm.GetDebugInfo();
        }

    }
}