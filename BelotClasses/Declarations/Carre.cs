using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Declarations
{
    public class Carre : Declaration
    {
        public Carre() : base(null, -1) // for deserialization only
        {
            Type = DeclarationType.Carre;
        }

        public Carre(BelotGame game, int player, Rank rank) : base(game, player)
        {
            Type = DeclarationType.Carre;
            Rank = rank;
        }

        public Rank Rank { get; set; }

        public override bool IsDeclarable
        {
            get
            {
                return !Declared && game != null && game.NumCardsPlayed < 5;
            }
        }
    }
}
