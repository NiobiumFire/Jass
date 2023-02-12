using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatWebApp
{
    public class BelotGame
    {
        public BelotGame(Player[] players)
        {
            Players = players;
        }

        public string GameId { get; set; }
        public Player[] Players { get; set; }
        public List<string> Deck { get; set; }
        public List<string>[] Hand { get; set; }
        public int CardsDealt { get; set; }
        public int FirstPlayer { get; set; }
        public int Turn { get; set; }
        public int NumCardsPlayed { get; set; }
        public int RoundSuit { get; set; }
        public int TrickSuit { get; set; }
        public List<int> SuitCall { get; set; }
        public bool EWCalled { get; set; }
        public int Multiplier { get; set; }
        public int EWRoundPoints { get; set; }
        public int NSRoundPoints { get; set; }
        public int EWTotal { get; set; }
        public int NSTotal { get; set; }
        public bool EWWonATrick { get; set; }
        public bool NSWonATrick { get; set; }
        public bool Capot { get; set; }
        public string[] PlayedCards { get; set; }
        public int HighestTrumpInTrick { get; set; }
        public List<Run>[] Runs { get; set; }
        public List<Carre>[] Carres { get; set; }
        public List<Belot>[] Belots { get; set; }

        public static string logPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/BelotServerLog-"+ GameId + ".txt");
        public static Serilog.Core.Logger Log = new LoggerConfiguration().WriteTo.File(logPath, rollingInterval: RollingInterval.Day).CreateLogger();

        public void Shuffle()
        {
            //var card = new List<string> { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13" }; // Full deck
            var card = new List<string> { "06", "07", "08", "09", "10", "11", "12", "13" }; // Belot deck
            var suit = new List<int> { 1, 2, 3, 4 };

            Random rnd = new Random();

            List<string> deck = new List<string>();

            while (deck.Count < card.Count * suit.Count)
            {
                int i = rnd.Next(card.Count); // 0 <= i <= 13
                int j = rnd.Next(suit.Count); // 0 <= i <= 4
                if (!deck.Contains("c" + suit[j] + "-" + card[i]))
                {
                    deck.Add("c" + suit[j] + "-" + card[i]);
                }
            }

            Log.Information("Shuffled deck: " + String.Join(",", deck));
        }

        public List<string> OrderCardsForHand(List<string> hand)
        {
            var nontrumporder = new List<string> { "06", "07", "08", "10", "11", "12", "09", "13" };
            var trumporder = new List<string> { "06", "07", "11", "12", "09", "13", "08", "10" };
            var nontrump = new List<int>();
            var trump = new List<int>();
            var masterlist = new List<string>();

            for (int i = 1; i < 5; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (RoundSuit == i || RoundSuit == 6)
                    {
                        masterlist.Add("c" + i + "-" + trumporder[j]);
                    }
                    else
                    {
                        masterlist.Add("c" + i + "-" + nontrumporder[j]);
                    }
                }
            }

            var sortedhand = hand.OrderBy(i => masterlist.IndexOf(i)).ToList();
            return sortedhand;
        }

        public List<string> OrderCardsForRuns(List<string> hand)
        {
            var runorder = new List<string> { "06", "07", "08", "09", "10", "11", "12", "13" };
            var masterlist = new List<string>();

            for (int i = 1; i < 5; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    masterlist.Add("c" + i + "-" + runorder[j]);
                }
            }

            var sortedhand = hand.OrderBy(i => masterlist.IndexOf(i)).ToList();
            return sortedhand;
        }

        public void Deal(int numCards)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < numCards; j++)
                {
                    Hand[Turn].Add(Deck[CardsDealt++]);
                }
                Hand[Turn] = OrderCardsForHand(Hand[Turn]);
                Log.Information(GetDisplayName(Turn) + " is dealt: " + String.Join(",", Hand[Turn]) + ".");
                if(--Turn == -1) Turn = 3;
            }
        }

        public int[] ValidCalls()
        {
            int[] validCalls = { 1, 1, 1, 1, 1, 1, 0, 0 }; // c, d, h, s, A, J, x2, x4

            if (RoundSuit > 0)
            {
                for (int i = 0; i < RoundSuit; i++)
                {
                    validCalls[i] = 0;
                }

                if ((Turn % 2 == 0 && !EWCalled) || (Turn % 2 == 1 && EWCalled))
                {
                    if (Multiplier == 1)
                    {
                        validCalls[6] = 1;
                    }
                    else if (Multiplier == 2)
                    {
                        validCalls[7] = 1;
                    }
                }
            }
            return validCalls;
        }

        public void NominateSuit(int suit)
        {
            SuitCall.Add(suit);

            if (suit > 0)
            {
                EWCalled = Turn == 0 || Turn == 2;
                if (suit < 7)
                {
                    RoundSuit = suit;
                    Multiplier = 1;
                }
                else if (suit == 7)
                {
                    Multiplier = 2;
                }
                else
                {
                    Multiplier = 4;
                }
            }

            if (--Turn == -1) Turn = 3;
        }

        public bool SuitDecided()
        {
            if (SuitCall.Count > 3)
            {
                if (string.Join("", SuitCall.GetRange(SuitCall.Count - 3, 3).ToArray()) == "000")
                {
                    if (SuitCall[SuitCall.Count - 4] == 0)
                    {
                        for (int i = 0; i < 4; i++) // 4 passes
                        {
                            Hand[i] = new List<string>();
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public int[] ValidCards()
        {
            int[] validCards = { 1, 1, 1, 1, 1, 1, 1, 1 };
            validCards = RemovePlayedCards(validCards);
            if (TrickSuit > 0) // if it's not the first card of the trick
            {
                if (RoundSuit == TrickSuit && PlayerHasCardsOfSuit(RoundSuit))
                {
                    validCards = RemoveCardsNotOfSuit(validCards, RoundSuit);
                    if (PlayerHasHigherTrump()) validCards = RemoveLowerTrumps(validCards);
                }

                else if (PlayerHasCardsOfSuit(TrickSuit))
                {
                    validCards = RemoveCardsNotOfSuit(validCards, TrickSuit);
                    if (RoundSuit == 6 && PlayerHasHigherTrump()) validCards = RemoveLowerTrumps(validCards);
                }
                // condition 3 is why it's necessary to first check if trick suit is trumps
                else if (RoundSuit < 5 && PlayerHasCardsOfSuit(RoundSuit)) // if trumps (C,D,H,S) NOT lead, and player doesn't have any of the trick suit
                {
                    int currentwinner = DetermineWinner();
                    if ((Turn % 2 == 0 && currentwinner % 2 != 0) || ((Turn - 1) % 2 == 0 && (currentwinner - 1) % 2 != 0)) // if partner is not currently winning the trick
                    {
                        if (PlayerHasHigherTrump()) // if player has a higher trump than what has been played in this trick, must overtrump
                        {
                            validCards = RemoveCardsNotOfSuit(validCards, RoundSuit);
                            validCards = RemoveLowerTrumps(validCards);
                        }
                    }
                }
            }
            return validCards;
        }

        public int[] RemovePlayedCards(int[] validcards)
        {
            for (int i = 0; i < 8; i++)
            {
                string card = Hand[Turn][i];
                if (Int32.Parse(card.Substring(1, 1)) == 0)
                {
                    validcards[i] = 0;
                }
            }
            return validcards;
        }
        public bool PlayerHasCardsOfSuit(int suit)
        {
            foreach (string card in Hand[Turn])
            {
                if (Int32.Parse(card.Substring(1, 1)) == suit)
                {
                    return true;
                }
            }
            return false;
        }
        public int[] RemoveCardsNotOfSuit(int[] validcards, int suit)
        {
            for (int i = 0; i < 8; i++)
            {
                if (validcards[i] == 1)
                {
                    string card = Hand[Turn][i];
                    if (Int32.Parse(card.Substring(1, 1)) != suit)
                    {
                        validcards[i] = 0;
                    }
                }
            }
            return validcards;
        }
        public bool PlayerHasHigherTrump()
        {
            foreach (string card in Hand[Turn])
            {
                if (TrumpStrength(card) > HighestTrumpInTrick)
                {
                    return true;
                }
            }
            return false;
        }
        public int[] RemoveLowerTrumps(int[] validcards)
        {
            for (int i = 0; i < 8; i++)
            {
                if (validcards[i] == 1)
                {
                    string card = Hand[Turn][i];
                    if (TrumpStrength(card) < HighestTrumpInTrick)
                    {
                        validcards[i] = 0;
                    }
                }
            }
            return validcards;
        }
        public int TrumpStrength(string card)
        {
            int trumpstrength = 0;
            if (Int32.Parse(card.Substring(1, 1)) == RoundSuit || (RoundSuit == 6 && Int32.Parse(card.Substring(1, 1)) == TrickSuit))
            {
                int[] strength = { 1, 2, 7, 5, 8, 3, 4, 6 };
                trumpstrength = strength[Int32.Parse(card.Substring(3, 2)) - 6];
            }
            return trumpstrength;
        }

        public List<string> FindExtras(string card)
        {
            List<string> extras = new List<string>();

            if (RoundSuit != 5)
            {
                List<bool> finalOverlaps = new List<bool>();

                int rank = Int32.Parse(card.Substring(3, 2));
                if (rank == 11 || rank == 12)
                {
                    int suit = Int32.Parse(card.Substring(1, 1));
                    if (Belots[Turn].Where(s => s.Suit == suit).ToList().Where(d => d.Declarable == true).Count() > 0)
                    {
                        Belots[Turn].Where(s => s.Suit == suit).First().Declarable = false;
                        if ((suit == TrickSuit || TrickSuit == 0))
                        {
                            extras.Add("Belot: " + GetSuitNameFromNumber(suit));
                            finalOverlaps.Add(false);
                        }
                    }
                }

                if (NumCardsPlayed < 4)
                {
                    bool[] overlaps = new bool[Runs[Turn].Count];

                    // find overlaps and truncate runs accordingly
                    // overlap only occurs if player has at least 1 run and exactly 1 Carre. Runs cannot overlap each other. With 2 Carres, there are no Runs and there is no overlap

                    if (Carres[Turn].Count == 1 && Runs[Turn].Count() > 0) overlaps = FindRunCarreOverlap();

                    for (int i = 0; i < Runs[Turn].Count; i++)
                    {
                        extras.Add(GetRunNameFromLength(Runs[Turn][i].Length) + ": " + GetSuitNameFromNumber(Runs[Turn][i].Suit) + " " + GetCardRankFromNumber(Runs[Turn][i].Strength - Runs[Turn][i].Length + 1) + "→" + GetCardRankFromNumber(Runs[Turn][i].Strength));
                        finalOverlaps.Add(overlaps[i]);
                    }
                    for (int i = 0; i < Carres[Turn].Count; i++)
                    {
                        extras.Add("Carre: " + GetCardRankFromNumber(Carres[Turn][i].Rank));
                        finalOverlaps.Add(false);
                    }
                }
            }
            return extras;
        }

        public void PlayCard(string card)
        {
            PlayedCards[Turn] = card;
            Log.Information(GetDisplayName(Turn) + " plays " + card + ".");

            Hand[Turn][Hand[Turn].IndexOf(card)] = "c0-00";

            NumCardsPlayed++;

            if (TrickSuit == 0) TrickSuit = Int32.Parse(card.Substring(1, 1)); // first card of a trick determines suit

            int trumpstrength = TrumpStrength(card);
            if (HighestTrumpInTrick < trumpstrength) HighestTrumpInTrick = trumpstrength;

            if (NumCardsPlayed % 4 == 0) // trick end
            {
                int winner = DetermineWinner();
                int pointsBefore = EWRoundPoints + EWRoundPoints;
                if (winner == 0 || winner == 2)
                {
                    EWRoundPoints += CalculateRoundPoints();
                    EWWonATrick = true;
                }
                else
                {
                    NSRoundPoints += CalculateRoundPoints();
                    NSWonATrick = true;
                }
                Log.Information(GetDisplayName(winner) + " wins trick " + NumCardsPlayed / 4 + ", worth " + (EWRoundPoints + NSRoundPoints - pointsBefore) + " points.");

                if (NumCardsPlayed < 32)
                {
                    Turn = winner;
                    PlayedCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
                }
                HighestTrumpInTrick = 0;
                TrickSuit = 0;
            }
            else
            {
                if (--Turn == -1) Turn = 3;
            }
        }

        public void FindRuns()
        {
            for (int i = 0; i < 4; i++)
            {
                List<string> hand = OrderCardsForRuns(Hand[i]);
                for (int j = 0; j < 6; j++)
                {
                    int maxrun = 1;
                    int suit = Int32.Parse(hand[j].Substring(1, 1));
                    int strength = Int32.Parse(hand[j].Substring(3, 2));
                    for (int k = 0; k < maxrun; k++)
                    {
                        if (j + k + 1 > 7) break;

                        if (Int32.Parse(hand[j + k + 1].Substring(1, 1)) == suit) // if two adjacent cards are of the same suit
                        {

                            if (Int32.Parse(hand[j + k + 1].Substring(3, 2)) == strength + k + 1) // if second card is adjacent in rank to the first card
                            {
                                maxrun++; // consider the next card
                            }
                        }
                    }
                    if (maxrun > 2 && maxrun < 6)
                    {
                        Runs[i].Add(new Run(maxrun, suit, strength + maxrun - 1, false));
                    }
                    else if (maxrun > 5)
                    {
                        Runs[i].Add(new Run(5, suit, strength + maxrun - 1, false));
                    }
                    j += maxrun - 1;
                }
            }
        }

        public void FindCarres()
        {
            for (int i = 0; i < 4; i++)
            {
                int[] ranks = { 0, 0, 0, 0, 0, 0 };
                for (int j = 0; j < 8; j++)
                {
                    int rank = Int32.Parse(Hand[i][j].Substring(3, 2));
                    if (rank > 7) ranks[rank - 8]++;
                }
                for (int j = 0; j < 6; j++)
                {
                    if (ranks[j] == 4) Carres[i].Add(new Carre(j + 8, false));
                }
            }
        }

        public bool[] FindRunCarreOverlap() // reduce overlapping runs where this still rewards points, and return invalidated runs
        {
            bool[] overlaps = new bool[Runs[Turn].Count];

            for (int i = 0; i < Runs[Turn].Count; i++)
            {
                int upper = Runs[Turn][i].Strength;
                int lower = upper - Runs[Turn][i].Length + 1;
                if (Carres[Turn][0].Rank >= lower && Carres[Turn][0].Rank <= upper)
                {
                    if (Runs[Turn][i].Length == 3)
                    {
                        overlaps[i] = true;
                    }
                    else // try truncate run
                    {
                        bool first = Carres[Turn][0].Rank == lower;
                        bool second = Carres[Turn][0].Rank == lower + 1;
                        bool secondLast = Carres[Turn][0].Rank == upper - 1;
                        bool last = Carres[Turn][0].Rank == upper;
                        if ((first || last) && Runs[Turn][i].Length > 3) // Quarte becomes Tierce, Quint becomes Quarte
                        {
                            Runs[Turn][i].Length -= 1;
                            // if first, strength remains the same, reducing length by 1 is sufficient
                            if (last) Runs[Turn][i].Strength -= 1;
                            overlaps[i] = false;
                        }
                        else if (second || secondLast)
                        {
                            if (Runs[Turn][i].Length == 4) // Quarte invalidated
                            {
                                overlaps[i] = true;
                            }
                            else // Quint becomes Tierce
                            {
                                Runs[Turn][i].Length -= 2;
                                // if second, strength remains the same, reducing length by 2 is sufficient
                                if (secondLast) Runs[Turn][i].Strength -= 2;
                                overlaps[i] = false;
                            }
                        }
                        else // Run is a Quint, and Carre is in the middle, therefore no truncation is possible
                        {
                            overlaps[i] = true;
                        }
                    }
                }
                else // carre rank does not lie within the run
                {
                    overlaps[i] = false;
                }
            }
            return overlaps;
        }

        public void FindBelots()
        {
            for (int i = 0; i < 4; i++)
            {
                int[] newBelots = { 0, 0, 0, 0 };
                for (int j = 0; j < 8; j++)
                {
                    int rank = Int32.Parse(Hand[i][j].Substring(3, 2));
                    if (rank == 11 || rank == 12)
                    {
                        int suit = Int32.Parse(Hand[i][j].Substring(1, 1));
                        newBelots[suit - 1]++;
                    }
                }
                for (int j = 0; j < 4; j++)
                {
                    if (newBelots[j] == 2 && (j + 1 == RoundSuit || RoundSuit == 6)) Belots[i].Add(new Belot(j + 1, false, true));
                }
            }
        }

        public int DetermineWinner()
        {
            int winner = Turn;
            int bestValue = 0;

            CardPower cp = new CardPower();

            if (TrickSuit > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (PlayedCards[i] != "c0-00")
                    {
                        int value = cp.DetermineCardPower(PlayedCards[i], RoundSuit, TrickSuit);
                        if (value > bestValue)
                        {
                            bestValue = value;
                            winner = i;
                        }
                    }
                }
            }
            return winner;
        }

        public int CalculateRoundPoints() // C,D,H,S = 162 (65 vs 97 would result in 7 & 10 in pure rounding), A = 260 (x2), J = 258
        {
            int[] nontrump = { 0, 0, 0, 10, 2, 3, 4, 11 };
            int[] trump = { 0, 0, 14, 10, 20, 3, 4, 11 };
            int points = 0;

            for (int i = 0; i < 4; i++)
            {

                int suit = Int32.Parse(PlayedCards[i].Substring(1, 1));
                int card = Int32.Parse(PlayedCards[i].Substring(3, 2)) - 6;
                if (RoundSuit == 6 || RoundSuit == suit)
                {
                    points += trump[card];
                }
                else
                {
                    points += nontrump[card];
                }
            }
            if (NumCardsPlayed == 32) points += 10;

            return points;
        }

        public void FinalisePoints()
        {
            int[] TrickPoints = new int[] { EWRoundPoints, NSRoundPoints };
            int[] DeclarationPoints = new int[] { 0, 0 };
            int[] BelotPoints = new int[] { 0, 0 };
            string[] Result = new string[] { "", "Success" };
            string[] message = { "N/S", "call", "succeeded" };
            if (EWCalled)
            {
                Result = new string[] { "Success", "" };
                message[0] = "E/W";
            }

            if (RoundSuit == 5) // no trumps points are always doubled
            {
                EWRoundPoints *= 2;
                NSRoundPoints *= 2;
            }
            else
            {
                // Tierce
                List<Run> EWRuns = new List<Run>();
                EWRuns.AddRange(Runs[0].Where(d => d.Declared == true));
                EWRuns.AddRange(Runs[2].Where(d => d.Declared == true));
                List<Run> NSRuns = new List<Run>();
                NSRuns.AddRange(Runs[1].Where(d => d.Declared == true));
                NSRuns.AddRange(Runs[3].Where(d => d.Declared == true));
                if (EWRuns.Count + NSRuns.Count > 0)
                {
                    int[] MaxComparer = new int[] { 0, 0 }; // EW, NS
                    if (EWRuns.Count > 0) MaxComparer[0] = EWRuns.OrderByDescending(r => r.Length).First().Length;
                    if (NSRuns.Count > 0) MaxComparer[1] = NSRuns.OrderByDescending(r => r.Length).First().Length;
                    if (MaxComparer[0] == MaxComparer[1])
                    {
                        MaxComparer[0] = EWRuns.Where(r => r.Length == MaxComparer[0]).OrderByDescending(r => r.Strength).First().Strength;
                        MaxComparer[1] = NSRuns.Where(r => r.Length == MaxComparer[1]).OrderByDescending(r => r.Strength).First().Strength;
                    }
                    if (MaxComparer[0] == MaxComparer[1])
                    {
                        Log.Information("The Runs were tied. No extra points awarded for Runs.");
                    }
                    else if (MaxComparer[0] > MaxComparer[1])
                    {
                        DeclarationPoints[0] += 20 * (EWRuns.Where(r => r.Length == 3).Count());
                        DeclarationPoints[0] += 50 * (EWRuns.Where(r => r.Length == 4).Count());
                        DeclarationPoints[0] += 100 * (EWRuns.Where(r => r.Length == 5).Count());
                    }
                    else
                    {
                        DeclarationPoints[1] += 20 * (NSRuns.Where(r => r.Length == 3).Count());
                        DeclarationPoints[1] += 50 * (NSRuns.Where(r => r.Length == 4).Count());
                        DeclarationPoints[1] += 100 * (NSRuns.Where(r => r.Length == 5).Count());
                    }
                }

                // Carre
                List<Carre> EWCarres = new List<Carre>();
                EWCarres.AddRange(Carres[0].Where(d => d.Declared == true));
                EWCarres.AddRange(Carres[2].Where(d => d.Declared == true));
                List<Carre> NSCarres = new List<Carre>();
                NSCarres.AddRange(Carres[1].Where(d => d.Declared == true));
                NSCarres.AddRange(Carres[3].Where(d => d.Declared == true));
                if (EWCarres.Count + NSCarres.Count > 0)
                {
                    int[] MaxComparer = new int[] { 0, 0 }; // EW, NS
                    if (EWCarres.Count > 0) MaxComparer[0] = EWCarres.OrderByDescending(r => r.Rank).First().Rank;
                    if (NSCarres.Count > 0) MaxComparer[1] = NSCarres.OrderByDescending(r => r.Rank).First().Rank;
                    if (MaxComparer[0] == 8) MaxComparer[0] = 14;
                    if (MaxComparer[0] == 10) MaxComparer[0] = 15;
                    if (MaxComparer[1] == 8) MaxComparer[1] = 14;
                    if (MaxComparer[1] == 10) MaxComparer[1] = 15;
                    if (MaxComparer[0] > MaxComparer[1])
                    {
                        DeclarationPoints[0] += 200 * (EWCarres.Where(r => r.Rank == 10).Count());
                        DeclarationPoints[0] += 150 * (EWCarres.Where(r => r.Rank == 8).Count());
                        DeclarationPoints[0] += 100 * (EWCarres.Where(r => r.Rank != 10 && r.Rank != 8).Count());
                    }
                    else
                    {
                        DeclarationPoints[1] += 200 * (NSCarres.Where(r => r.Rank == 10).Count());
                        DeclarationPoints[1] += 150 * (NSCarres.Where(r => r.Rank == 8).Count());
                        DeclarationPoints[1] += 100 * (NSCarres.Where(r => r.Rank != 10 && r.Rank != 8).Count());
                    }
                }

                EWRoundPoints += DeclarationPoints[0];
                NSRoundPoints += DeclarationPoints[1];

                // Belot
                BelotPoints[0] += 20 * (Belots[0].Where(d => d.Declared == true).Count() + Belots[2].Where(d => d.Declared == true).Count());
                BelotPoints[1] += 20 * (Belots[1].Where(d => d.Declared == true).Count() + Belots[3].Where(d => d.Declared == true).Count());

                EWRoundPoints += BelotPoints[0];
                NSRoundPoints += BelotPoints[1];
            }

            if (!EWWonATrick) // capot
            {
                NSRoundPoints += 90;
                Result[1] = "Capot";
                message[3] += ", Capot";
                Capot = true;
            }
            else if (!NSWonATrick)
            {
                EWRoundPoints += 90;
                Result[0] = "Capot";
                message[3] += ", Capot";
                Capot = true;
            }

            if (EWCalled && EWRoundPoints <= NSRoundPoints) // inside
            {
                NSRoundPoints += EWRoundPoints;
                EWRoundPoints = 0;
                Result[0] = "Inside";
                message[2] = "failed";
                if (Capot) message[2] += ", Capot";
                message[2] += ", Inside";
            }
            else if (!EWCalled && NSRoundPoints <= EWRoundPoints)
            {
                EWRoundPoints += NSRoundPoints;
                NSRoundPoints = 0;
                Result[1] = "Inside";
                message[2] = "failed";
                if (Capot) message[2] += ", Capot";
                message[2] += ", Inside";
            }

            if (Multiplier > 1) // double and redouble
            {
                EWRoundPoints *= Multiplier;
                NSRoundPoints *= Multiplier;
                if (EWCalled && EWRoundPoints > NSRoundPoints)
                {
                    EWRoundPoints += NSRoundPoints;
                    NSRoundPoints = 0;
                }
                else if (!EWCalled && NSRoundPoints > EWRoundPoints)
                {
                    NSRoundPoints += EWRoundPoints;
                    EWRoundPoints = 0;
                }
            }
            EWTotal += EWRoundPoints;
            NSTotal += NSRoundPoints;
            Log.Information(String.Join(" ", message) + ".");
            Log.Information("E/W win {0} points. N/S win {1} points.", EWRoundPoints, NSRoundPoints);
        }

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

        public string GetBotName(int pos)
        {
            string[] seat = { "West", "North", "East", "South" };
            return "Robot " + seat[pos];
        }

        public string GetDisplayName(int pos)
        {
            if (Players[pos].IsHuman)
            {
                return Players[pos].Username;
            }
            else
            {
                return GetBotName(pos);
            }
        }
    }

    public class BelotGameState
    {
        public static Player[] players = { new Player(), new Player(), new Player(), new Player() };
        public static List<string> deck;
        public static List<string>[] hand = { new List<string>(), new List<string>(), new List<string>(), new List<string>() };
        public static bool newRound = true;
        public static int cardsDealt;
        public static int firstPlayer, turn;
        public static int numCardsPlayed = 0;
        public static int roundSuit, trickSuit;
        public static List<int> suitCall;
        public static bool ewCalled;
        public static int multiplier;
        public static int ewRoundPoints, nsRoundPoints, ewTotal, nsTotal;
        public static bool ewWonATrick, nsWonATrick;
        public static bool capot;
        public static int scoreTarget = 1501;
        public static string[] playedCards;
        public static int highestTrumpInTrick;
        public static List<Run>[] runs;
        public static List<Carre>[] carres;
        public static List<Belot>[] belots;
        public static string botGUID = "7eae0694-38c9-48c0-9016-40e7d9ab962c";
        public static int botDelay = 500;
        public static bool waitDeal, waitCall, waitCard;

        public static string logPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/BelotServerLog-.txt");
        public static Serilog.Core.Logger log = new LoggerConfiguration().WriteTo.File(logPath, rollingInterval: RollingInterval.Day).CreateLogger();
    }
}