using BelotWebApp.BelotClasses.Agents;
using BelotWebApp.BelotClasses.Observers;
using BelotWebApp.BelotClasses.Players;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace BelotWebApp.BelotClasses.Training
{
    public class BelotGameSimulator
    {
        //private readonly ILogger _log;
        //public BelotGameSimulator(ILogger log)
        private readonly SimulationResult _result;

        public BelotGameSimulator(SimulationResult result)
        {
            _result = result;
            //_log = log;
        }

        public void SimulateGames(int populationSize, int numGenerations)
        {
            Stopwatch stopwatch = new();

            float bestFitness = -100f;

            int inputSize;
            int hiddenSize;
            int outputSize;

            (inputSize, hiddenSize, outputSize) = GetNNSize();

            var population = new PopulationManager(populationSize, inputSize, hiddenSize, outputSize);

            int parallelism = Environment.ProcessorCount;

            _result.TotalGenerations = numGenerations;

            for (int generation = 0; generation < numGenerations; generation++)
            {
                _result.CurrentGeneration = generation + 1;

                // Reset fitness before each generation
                foreach (var agent in population.Agents)
                {
                    agent.Fitness = 0;
                    agent.GamesPlayed = 0;
                }

                var fitnesses = new ConcurrentBag<float>();

                stopwatch.Restart();

                Parallel.ForEach(population.Agents, new ParallelOptions { MaxDegreeOfParallelism = parallelism }, agent =>
                {
                    var game = CreateGame(agent);
                    var observer = new TrainingBelotObserver(game);
                    var engine = new BelotGameEngine(game, observer);

                    //_log?.Information($"[SimulateGames] Starting simulation {i + 1}/{count}");

                    engine.GameController().GetAwaiter().GetResult(); // blocking call for now

                    fitnesses.Add(agent.Fitness);
                    //Debug.Print($"[SimulateGames] Sim {i + 1} - EW: {game.EWTotal} / NS: {game.NSTotal} - fitness: {game.Players[0].Agent.Fitness}");

                    //_log?.Information($"[SimulateGames] Finished simulation {i + 1}/{count}");
                });

                stopwatch.Stop();

                population.Evolve(inputSize, hiddenSize, outputSize);

                float localBestFitness = fitnesses.Max();
                if (localBestFitness > _result.BestFitness)
                {
                    _result.BestFitness = localBestFitness;
                }

                _result.IsComplete = generation == numGenerations - 1;

                Debug.Print($"gen {generation + 1} - avg: {fitnesses.Average()} - best: {fitnesses.Max()}"); // [SimulateGames]
                Debug.Print($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms");
            }
            Debug.Print($"best: {bestFitness}");
        }

        private (int, int, int) GetNNSize()
        {
            var game = new BelotGame([new(), new(), new(), new()], Guid.NewGuid().ToString(), null);
            int inputs = AgentAdvanced.BuildNNInputVector(game).Length;
            return (inputs, 128, 8);
        }

        private BelotGame CreateGame(AgentAdvanced agent)
        {
            var game = new BelotGame([new(), new(), new(), new()], Guid.NewGuid().ToString(), null);

            Player agentPlayer = new() { PlayerType = PlayerType.Advanced, Agent = agent };
            Player[] players = [agentPlayer, new("bot2", "", PlayerType.Basic), new("bot3", "", PlayerType.Basic), new("bot4", "", PlayerType.Basic)];

            game.Players = players;

            return game;
        }
    }
}
