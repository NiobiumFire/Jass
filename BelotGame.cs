using Serilog;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace ChatWebApp
{
    public class BelotGame
    {
        public BelotGame(Player[] players, string gameId, bool enableLogging)
        {
            Players = players;
            GameId = gameId;
            Spectators = new List<Spectator>();
            EnableLogging = enableLogging;
        }

        public string GameId { get; set; }
        public Player[] Players { get; set; }
        public List<Spectator> Spectators { get; set; }
        public List<string> Deck { get; set; }
        public List<string>[] Hand { get; set; }
        public int CardsDealt { get; set; }
        public int FirstPlayer { get; set; }
        public int Turn { get; set; }
        public int NumCardsPlayed { get; set; }
        public int RoundSuit { get; set; }
        public int TrickSuit { get; set; }
        public List<int> SuitCall { get; set; }
        public int Rounds { get; set; }
        public bool EWCalled { get; set; }
        public int Caller { get; set; }
        public int Multiplier { get; set; }
        public int[] TrickPoints { get; set; }
        public int[] DeclarationPoints { get; set; }
        public int[] BelotPoints { get; set; }
        public string[] Result { get; set; }
        public int EWRoundPoints { get; set; }
        public int NSRoundPoints { get; set; }
        public List<int[]> ScoreHistory { get; set; }
        public int EWTotal { get; set; }
        public int NSTotal { get; set; }
        public bool EWWonATrick { get; set; }
        public bool NSWonATrick { get; set; }
        public bool Capot { get; set; }
        public bool Inside { get; set; }
        public string[] PlayedCards { get; set; }
        public List<int> AllPlayedCards { get; set; }
        public int HighestTrumpInTrick { get; set; }
        public List<Run>[] Runs { get; set; }
        public List<Carre>[] Carres { get; set; }
        public List<Belot>[] Belots { get; set; }
        public List<string> CurrentExtras { get; set; }
        public bool IsNewRound { get; set; } = true;
        public bool IsNewGame { get; set; } = true;
        public bool WaitDeal { get; set; }
        public bool WaitCall { get; set; }
        public bool WaitCard { get; set; }
        public string LogPath { get; set; }
        public Serilog.Core.Logger Log { get; set; }
        public bool EnableLogging { get; set; }

        public int WinnerDelay { get; set; } = 400;
        public int BotDelay { get; set; } = 800;
        public int RoundSummaryDelay { get; set; } = 6000;

        public static Random rnd = new Random();

        public void SetLogger()
        {
            if (EnableLogging)
            {
                Log = new LoggerConfiguration().WriteTo.File(ConfigurationManager.AppSettings["logfilepath"] + Guid.NewGuid().ToString() + ".txt").CreateLogger();
            }
        }
        public void NewGame()
        {
            SetLogger();
            if (EnableLogging) Log.Information("Resetting for a new game. The players are {0}, {1}, {2}, {3}.", GetDisplayName(0), GetDisplayName(1), GetDisplayName(2), GetDisplayName(3));
            //Random rnd = new Random();
            lock (rnd) FirstPlayer = rnd.Next(4);
            //FirstPlayer = 0;
            WaitDeal = false;
            WaitCall = false;
            WaitCard = false;
            IsNewRound = true;
            EWTotal = 0;
            NSTotal = 0;
            ScoreHistory = new List<int[]>();
        }

        public void NewRound() // set new first player
        {
            Rounds++;
            Turn = FirstPlayer;
            if (EnableLogging) Log.Information("The dealer is " + GetDisplayName(Turn) + ".");

            if (--FirstPlayer == -1) FirstPlayer = 3;
            Deck = new List<string>();
            Hand = new List<string>[4];
            Runs = new List<Run>[4];
            Carres = new List<Carre>[4];
            Belots = new List<Belot>[4];
            for (int i = 0; i < 4; i++)
            {
                Hand[i] = new List<string>();
                Runs[i] = new List<Run>();
                Carres[i] = new List<Carre>();
                Belots[i] = new List<Belot>();
            }
            PlayedCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
            AllPlayedCards = new List<int>();
            CardsDealt = 0;
            NumCardsPlayed = 0;
            TrickSuit = 0;
            HighestTrumpInTrick = 0;
            RoundSuit = 0; // 0 = pass, 1 = clubs ... 5 = no trumps, 6 = all trumps
            SuitCall = new List<int>();
            EWRoundPoints = 0;
            NSRoundPoints = 0;
            Multiplier = 1;
            EWWonATrick = false;
            NSWonATrick = false;
            //Capot = false;
            Inside = false;
        }

        public void Shuffle()
        {
            //var card = new List<string> { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13" }; // Full deck
            var card = new List<string> { "06", "07", "08", "09", "10", "11", "12", "13" }; // Belot deck
            var suit = new List<int> { 1, 2, 3, 4 };

            List<string> masterDeck = new List<string>();

            for (int i = 0; i < card.Count; i++)
            {
                for (int j = 0; j < suit.Count; j++)
                {
                    masterDeck.Add("c" + suit[j] + "-" + card[i]);
                }
            }

            for (int i = 0; i < card.Count * suit.Count; i++)
            {
                int p;
                lock (rnd) p = rnd.Next(masterDeck.Count);
                Deck.Add(masterDeck[p]);
                masterDeck.RemoveAt(p);
            }

            //Deck = new List<string> {"c1-06", "c1-07", "c2-07", "c3-06", "c4-07",
            //    "c1-08", "c1-07", "c2-07", "c3-06", "c4-07",
            //    "c1-06", "c1-07", "c2-07", "c3-06", "c4-07",
            //    "c1-06", "c1-07", "c2-07", "c3-06", "c4-07",
            //    "c1-10", "c1-11", "c1-12",
            //    "c4-11", "c4-12", "c2-12",
            //    "c3-12", "c4-12", "c4-06",
            //    "c2-06", "c3-07", "c4-06" };

            if (EnableLogging) Log.Information("Shuffled deck: " + String.Join(",", Deck) + ".");
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
            Turn = FirstPlayer;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < numCards; j++)
                {
                    Hand[Turn].Add(Deck[CardsDealt++]);
                }
                Hand[Turn] = OrderCardsForHand(Hand[Turn]);
                if (EnableLogging) Log.Information(GetDisplayName(Turn) + " is dealt: " + String.Join(",", Hand[Turn]) + ".");
                if (--Turn == -1) Turn = 3;
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
            if (EnableLogging)
            {
                if (suit > 0 && suit < 7) Log.Information(GetDisplayName(Turn) + " called " + BelotHelpers.GetSuitNameFromNumber(suit) + ".");
                else if (suit == 7) Log.Information(GetDisplayName(Turn) + " doubled.");
                else if (suit == 8) Log.Information(GetDisplayName(Turn) + " redoubled.");
                else if (suit == 0) Log.Information(GetDisplayName(Turn) + " passed.");
                else Log.Information(GetDisplayName(Turn) + " called five-under-nine.");
            }

            SuitCall.Add(suit);

            if (suit > 0)
            {
                EWCalled = Turn == 0 || Turn == 2;
                Caller = Turn;
                if (suit < 7 || suit == 9)
                {
                    RoundSuit = suit;
                    Multiplier = 1;
                }
                else if (suit == 7)
                {
                    Multiplier = 2;
                }
                else if (suit == 8)
                {
                    Multiplier = 4;
                }
            }
        }

        public bool SuitDecided()
        {
            if (RoundSuit == 9) return true;
            if (SuitCall.Count > 3)
            {
                if (string.Join("", SuitCall.GetRange(SuitCall.Count - 3, 3).ToArray()) == "000")
                {
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

        public int[] GetWinners(int player)
        {
            int[] winners = { 0, 0, 0, 0, 0, 0, 0, 0 }; // 2 = hard winner, 1 = soft winner/could be trumped

            bool theirTrumps = false;

            int[] theirStrongestCard = { 0, 0, 0, 0 };
            for (int i = 0; i < 4; i++) // player
            {
                if (i == player) continue;
                for (int j = 0; j < 8; j++) // card
                {
                    int suit = BelotHelpers.GetSuitFromCard(Hand[i][j]);
                    if (suit == 0) continue;
                    int str = BelotHelpers.DetermineCardPower(Hand[i][j], RoundSuit, suit);
                    if (str > theirStrongestCard[suit - 1])
                    {
                        theirStrongestCard[suit - 1] = str;
                    }
                    if (suit == RoundSuit && i != player) theirTrumps = true;
                }
            }

            for (int i = 0; i < 8; i++)
            {
                int suit = BelotHelpers.GetSuitFromCard(Hand[player][i]);
                if (suit == 0) continue;
                int myStrength = BelotHelpers.DetermineCardPower(Hand[player][i], RoundSuit, suit);

                if (suit != RoundSuit && !theirTrumps && myStrength > theirStrongestCard[suit - 1]) winners[i] = 2;
                else if (suit != RoundSuit && theirTrumps && myStrength > theirStrongestCard[suit - 1]) winners[i] = 1;
                else if (suit == RoundSuit && myStrength > theirStrongestCard[suit - 1]) winners[i] = 2;
            }

            return winners;
        }

        public int RemainingCardsInASuit(int suit, int player)
        {
            int cards = 0;
            for (int i = 0; i < 8; i++)
            {
                if (suit == BelotHelpers.GetSuitFromCard(Hand[player][i])) cards++;
            }
            return cards;
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

        public bool CheckBelot(string card)
        {
            bool canDeclare = false;
            if (RoundSuit != 5)
            {
                int rank = Int32.Parse(card.Substring(3, 2));
                if (rank == 11 || rank == 12)
                {
                    int suit = Int32.Parse(card.Substring(1, 1));
                    if (Belots[Turn].Where(s => s.Suit == suit).Where(d => d.Declarable == true).Count() > 0)
                    {
                        Belots[Turn].Where(s => s.Suit == suit).First().Declarable = false;
                        if ((suit == TrickSuit && RoundSuit == 6) || RoundSuit < 5)
                        {
                            canDeclare = true;
                        }
                    }
                }
            }
            return canDeclare;
        }

        public void DeclareBelot(bool declared = true)
        {
            if (RoundSuit < 5)
            {
                if (Belots[Turn].Where(s => s.Suit == RoundSuit).Count() > 0) Belots[Turn].Where(s => s.Suit == RoundSuit).First().Declared = declared;
                if (EnableLogging && Belots[Turn].Where(s => s.Suit == RoundSuit).Where(d => d.Declared).Count() > 0) Log.Information(GetDisplayName(Turn) + " declares a Belot.");
            }
            else if (RoundSuit == 6)
            {
                if (Belots[Turn].Where(s => s.Suit == TrickSuit).Count() > 0) Belots[Turn].Where(s => s.Suit == TrickSuit).First().Declared = declared;
                if (EnableLogging && Belots[Turn].Where(s => s.Suit == TrickSuit).Where(d => d.Declared).Count() > 0) Log.Information(GetDisplayName(Turn) + " declares a Belot.");
            }
        }

        public void DeclareRuns(bool[] declared = null)
        {
            for (int i = 0; i < Runs[Turn].Count; i++)
            {
                if (Runs[Turn][i].Declarable)
                {
                    if (declared == null) Runs[Turn][i].Declared = true;
                    else Runs[Turn][i].Declared = declared[i];
                    if (EnableLogging && Runs[Turn][i].Declared) Log.Information(GetDisplayName(Turn) + " declares a " + BelotHelpers.GetRunNameFromLength(Runs[Turn][i].Length) + ".");
                }
            }
        }

        public void DeclareCarres(bool[] declared = null)
        {
            for (int i = 0; i < Carres[Turn].Count; i++)
            {
                if (declared == null) Carres[Turn][i].Declared = true;
                else Carres[Turn][i].Declared = declared[i];
                if (EnableLogging && Carres[Turn][i].Declared) Log.Information(GetDisplayName(Turn) + " declares a Carre.");
            }
        }

        public void PlayCard(string card)
        {
            PlayedCards[Turn] = card;
            if (EnableLogging) Log.Information(GetDisplayName(Turn) + " plays " + card + ".");

            Hand[Turn][Hand[Turn].IndexOf(card)] = "c0-00";

            NumCardsPlayed++;

            if (TrickSuit == 0) TrickSuit = Int32.Parse(card.Substring(1, 1)); // first card of a trick determines suit

            int trumpstrength = TrumpStrength(card);
            if (HighestTrumpInTrick < trumpstrength) HighestTrumpInTrick = trumpstrength;

            AllPlayedCards.Add(BelotHelpers.GetCardNumber(card));

            //if (EnableLogging && NumCardsPlayed % 4 == 0) Log.Information(GetDisplayName(DetermineWinner()) + " wins trick " + NumCardsPlayed / 4 + ", worth " + CalculateTrickPoints() + " points.");
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
                    if (maxrun == 8)
                    {
                        Runs[i].Add(new Run(3, suit, 8, false, false));
                        Runs[i].Add(new Run(5, suit, 13, false, false));
                    }
                    else if (maxrun > 2 && maxrun < 6)
                    {
                        Runs[i].Add(new Run(maxrun, suit, strength + maxrun - 1, false, false));
                    }
                    else if (maxrun > 5)
                    {
                        Runs[i].Add(new Run(5, suit, strength + maxrun - 1, false, false));
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

        public void TruncateRuns() // reduce overlapping runs where this still rewards points, and return invalidated runs
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < Runs[i].Count; j++)
                {
                    int upper = Runs[i][j].Strength;
                    int lower = upper - Runs[i][j].Length + 1;
                    if (Carres[i].Count == 1)
                    {
                        if (Carres[i][0].Rank >= lower && Carres[i][0].Rank <= upper)
                        {
                            if (Runs[i][j].Length == 3) Runs[i][j].Declarable = false;

                            else // try truncate run
                            {
                                bool first = Carres[i][0].Rank == lower;
                                bool second = Carres[i][0].Rank == lower + 1;
                                bool secondLast = Carres[i][0].Rank == upper - 1;
                                bool last = Carres[i][0].Rank == upper;
                                if ((first || last) && Runs[i][j].Length > 3) // Quarte becomes Tierce, Quint becomes Quarte
                                {
                                    Runs[i][j].Length -= 1;
                                    // if first, strength remains the same, reducing length by 1 is sufficient
                                    if (last) Runs[i][j].Strength -= 1;
                                    Runs[i][j].Declarable = true;
                                }
                                else if (second || secondLast)
                                {
                                    if (Runs[i][j].Length == 4) // Quarte invalidated
                                    {
                                        Runs[i][j].Declarable = false;
                                    }
                                    else // Quint becomes Tierce
                                    {
                                        Runs[i][j].Length -= 2;
                                        // if second, strength remains the same, reducing length by 2 is sufficient
                                        if (secondLast) Runs[i][j].Strength -= 2;
                                        Runs[i][j].Declarable = true;
                                    }
                                }
                                else // Run is a Quint, and Carre is in the middle, therefore no truncation is possible
                                {
                                    Runs[i][j].Declarable = false;
                                }
                            }
                        }
                        else // carre rank does not lie within the run
                        {
                            Runs[i][j].Declarable = true;
                        }
                    }
                    else
                    {
                        Runs[i][j].Declarable = true;
                    }
                }
            }
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

            if (TrickSuit > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (PlayedCards[i] != "c0-00")
                    {
                        int value = BelotHelpers.DetermineCardPower(PlayedCards[i], RoundSuit, TrickSuit);
                        if (value > bestValue)
                        {
                            bestValue = value;
                            winner = i;
                        }
                    }
                }
            }
            if (EnableLogging && PlayedCards.Where(c => c == "c0-00").Count() == 0)
            {
                Log.Information(GetDisplayName(winner) + " wins trick " + NumCardsPlayed / 4 + ", worth " + CalculateTrickPoints() + " points.");
            }
            return winner;
        }

        public int CalculateTrickPoints() // C,D,H,S = 162 (65 vs 97 would result in 7 & 10 in pure rounding), A = 260 (x2), J = 258
        {
            int points = 0;

            for (int i = 0; i < 4; i++)
            {
                points += CalculateCardPoints(PlayedCards[i]);
            }

            if (NumCardsPlayed == 32) points += 10;

            return points;
        }

        public int CalculateCardPoints(string card) // C,D,H,S = 162 (65 vs 97 would result in 7 & 10 in pure rounding), A = 260 (x2), J = 258
        {
            int[] nontrump = { 0, 0, 0, 10, 2, 3, 4, 11 };
            int[] trump = { 0, 0, 14, 10, 20, 3, 4, 11 };
            int points = 0;

            if (card == "c0-00") return points;
            int suit = Int32.Parse(card.Substring(1, 1));
            int rank = Int32.Parse(card.Substring(3, 2)) - 6;
            if (RoundSuit == 6 || RoundSuit == suit)
            {
                points += trump[rank];
            }
            else
            {
                points += nontrump[rank];
            }

            return points;
        }

        public string FinalisePoints()
        {
            TrickPoints = new int[] { EWRoundPoints, NSRoundPoints };
            DeclarationPoints = new int[] { 0, 0 };
            BelotPoints = new int[] { 0, 0 };
            Result = new string[] { "", "Success" };
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
                        if (EnableLogging) Log.Information("The Runs were tied. No extra points awarded for Runs.");
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
                message[2] += ", Capot";
                Capot = true;
            }
            else if (!NSWonATrick)
            {
                EWRoundPoints += 90;
                Result[0] = "Capot";
                message[2] += ", Capot";
                Capot = true;
            }
            else Capot = false;

            if (EWCalled && EWRoundPoints <= NSRoundPoints) // inside
            {
                NSRoundPoints += EWRoundPoints;
                EWRoundPoints = 0;
                Result[0] = "Inside";
                message[2] = "failed";
                if (Capot) message[2] += ", Capot";
                message[2] += ", Inside";
                Inside = true;
            }
            else if (!EWCalled && NSRoundPoints <= EWRoundPoints)
            {
                EWRoundPoints += NSRoundPoints;
                NSRoundPoints = 0;
                Result[1] = "Inside";
                message[2] = "failed";
                if (Capot) message[2] += ", Capot";
                message[2] += ", Inside";
                Inside = true;
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

            if (EWRoundPoints > 1000 || NSRoundPoints > 1000)
            {

            }

            EWTotal += EWRoundPoints;
            NSTotal += NSRoundPoints;
            if (EnableLogging) Log.Information(String.Join(" ", message) + ".");
            if (EnableLogging) Log.Information("E/W win {0} points. N/S win {1} points.", EWRoundPoints, NSRoundPoints);
            return String.Join(" ", message) + ".";
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

        public void CloseLog()
        {
            Log.Dispose();
        }
    }
}