namespace BelotWebApp.BelotClasses.RoundSummary
{
    public sealed class RoundSummaryInfo
    {
        public int[] TrickPoints { get; init; }
        public int[] DeclarationPoints { get; init; }
        public int[] BelotPoints { get; init; }
        public string[] Result { get; init; }
        public int[] RoundPoints { get; init; }
        public bool EWFirst { get; init; }
        public Guid RoundToken { get; init; }
        public int RoundSummaryDelay { get; init; }
        public int RequiredContinueVotes { get; init; }
        public bool VoteToContinueDisabled { get; init; }
    }
}
