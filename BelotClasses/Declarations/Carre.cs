using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Declarations
{
    public class Carre
    {
        public Carre(Rank rank, bool declared)
        {
            Rank = rank;
            Declared = declared;
        }

        public Rank Rank { get; set; }
        public bool Declared { get; set; }
    }
}
