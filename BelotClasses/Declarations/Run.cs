using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Declarations
{
    public class Run : Declaration
    {
        public Run() : base(null, -1) // for deserialization only
        {
            Type = DeclarationType.Run;
        }

        public Run(BelotGame game, int player, int length, Suit suit, Rank strength) : base(game, player)
        {
            Type = DeclarationType.Run;
            Length = length;
            Suit = suit;
            Rank = strength;
        }

        public bool IsValid { get; private set; }

        public int Length { get; set; } // 3 = Tierce, 4 = Quarte, 5 = Quint
        public Suit Suit { get; set; }
        public Rank Rank { get; set; }

        public override bool IsDeclarable
        {
            get
            {
                return !Declared && game != null && game.NumCardsPlayed < 5;
            }
        }

        public Run TruncateAndValidate() // reduce runs which overlap a carre, where this still rewards points
        {
            //int index = Array.IndexOf(game.Declarations.Where(r => r is Run && r.Player == Player).ToArray(), this);

            //if (index == -1)
            //{
            //    isValid = false;
            //    return this;
            //}

            var carres = game.Declarations.Where(c => c is Carre && c.Player == Player);

            if (carres.Count() != 1) // if zero carres: no overlap, if two: can't have any runs
            {
                IsValid = true;
                return this;
            }

            Carre carre = carres.First() as Carre;

            Rank upper = Rank;
            Rank lower = upper - Length + 1;

            if (carre.Rank >= lower && carre.Rank <= upper)
            {
                if (Length == 3)
                {
                    IsValid = false;
                }
                else // try truncate run
                {
                    bool first = carre.Rank == lower;
                    bool second = carre.Rank == lower + 1;
                    bool secondLast = carre.Rank == upper - 1;
                    bool last = carre.Rank == upper;
                    if (first || last) // Quarte becomes Tierce, Quint becomes Quarte
                    {
                        Length -= 1;
                        // if first, strength remains the same, reducing length by 1 is sufficient
                        if (last)
                        {
                            Rank -= 1;
                        }
                        IsValid = true;
                    }
                    else if (second || secondLast)
                    {
                        if (Length == 4) // Quarte invalidated
                        {
                            IsValid = false;
                        }
                        else // Quint becomes Tierce
                        {
                            Length -= 2;
                            // if second, strength remains the same, reducing length by 2 is sufficient
                            if (secondLast)
                            {
                                Rank -= 2;
                            }
                            IsValid = true;
                        }
                    }
                    else // Run is a Quint, and Carre is in the middle, therefore no truncation is possible
                    {
                        IsValid = false;
                    }
                }
            }
            else // carre rank does not lie within the run
            {
                IsValid = true;
            }

            return this;
        }
    }
}
