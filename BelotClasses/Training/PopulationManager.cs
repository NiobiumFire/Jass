using BelotWebApp.BelotClasses.Agents;

namespace BelotWebApp.BelotClasses.Training
{
    public class PopulationManager
    {
        public List<AgentAdvanced> Agents;
        public int Generation;

        public PopulationManager(int populationSize, int inputSize, int hiddenSize, int outputSize)
        {
            Agents = [];
            for (int i = 0; i < populationSize; i++)
            {
                Agents.Add(new(inputSize, hiddenSize, outputSize));
            }
        }

        public void Evolve(int inputSize, int hiddenSize, int outputSize)
        {
            Agents = Agents.OrderByDescending(a => a.Fitness).ToList();
            var top = Agents.Take(10).ToList(); // Keep elites
            var nextGen = new List<AgentAdvanced>(top);

            int numRandomAgents = (int)(Agents.Count * 0.1);

            while (nextGen.Count < Agents.Count - numRandomAgents)
            {
                var parent1 = SelectByFitness();
                var parent2 = SelectByFitness();
                var child = new AgentAdvanced(parent1);
                child.CrossOver(parent2, chance: 20);
                child.Mutate(chance: 30);
                nextGen.Add(child);
            }

            // Add randomised agents to maintain diversity
            for (int i = 0; i < numRandomAgents; i++)
            {
                nextGen.Add(new AgentAdvanced(inputSize, hiddenSize, outputSize));
            }

            Agents = nextGen;
            Generation++;
        }

        AgentAdvanced SelectByFitness()
        {
            double totalFitness = Agents.Sum(a => a.Fitness);
            double pick = AgentAdvanced.rnd.NextDouble() * totalFitness;
            double cumulative = 0;
            foreach (var agent in Agents)
            {
                cumulative += agent.Fitness;
                if (cumulative >= pick)
                {
                    return agent;
                }
            }
            return Agents.Last(); // fallback
        }
    }

}
