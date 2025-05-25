using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Declarations
{
    public class Belot
    {
        public Suit Suit { get; set; }
        public bool Declared { get; set; }
        public bool Declarable { get; set; }

        public Belot(Suit suit, bool declared, bool declarable)
        {
            Suit = suit;
            Declared = declared;
            Declarable = declarable;
        }

        public bool Equals(Belot other)
        {
            return Suit == other.Suit && Declared == other.Declared && Declarable == other.Declarable;
        }
    }
}
