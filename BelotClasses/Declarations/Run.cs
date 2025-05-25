using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Declarations
{
    public class Run
    {
        public Run(int length, Suit suit, Rank strength, bool declared, bool declarable)
        {
            Length = length;
            Suit = suit;
            Rank = strength;
            Declared = declared;
            Declarable = declarable;
        }

        public int Length { get; set; } // 3 = Tierce, 4 = Quarte, 5 = Quint
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }
        public bool Declared { get; set; }
        public bool Declarable { get; set; }
    }
}
