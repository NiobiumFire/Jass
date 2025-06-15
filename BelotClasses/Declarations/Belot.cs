using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Declarations
{
    public class Belot : Declaration
    {
        public Belot() : base(null, -1) // for deserialization only
        {
            Type = DeclarationType.Belot;
        }

        public Belot(BelotGame game, int player, Suit suit) : base(game, player)
        {
            Type = DeclarationType.Belot;
            Suit = suit;
            Declared = false;
        }

        public Suit Suit { get; set; }

        public override bool IsDeclarable
        {
            get
            {
                if (Declared)
                {
                    return false;
                }

                if (game == null)
                {
                    return false;
                }

                if (!BelotHelpers.IsSuit(game.RoundCall) && !(game.RoundCall == Call.AllTrumps && Suit == game.TrickSuit))
                {
                    return false;
                }

                var queen = GetKingOrQueen(Rank.Queen);
                var king = GetKingOrQueen(Rank.King);

                if (queen == null || king == null)
                {
                    return false;
                }

                bool queenIsPlayedNow = game.TableCards[game.Turn] == queen;
                bool kingIsPlayedNow = game.TableCards[game.Turn] == king;

                bool queenUnplayed = !queen.Played;
                bool kingUnplayed = !king.Played;

                return
                    //(queenUnplayed && kingUnplayed) || // neither played yet
                    (queenIsPlayedNow && kingUnplayed) || // queen just played, king not yet
                    (kingIsPlayedNow && queenUnplayed); // king just played, queen not yet
            }
        }

        public bool Unplayed()
        {
            var queen = GetKingOrQueen(Rank.Queen);
            var king = GetKingOrQueen(Rank.King);

            return queen != null && king != null && !queen.Played && !king.Played;
        }

        private Card? GetKingOrQueen(Rank rank)
        {
            return game.Hand.SelectMany(h => h).FirstOrDefault(card => card.Suit == Suit && card.Rank == rank);
        }
    }
}
