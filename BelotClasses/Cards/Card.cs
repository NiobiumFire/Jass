namespace BelotWebApp.BelotClasses.Cards
{
    public class Card
    {
        public Card()
        {

        }

        public Card(Suit suit, Rank strength)
        {
            Suit = suit;
            Rank = strength;
            Played = false;
        }

        public Suit? Suit { get; set; }
        public Rank? Rank { get; set; }
        public bool Played { get; set; }

        public bool IsNull()
        {
            return Suit == null || Rank == null;
        }

        public Card Clone()
        {
            return new Card()
            {
                Suit = Suit,
                Rank = Rank
            };
        }
    }
}
