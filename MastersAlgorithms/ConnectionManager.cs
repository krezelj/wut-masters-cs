using System.Runtime.CompilerServices;
using System.Text;
using MastersAlgorithms.Algorithms;
using MastersAlgorithms.Games;

namespace MastersAlgorithms
{
    // public class CommandLineParser
    // {
    //     private readonly List<string> _args;
    //     private Dictionary<string, string> _defaultValues;

    //     public CommandLineParser(string[] args)
    //     {
    //         _args = args.ToList();
    //         _defaultValues = new Dictionary<string, string>();
    //     }

    //     public void AddDefaultValue(string key, string value)
    //     {
    //         if (!_defaultValues.ContainsKey(key))
    //         {
    //             _defaultValues.Add(key, value);
    //         }
    //         else
    //         {
    //             _defaultValues[key] = value;
    //         }
    //     }

    //     public string Get(string key, string? defaultValue = null)
    //     {
    //         var index = _args.IndexOf("--" + key);

    //         if (index >= 0 && _args.Count > index)
    //         {
    //             return _args[index + 1];
    //         }
    //         else if (_defaultValues.ContainsKey(key))
    //         {
    //             return _defaultValues[key];
    //         }
    //         else if (defaultValue != null)
    //         {
    //             return defaultValue;
    //         }

    //         throw new KeyNotFoundException($"{key} not found in options");
    //     }

    //     public bool GetSwitch(string key)
    //     {
    //         return _args.Contains("--" + key);
    //     }
    // }

    // public class ConnectionManager
    // {
    //     private CommandLineParser _cml;

    //     public ConnectionManager(string[] args)
    //     {
    //         _cml = new CommandLineParser(args);
    //     }

    //     public void Run()
    //     {
    //         Utils.SetRNGSeed(int.Parse(_cml.Get("seed", "0")));
    //         IAlgorithm algorithm = CreateAlgorithm();
    //         while (true)
    //         {
    //             string state = Console.ReadLine()!; // todo check when null
    //             if (state == "")
    //                 return;

    //             IGame game = CreateGame(state);
    //             IMove move = algorithm.GetMove(game)!;

    //             string debugMsg = _cml.GetSwitch("verbose") ? algorithm.GetDebugInfo() : "";
    //             string response = $"{move.Index};{debugMsg}";
    //             Console.WriteLine(response);
    //         }
    //     }

    //     public IAlgorithm CreateAlgorithm()
    //     {
    //         string algorithmType = _cml.Get("algorithm");
    //         switch (algorithmType)
    //         {
    //             case "minimax":
    //                 return new Minimax(
    //                     depth: int.Parse(_cml.Get("depth"))
    //                 );
    //             case "mcts":
    //                 return new MCTS(
    //                     maxIters: int.Parse(_cml.Get("maxiters")),
    //                     estimator: MCTS.GetEstimatorByName(_cml.Get("estimator", "ucb"))
    //                 );
    //             default:
    //                 throw new ArgumentException($"Invalid algorithm name: {algorithmType}");
    //         }
    //     }

    //     public IGame CreateGame(string state)
    //     {
    //         string gameType = _cml.Get("game");
    //         switch (gameType)
    //         {
    //             // case "connect-four":
    //             //     return new ConnectFour(state);
    //             case "othello":
    //                 return new BitOthello(state);
    //             default:
    //                 throw new ArgumentException($"Invalid game name: {gameType}");
    //         }
    //     }

    // }

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
                    Console.WriteLine(e);
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
                case "evaluate":
                    return _games[Get("game")].Evaluate().ToString();
                case "getString":
                    return _games[Get("game")].ToString()!;
                case "copy":
                    return Copy();
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
                // case "connect-four":
                //     return new ConnectFour(state);
                case "othello":
                    if (state != "")
                        return new BitOthello(state, useZobrist: Get("zobrist") == "True");
                    else
                        return new BitOthello(useZobrist: Get("zobrist") == "True");
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
                case "minimax":
                    return new MinimaxFast(
                        depth: int.Parse(Get("depth")),
                        verbose: false
                    );
                case "mcts":
                    return new MCTS(
                        maxIters: int.Parse(Get("maxiters")),
                        estimator: MCTS.GetEstimatorByName(Get("estimator", "ucb")),
                        verbose: false
                    );
                default:
                    throw new ArgumentException($"Invalid algorithm name: {name}");
            }
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

    }
}