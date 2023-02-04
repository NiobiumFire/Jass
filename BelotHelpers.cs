using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatWebApp
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

    public class Run
    {
        public Run(int length, int suit, int strength, bool declared)
        {
            Length = length;
            Suit = suit;
            Strength = strength;
            Declared = declared;
        }

        public int Length { get; set; } // 3 = Tierce, 4 = Quart, 5 = Quint
        public int Suit { get; set; } // 1=C, 2=D, 3=H, 4=S
        public int Strength { get; set; } // 9 high=0, A high=5
        public bool Declared { get; set; }
    }

    public class Carre
    {
        public Carre(int rank, bool declared)
        {
            Rank = rank;
            Declared = declared;
        }

        public int Rank { get; set; }
        public bool Declared { get; set; }
    }

    public class Belot
    {
        public Belot(int suit, bool declared, bool declarable)
        {
            Suit = suit;
            Declared = declared;
            Declarable = declarable;
        }

        public int Suit { get; set; }
        public bool Declared { get; set; }
        public bool Declarable { get; set; }

        public bool Equals(Belot other)
        {
            return Suit == other.Suit && Declared == other.Declared && Declarable == other.Declarable;
        }
    }

    public class CardPower
    {
        public int DetermineCardPower(string card, int roundSuit, int trickSuit)
        {
            int value = 0;

            int[] nontrump = { 1, 2, 3, 7, 4, 5, 6, 8 };
            int[] trump = { 9, 10, 15, 13, 16, 11, 12, 14 };

            int suit = Int32.Parse(card.Substring(1, 1));
            int rank = Int32.Parse(card.Substring(3, 2)) - 6;
            if (roundSuit < 5) // C,D,H,S
            {
                if (roundSuit == suit) // if a trump was played
                {
                    value = trump[rank];
                }
                else if (trickSuit == suit)  // if the suit was followed
                {
                    value = nontrump[rank];
                }
                else
                {
                    value = 0;
                }
            }
            else if (roundSuit == 5 && trickSuit == suit)  // if the suit was followed in A
            {
                value = nontrump[rank];
            }
            else if (trickSuit == suit)  // if the suit was followed in J
            {
                value = trump[rank];
            }

            return value;
        }
    }
}