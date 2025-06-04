using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Replays
{
    public class BelotStateDiff
    {
        public string[]? Players { get; set; }
        public int[]? Scores { get; set; }
        public int? Dealer { get; set; }
        public Call? RoundCall { get; set; }
        public int? Caller { get; set; }
        public int? Turn { get; set; }

        public string?[]? Emotes { get; set; }
        public List<Card?>? TableCards { get; set; }
        public List<Card?>?[]? Hand { get; set; }

        public static List<Card?>? CloneCards(List<Card?>? cards)
        {
            if (cards == null)
            {
                return null;
            }

            return cards.Select(card => card == null ? null : new Card
            {
                Suit = card.Suit,
                Rank = card.Rank
            }).ToList();
        }

        public static Card?[]? CloneCards(Card?[]? cards)
        {
            if (cards == null)
            {
                return null;
            }

            return cards.Select(card => card == null ? null : new Card
            {
                Suit = card.Suit,
                Rank = card.Rank
            }).ToArray();
        }

        public void AddEmote(string value, int pos)
        {
            Emotes ??= [null, null, null, null];
            Emotes[pos] = value;
        }
    }
}