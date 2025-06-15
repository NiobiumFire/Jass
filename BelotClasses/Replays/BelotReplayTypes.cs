using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Replays
{
    public record ReplayEmote(int Player, string? Emote);
    public record ReplayTableCard(int Player, Card? Card);
    public record ReplayHandCard(int Player, int Index, Card? Card);
}
