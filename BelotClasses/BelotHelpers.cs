using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using Microsoft.AspNetCore.SignalR;
using Serilog;

namespace BelotWebApp.BelotClasses
{
    public class BelotCard
    {
        public int Suit { get; set; } // 1=C, 2=D, 3=H, 4=S
        public int RankNT { get; set; } // No trumps rank -> 7=0, 8=1, 9=2, J=3, Q=4, K=5, 10=6, A=7
        public int PointsNT { get; set; } // No trumps point value -> 7=0, 8=0, 9=0, J=2, Q=3, K=4, 10=10, A=11
        public int RankT { get; set; } // Trumps rank -> 7=8, 8=9, Q=10, K=11, 10=12, A=13, 9=14, J=15
        public int PointsT { get; set; } // Trumps point value -> 7=0, 8=0, 9=14, J=20, Q=3, K=4, 10=10, A=11
        public int RankE { get; set; } // rank for determining extras -> 7, 8, 9, 10, J, Q, K, A
        public string Image { get; set; } // image to be displayed on the table
    }

    public class BelotHelpers
    {
        public static readonly Rank[] runRanks = [Rank.Seven, Rank.Eight, Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King, Rank.Ace];
        public static readonly Rank[] nonTrumpRanks = [Rank.Seven, Rank.Eight, Rank.Nine, Rank.Jack, Rank.Queen, Rank.King, Rank.Ten, Rank.Ace];
        public static readonly Rank[] trumpRanks = [Rank.Seven, Rank.Eight, Rank.Queen, Rank.King, Rank.Ten, Rank.Ace, Rank.Nine, Rank.Jack];
        public static readonly int[] offSuitNonTrumpStrength = [1, 2, 3, 7, 4, 5, 6, 8]; // to help bots choose a card when losing
        public static readonly int[] offSuitTrumpStrength = [1, 2, 7, 5, 8, 3, 4, 6]; // to help bots choose a card when losing
        public static readonly int[] onSuitNonTrumpStrength = [9, 10, 11, 15, 12, 13, 14, 16];
        public static readonly int[] onSuitTrumpStrength = [17, 18, 23, 21, 24, 19, 20, 22];
        public static readonly List<string> declarationDisplayOrder = ["Belot", "Carre", "Tierce", "Quarte", "Quint"];

        public static bool IsSuit(Call call)
        {
            return call <= (Call)Suit.Spades && call >= Call.Pass;
        }

        public static string GetSuitNameFromNumber(Call call)
        {
            return call switch
            {
                Call.NoTrumps => "No Trumps",
                Call.AllTrumps => "All Trumps",
                _ => call.ToString(),
            };
        }

        public static string GetCardRankFromNumber(Rank rank)
        {
            string[] rankNames = { "7", "8", "9", "10", "J", "Q", "K", "A" };
            return rankNames[(int)rank];
        }

        public static string GetRunNameFromLength(int length)
        {
            string[] types = { "Tierce", "Quarte", "Quint" };
            return types[length - 3];
        }

        public static int GetCardNumber(Card card) // 0 for already played, 1-32 for the possible cards
        {
            if (card.Played || card.Suit is not Suit suit || card.Rank is not Rank rank)
            {
                return 0;
            }
            return ((int)suit - 1) * 8 + (int)rank + 1;
        }

        public static int GetCardStrength(Card card, Call roundSuit, Suit? trickSuit)
        {
            int strength;

            var ranks = Enum.GetValues(typeof(Rank));
            int rankIndex = Array.IndexOf(ranks, card.Rank);

            if (IsSuit(roundSuit)) // C,D,H,S
            {
                if ((Suit)roundSuit == card.Suit) // if card is a trump
                {
                    strength = onSuitTrumpStrength[rankIndex];
                }
                else if (trickSuit == card.Suit)  // if card is in lead-suit and is not a trump
                {
                    strength = onSuitNonTrumpStrength[rankIndex];
                }
                else // if card is off-suit, and is a discard
                {
                    strength = offSuitNonTrumpStrength[rankIndex];
                }
            }
            else if (roundSuit == Call.NoTrumps)
            {
                if (trickSuit == card.Suit) // if the suit was followed in A
                {
                    strength = onSuitNonTrumpStrength[rankIndex];
                }
                else
                {
                    strength = offSuitNonTrumpStrength[rankIndex];
                }
            }
            else  // if the suit was followed in J
            {
                if (trickSuit == card.Suit)
                {
                    strength = onSuitTrumpStrength[rankIndex];
                }
                else
                {
                    strength = offSuitTrumpStrength[rankIndex];
                }
            }

            return strength;
        }

        public static bool FiveUnderNine(List<Card> hand)
        {
            return hand.Count(c => c.Rank < Rank.Nine) == 5;
        }

        public static (List<string>, List<string>) GetDeclarationMessagesAndEmotes(List<Declaration> declarations, BelotGame game)
        {
            List<string> messages = [];
            List<string> emotes = [];

            foreach (var declaration in declarations.OfType<Belot>())
            {
                messages.Add(game.GetDisplayName(game.Turn) + " called a Belot.");
                emotes.Add("Belot");
            }
            foreach (var declaration in declarations.OfType<Carre>())
            {
                messages.Add(game.GetDisplayName(game.Turn) + " called a Carre.");
                emotes.Add("Carre");
            }
            foreach (var declaration in declarations.OfType<Run>())
            {
                string runName = GetRunNameFromLength(declaration.Length);
                messages.Add(game.GetDisplayName(game.Turn) + " called a " + runName + ".");
                emotes.Add(runName);
            }

            if (emotes.Count > 0)
            {
                emotes = emotes.OrderBy(i =>
                {
                    int index = declarationDisplayOrder.IndexOf(i);
                    return index == -1 ? int.MaxValue : declarationDisplayOrder.IndexOf(i);
                }).ToList();
            }

            return (messages, emotes);
        }
    }
}