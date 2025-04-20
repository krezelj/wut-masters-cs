using MastersAlgorithms.Algorithms;
using MastersAlgorithms.Games;
using System.Collections.Generic;
using System.Runtime.InteropServices.Marshalling;

namespace MastersAlgorithms
{
    public class CommandLineParser
    {
        private readonly List<string> _args;
        private Dictionary<string, string> _defaultValues;

        public CommandLineParser(string[] args)
        {
            _args = args.ToList();
            _defaultValues = new Dictionary<string, string>();
        }

        public void AddDefaultValue(string key, string value)
        {
            if (!_defaultValues.ContainsKey(key))
            {
                _defaultValues.Add(key, value);
            }
            else
            {
                _defaultValues[key] = value;
            }
        }

        public string Get(string key, string? defaultValue = null)
        {
            var index = _args.IndexOf("--" + key);

            if (index >= 0 && _args.Count > index)
            {
                return _args[index + 1];
            }
            else if (_defaultValues.ContainsKey(key))
            {
                return _defaultValues[key];
            }
            else if (defaultValue != null)
            {
                return defaultValue;
            }

            throw new KeyNotFoundException($"{key} not found in options");
        }

        public bool GetSwitch(bool value)
        {
            return _args.Contains("--" + value);
        }
    }

    public class ConnectionManager
    {
        private CommandLineParser _cml;

        public ConnectionManager(string[] args)
        {
            _cml = new CommandLineParser(args);
        }

        public void Run()
        {
            Utils.SetRNGSeed(int.Parse(_cml.Get("seed", "0")));
            IAlgorithm algorithm = CreateAlgorithm();
            while (true)
            {
                string state = Console.ReadLine()!; // todo check when null
                if (state == "")
                    return;

                IGame game = CreateGame(state);
                IMove move = algorithm.GetMove(game)!;

                Console.WriteLine(move.Index);
            }
        }

        public IAlgorithm CreateAlgorithm()
        {
            string algorithmType = _cml.Get("algorithm");
            switch (algorithmType)
            {
                case "minimax":
                    return new Minimax(
                        depth: int.Parse(_cml.Get("depth"))
                    );
                case "mcts":
                    return new MCTS(
                        maxIters: int.Parse(_cml.Get("maxiters")),
                        estimator: MCTS.GetEstimatorByName(_cml.Get("estimator", "ucb"))
                    );
                default:
                    throw new ArgumentException($"Invalid algorithm name: {algorithmType}");
            }
        }

        public IGame CreateGame(string state)
        {
            string gameType = _cml.Get("game");
            switch (gameType)
            {
                // case "connect-four":
                //     return new ConnectFour(state);
                case "othello":
                    return new BitOthello(state);
                default:
                    throw new ArgumentException($"Invalid game name: {gameType}");
            }
        }

    }
}