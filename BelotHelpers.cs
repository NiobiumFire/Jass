using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatWebApp
{
    public class Spectator // don't technically need a connection Id because nothing is pushed to spectators exclusively, but it may be needed for Groups when multiple different rooms are implemented
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public Spectator(string username, string connectionId)
        {
            Username = username;
            ConnectionId = connectionId;
        }
    }

    public class Player
    {
        public string Username { get; set; }
        public string ConnectionId { get; set; }
        public bool IsHuman { get; set; }
        public bool IsDisconnected { get; set; }
        public AgentAdvanced Agent { get; set; }

        public Player()
        {
            Username = "";
            ConnectionId = "";
            IsHuman = false;
        }

        public Player(string username, string connectionId, bool isHuman)
        {
            Username = username;
            ConnectionId = connectionId;
            IsHuman = isHuman;
        }

    }
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

    public class Run
    {
        public Run(int length, int suit, int strength, bool declared, bool declarable)
        {
            Length = length;
            Suit = suit;
            Strength = strength;
            Declared = declared;
            Declarable = declarable;
        }

        public int Length { get; set; } // 3 = Tierce, 4 = Quarte, 5 = Quint
        public int Suit { get; set; } // 1=C, 2=D, 3=H, 4=S
        public int Strength { get; set; }
        public bool Declared { get; set; }
        public bool Declarable { get; set; }
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

    public class BelotHelpers
    {
        public string GetSuitNameFromNumber(int suit)
        {
            string[] suitNames = { "Clubs", "Diamonds", "Hearts", "Spades", "No Trumps", "All Trumps" };
            return suitNames[suit - 1];
        }
        public int GetSuitNumberFromName(string suit)
        {
            string[] suitNames = { "Clubs", "Diamonds", "Hearts" };
            for (int i = 0; i < 3; i++)
            {
                if (suit == suitNames[i]) return i + 1;
            }
            return 4;
        }
        public string GetCardRankFromNumber(int rank)
        {
            string[] rankNames = { "7", "8", "9", "10", "J", "Q", "K", "A" };
            return rankNames[rank - 6];
        }
        public int GetRankFromChar(string rank)
        {
            string[] rankNames = { "7", "8", "9", "10", "J", "Q", "K" };
            for (int i = 0; i < 7; i++)
            {
                if (rank == rankNames[i])
                    return i + 6;
            }
            return 13;
        }
        public string GetRunNameFromLength(int length)
        {
            string[] types = { "Tierce", "Quarte", "Quint" };
            return types[length - 3];
        }

        public int GetCardNumber(string card) // 0 for already played, 1-32 for the possible cards
        {
            if (card == "c0-00") return 0;
            int rank = Int32.Parse(card.Substring(3, 2));
            int suit = Int32.Parse(card.Substring(1, 1));
            return (suit - 1) * 8 + rank - 6 + 1;
        }

        public int DetermineCardPower(string card, int roundSuit, int trickSuit)
        {
            int value = 0;

            int[] offNonTrumpSuit = { 1, 2, 3, 7, 4, 5, 6, 8 }; // to help bots choose a card when losing
            int[] offTrumpSuit = { 1, 2, 7, 5, 8, 3, 4, 6 }; // to help bots choose a card when losing
            int[] nonTrump = { 9, 10, 11, 15, 12, 13, 14, 16 };
            int[] trump = { 17, 18, 23, 21, 24, 19, 20, 22 };

            int suit = Int32.Parse(card.Substring(1, 1));
            int rank = Int32.Parse(card.Substring(3, 2)) - 6;
            if (roundSuit < 5) // C,D,H,S
            {
                if (roundSuit == suit) // if card is a trump
                {
                    value = trump[rank];
                }
                else if (trickSuit == suit)  // if card is in lead-suit and is not a trump
                {
                    value = nonTrump[rank];
                }
                else // if card is off-suit, and is a discard
                {
                    value = offNonTrumpSuit[rank];
                }
            }
            else if (roundSuit == 5)
            {
                if (trickSuit == suit) // if the suit was followed in A
                {
                    value = nonTrump[rank];
                }
                else
                {
                    value = offNonTrumpSuit[rank];
                }
            }
            else  // if the suit was followed in J
            {
                if (trickSuit == suit)
                {
                    value = trump[rank];
                }
                else
                {
                    value = offTrumpSuit[rank];
                }
            }

            return value;
        }
    }
}