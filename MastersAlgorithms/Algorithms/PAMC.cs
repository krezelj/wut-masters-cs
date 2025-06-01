// using System;
// using System.Collections.Generic;
// using System.ComponentModel;
// using System.Diagnostics;
// using System.Linq;
// using System.Threading.Tasks;
// using MastersAlgorithms.Games;

// namespace MastersAlgorithms.Algorithms
// {
//     public class PAMC : IAlgorithm
//     {
//         long _nodes;
//         Stopwatch _sw;
//         IGame? _game;
//         float _time;
//         float _value;

//         IGame[]? _simulations;
//         int[]? _simulationIdxs;

//         float[]? _finalValues;
//         float[]? _resultValues;
//         float[]? _estimateValues;


//         Func<IGame[], (IMove[] moves, float[] values)> _policy;
//         int _simulationsPerMove;
//         float _alpha;
//         float _lambda;
//         bool _verbose;

//         public PAMC(
//             Func<IGame[], (IMove[], float[])> policy,
//             int simulationsPerMove = 16,
//             float alpha = 1.0f,
//             float lambda = 1.0f,
//             bool verbose = false
//         )
//         {
//             _sw = new Stopwatch();

//             _policy = policy;
//             _simulationsPerMove = simulationsPerMove;
//             _alpha = alpha;
//             _lambda = lambda;
//             _verbose = verbose;
//         }

//         public string GetDebugInfo()
//         {
//             return string.Format("Nodes {0,11} | {1,8:F2}kN/s | {2,6}ms | Eval {3,5:F3}",
//                 _nodes, _nodes / _time, _time, _value);
//         }

//         public IMove? GetMove(IGame game)
//         {
//             _nodes = 0;
//             _sw.Restart();

//             _game = game.Copy(disableZobrist: true);

//             var rootMoves = _game.GetMoves();
//             int nSimulations = rootMoves.Length * _simulationsPerMove;
//             _simulationIdxs = Enumerable.Range(0, nSimulations).ToArray();
//             _finalValues = new float[rootMoves.Length];
//             _resultValues = new float[nSimulations];
//             _estimateValues = new float[nSimulations];

//             _simulations = new IGame[nSimulations];
//             for (int i = 0; i < rootMoves.Length; i++)
//             {
//                 for (int j = 0; j < _simulationsPerMove; j++)
//                 {
//                     int idx = i * _simulationsPerMove + j;
//                     _simulations[idx] = _game.Copy();
//                     _simulations[idx].MakeMove(rootMoves[i]);
//                 }
//             }

//             Search();
//             int bestIdx = Utils.ArgMax(_finalValues);
//             _value = _finalValues[bestIdx];

//             _time = _sw.ElapsedMilliseconds;
//             if (_verbose)
//                 Console.WriteLine(GetDebugInfo());

//             return rootMoves[bestIdx];
//         }

//         private void Search()
//         {
//             while (true)
//             {
//                 if (_simulations!.Length == 0)
//                     break;

//                 (var moves, var values) = _policy(_simulations!);
//                 MakeMoves(moves);
//                 UpdateValues(values);
//                 UpdateSimulations();
//             }
//             CalculateValues();
//         }

//         private void UpdateSimulations()
//         {
//             int nonTerminalCount = 0;
//             IGame[] newSimulations = new IGame[_simulations!.Length];
//             int[] newSimulationIdxs = new int[_simulationIdxs!.Length];

//             for (int i = 0; i < _simulationIdxs.Length; i++)
//             {
//                 if (_simulations[i].IsOver)
//                 {
//                     FinishSimulation(i);
//                     continue;
//                 }

//                 // non terminal
//                 newSimulations[nonTerminalCount] = _simulations[i];
//                 newSimulationIdxs[nonTerminalCount] = _simulationIdxs[i];
//                 nonTerminalCount++;
//             }

//             Array.Resize(ref newSimulations, nonTerminalCount);
//             Array.Resize(ref newSimulationIdxs, nonTerminalCount);

//             _simulations = newSimulations;
//             _simulationIdxs = newSimulationIdxs;
//         }

//         private void MakeMoves(IMove[] moves)
//         {
//             for (int i = 0; i < moves.Length; i++)
//             {
//                 _nodes++;
//                 _simulations![i].MakeMove(moves[i], updateMove: false);
//             }
//         }

//         private void UpdateValues(float[] values)
//         {
//             for (int i = 0; i < _simulationIdxs!.Length; i++)
//             {
//                 _estimateValues![_simulationIdxs[i]] *= 1 - _alpha;
//                 _estimateValues![_simulationIdxs[i]] += _alpha * values[i];
//             }
//         }

//         private void FinishSimulation(int idx)
//         {
//             var leaf = _simulations![idx];

//             float value = MathF.Sign(leaf.Evaluate());
//             if (leaf.Player != _game!.Player)
//                 value = -value;

//             _resultValues![idx] = value;
//         }

//         private void CalculateValues()
//         {
//             for (int i = 0; i < _finalValues!.Length; i++)
//             {
//                 for (int j = 0; j < _simulationsPerMove; j++)
//                 {
//                     int idx = i * _simulationsPerMove + j;
//                     _finalValues[i] += (1 - _lambda) * _estimateValues![idx] + _lambda * _resultValues![idx];
//                 }
//                 _finalValues[i] /= _simulationsPerMove;
//             }
//         }

//         public static PAMC GetAgentPAMC(
//             Agent agent,
//             int simulationsPerMove = 16,
//             float alpha = 1.0f,
//             float lambda = 1.0f,
//             float temperature = 1.0f,
//             bool verbose = false)
//         {
//             AgentControllerPAMC ac = new AgentControllerPAMC(agent, temperature);
//             return new PAMC(
//                 policy: ac.Policy,
//                 simulationsPerMove: simulationsPerMove,
//                 alpha: alpha,
//                 lambda: lambda,
//                 verbose: verbose
//             );
//         }

//     }

//     public class AgentControllerPAMC
//     {
//         private Agent _agent;
//         private float _temperature;

//         public AgentControllerPAMC(Agent agent, float temperature = 1.0f)
//         {
//             _agent = agent;
//             _temperature = temperature;
//         }

//         public (IMove[] moves, float[] values) Policy(IGame[] states)
//         {
//             int stateCount = states.Length;
//             int nPossibleMoves = states[0].PossibleMovesCount;

//             IMove[][] allMoves = new IMove[stateCount][];
//             bool[] actionMasks = new bool[nPossibleMoves * stateCount];
//             for (int i = 0; i < stateCount; i++)
//             {
//                 bool[] currentActionMasks = states[i].GetActionMasks(out allMoves[i]);
//                 Array.Copy(currentActionMasks, 0, actionMasks, i * nPossibleMoves, nPossibleMoves);
//             }

//             float[] obs = Utils.GetFlatObservations(states, _agent.ActorMode);
//             var output = _agent.Policy.GetMaskedProbsAndValues(obs, actionMasks, batchCount: stateCount);

//             float[][] probs = new float[stateCount][];
//             for (int i = 0; i < stateCount; i++)
//             {
//                 probs[i] = new float[nPossibleMoves];
//                 Array.Copy(output.probs, i * nPossibleMoves, probs[i], 0, nPossibleMoves);
//             }

//             IMove[] moves = new IMove[stateCount];
//             for (int i = 0; i < stateCount; i++)
//             {
//                 int sampledMoveIdx = Utils.Sample(probs[i]);
//                 moves[i] = states[i].GetMoveFromAction(sampledMoveIdx);
//                 // moves[i] = allMoves[i][];
//             }

//             return (moves, output.values);
//         }
//     }
// }