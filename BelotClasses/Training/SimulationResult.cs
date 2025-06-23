namespace BelotWebApp.BelotClasses.Training
{
    public class SimulationResult
    {
        public int CurrentGeneration { get; set; }
        public int TotalGenerations { get; set; }
        public float BestFitness { get; set; } = -100;
        public float AverageFitness { get; set; } = -100;
        public bool IsComplete { get; set; }
    }
}
