using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using BelotWebApp.BelotClasses.Players;
using Serilog;

namespace BelotWebApp.BelotClasses
{
    public class BelotGame
    {
        public BelotGame(Player[] players, string roomId, string logPath = "")
        {
            Players = players;
            RoomId = roomId;
            Spectators = new List<Spectator>();
            EnableLogging = logPath == "" ? false : true;
            LogPath = logPath;
        }

        public string RoomId { get; set; }
        public string GameId { get; set; } = "";
        public Player[] Players { get; set; }
        public List<Spectator> Spectators { get; set; }
        public List<Card> Deck { get; set; }
        public List<Card>[] Hand { get; set; }
        public int CardsDealt { get; set; }
        public int FirstPlayer { get; set; }
        public int Turn { get; set; }
        public int NumCardsPlayed { get; set; }
        public Call RoundCall { get; set; }
        public Suit? TrickSuit { get; set; }
        public List<Call> Calls { get; set; }
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
        public Card[] TableCards { get; set; }
        public List<int> CardsPlayedThisRound { get; set; }
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
        public Serilog.Core.Logger Log { get; set; }
        public string LogPath { get; set; }
        public bool EnableLogging { get; set; }

        public int WinnerDelay { get; set; } = 400;
        public int BotDelay { get; set; } = 800;
        public int RoundSummaryDelay { get; set; } = 6000;

        private static readonly Random rnd = new();

        public void SetLogger()
        {
            if (EnableLogging)
            {
                //Log = new LoggerConfiguration().WriteTo.File(ConfigurationManager.AppSettings["logfilepath"] + GameId + ".txt").CreateLogger();
                Log = new LoggerConfiguration().WriteTo.File(LogPath + GameId + ".txt").CreateLogger();
            }
        }

        public void NewGame()
        {
            GameId = Guid.NewGuid().ToString();
            SetLogger();
            if (EnableLogging) Log.Information("Players: {0},{1},{2},{3}", GetDisplayName(0), GetDisplayName(1), GetDisplayName(2), GetDisplayName(3));
            lock (rnd) FirstPlayer = rnd.Next(4);
            //FirstPlayer = 1;
            WaitDeal = false;
            WaitCall = false;
            WaitCard = false;
            IsNewRound = true;
            EWTotal = 0;
            NSTotal = 0;
            ScoreHistory = [];
        }

        public void NewRound() // set new first player
        {
            Rounds++;
            Turn = FirstPlayer;
            if (EnableLogging)
            {
                Log.Information("Dealer: {0}", Turn);
            }

            if (--FirstPlayer == -1) FirstPlayer = 3;
            Deck = [];
            Hand = new List<Card>[4];
            Runs = new List<Run>[4];
            Carres = new List<Carre>[4];
            Belots = new List<Belot>[4];
            for (int i = 0; i < 4; i++)
            {
                Hand[i] = [];
                Runs[i] = [];
                Carres[i] = [];
                Belots[i] = [];
            }
            TableCards = [new(), new(), new(), new()];
            CardsPlayedThisRound = [];
            CardsDealt = 0;
            NumCardsPlayed = 0;
            TrickSuit = null;
            HighestTrumpInTrick = 0;
            RoundCall = 0; // 0 = pass, 1 = clubs ... 5 = no trumps, 6 = all trumps
            Calls = [];
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
            Suit[] suits = [Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades];
            var ranks = Enum.GetValues(typeof(Rank));

            List<Card> masterDeck = [];

            foreach (Suit suit in suits)
            {
                foreach (Rank rank in ranks)
                {
                    masterDeck.Add(new(suit, rank));
                }
            }

            for (int i = 0; i < suits.Length * ranks.Length; i++)
            {
                int p;
                lock (rnd) p = rnd.Next(masterDeck.Count);
                Deck.Add(masterDeck[p]);
                masterDeck.RemoveAt(p);
            }
            Deck = [
                new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven),
                new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven),
                new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven),
                new(Suit.Spades, Rank.Seven), new(Suit.Spades, Rank.Eight), new(Suit.Spades, Rank.Nine), new(Suit.Spades, Rank.Ten), new(Suit.Spades, Rank.Jack),
                new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven),
                new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven),
                new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Seven),
                new(Suit.Spades, Rank.Queen), new(Suit.Spades, Rank.King), new(Suit.Spades, Rank.Ace) ];

            if (EnableLogging) Log.Information("Deck: {0}", String.Join(",", Deck));
        }

        public List<Card> OrderCardsForHand(List<Card> hand)
        {
            var masterlist = new List<Card>();

            Call[] suitCalls = [Call.Clubs, Call.Diamonds, Call.Hearts, Call.Spades];

            foreach (var call in suitCalls)
            {
                var strengths = RoundCall == call || RoundCall == Call.AllTrumps ? BelotHelpers.trumpRanks : BelotHelpers.nonTrumpRanks;

                for (int i = 0; i < 8; i++)
                {
                    masterlist.Add(new((Suit)call, strengths[i]));
                }
            }

            var sortedhand = hand.OrderBy(i => masterlist.FindIndex(m => m.Suit == i.Suit && m.Rank == i.Rank)).ToList();
            return sortedhand;
        }

        public List<Card> OrderCardsForRuns(List<Card> hand)
        {
            var masterlist = new List<Card>();

            Call[] suitCalls = [Call.Clubs, Call.Diamonds, Call.Hearts, Call.Spades];

            foreach (var call in suitCalls)
            {
                for (int j = 0; j < 8; j++)
                {
                    masterlist.Add(new((Suit)call, BelotHelpers.runRanks[j]));
                }
            }

            var sortedhand = hand.OrderBy(i => masterlist.FindIndex(m => m.Suit == i.Suit && m.Rank == i.Rank)).ToList();
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
                if (EnableLogging) Log.Information("Hand {0}: {1}", Turn, String.Join(",", Hand[Turn]));
                if (--Turn == -1) Turn = 3;
            }
        }

        public int[] ValidCalls()
        {
            int[] validCalls = [1, 1, 1, 1, 1, 1, 0, 0]; // c, d, h, s, A, J, x2, x4

            if (RoundCall > 0)
            {
                for (int i = 0; i < (int)RoundCall; i++)
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

        public void NominateSuit(Call call)
        {
            //if (EnableLogging) Log.Information("Call {0}: {1}", Turn, suit);

            Calls.Add(call);

            if (call != Call.Pass)
            {
                EWCalled = Turn == 0 || Turn == 2;
                Caller = Turn;
                if (call == Call.Double)
                {
                    Multiplier = 2;
                }
                else if (call == Call.Redouble)
                {
                    Multiplier = 4;
                }
                else
                {
                    RoundCall = call;
                    Multiplier = 1;
                }
            }
        }

        public bool SuitDecided()
        {
            if (RoundCall == Call.FiveUnderNine || (Calls.Count > 3 && Calls.TakeLast(3).All(c => c == Call.Pass)))
            {
                Log.Information("Call: {0}", String.Join(",", Calls));
                return true;
            }
            return false;
        }

        public int[] ValidCards()
        {
            int[] validCards = [1, 1, 1, 1, 1, 1, 1, 1];
            validCards = InvalidatePlayedCards(validCards);
            if (validCards.Sum() == 0)
            {

            }
            if (TrickSuit != null) // if it's not the first card of the trick
            {
                if (BelotHelpers.IsSuit(RoundCall) && (Suit)RoundCall == TrickSuit && PlayerHasCardsOfSuit((Suit)RoundCall)) // RoundSuit is C,D,H,S
                {
                    validCards = InvalidateCardsNotOfSuit(validCards, (Suit)RoundCall);
                    if (validCards.Sum() == 0)
                    {

                    }
                    if (PlayerHasHigherTrump())
                    {
                        validCards = InvalidateLowerTrumps(validCards);
                        if (validCards.Sum() == 0)
                        {

                        }
                    }
                }
                else if (PlayerHasCardsOfSuit((Suit)TrickSuit))
                {
                    validCards = InvalidateCardsNotOfSuit(validCards, (Suit)TrickSuit);
                    if (validCards.Sum() == 0)
                    {

                    }
                    if (RoundCall == Call.AllTrumps && PlayerHasHigherTrump())
                    {
                        validCards = InvalidateLowerTrumps(validCards);
                        if (validCards.Sum() == 0)
                        {

                        }
                    }
                }
                // condition 3 is why it's necessary to first check if trick suit is trumps
                else if (BelotHelpers.IsSuit(RoundCall) && PlayerHasCardsOfSuit((Suit)RoundCall)) // if trumps (C,D,H,S) NOT lead, and player doesn't have any of the trick suit
                {
                    int currentwinner = DetermineWinner();
                    if ((Turn % 2 == 0 && currentwinner % 2 != 0) || ((Turn - 1) % 2 == 0 && (currentwinner - 1) % 2 != 0)) // if partner is not currently winning the trick
                    {
                        if (PlayerHasHigherTrump()) // if player has a higher trump than what has been played in this trick, must overtrump
                        {
                            validCards = InvalidateCardsNotOfSuit(validCards, (Suit)RoundCall);
                            if (validCards.Sum() == 0)
                            {

                            }
                            validCards = InvalidateLowerTrumps(validCards);
                            if (validCards.Sum() == 0)
                            {

                            }
                        }
                    }
                }
            }

            if (validCards.Sum() == 0)
            {

            }

            return validCards;
        }

        public int[] GetWinners(int player)
        {
            int[] winners = [0, 0, 0, 0, 0, 0, 0, 0]; // 2 = hard winner, 1 = soft winner/could be trumped

            bool otherHandsHaveTrumps = false;

            Dictionary<Suit, int> otherHandsMaxBySuit = new()
                {
                    { Suit.Clubs, 0 },
                    { Suit.Diamonds, 0 },
                    { Suit.Hearts, 0 },
                    { Suit.Spades, 0 }
                };


            for (int i = 0; i < 4; i++) // player
            {
                if (i == player)
                {
                    continue;
                }

                for (int j = 0; j < 8; j++) // card
                {
                    if (Hand[i][j].Played || Hand[i][j].Suit is not Suit suit)
                    {
                        continue;
                    }

                    int strength = BelotHelpers.GetCardStrength(Hand[i][j], RoundCall, suit);

                    if (strength > otherHandsMaxBySuit[suit])
                    {
                        otherHandsMaxBySuit[suit] = strength;
                    }

                    if (BelotHelpers.IsSuit(RoundCall) && suit == (Suit)RoundCall)
                    {
                        otherHandsHaveTrumps = true;
                    }
                }
            }

            for (int i = 0; i < 8; i++)
            {
                if (Hand[player][i].Played || Hand[player][i].Suit is not Suit suit)
                {
                    continue;
                }

                int myStrength = BelotHelpers.GetCardStrength(Hand[player][i], RoundCall, suit);

                if ((int)suit != (int)RoundCall && !otherHandsHaveTrumps && myStrength > otherHandsMaxBySuit[suit])
                {
                    winners[i] = 2;
                }
                else if ((int)suit != (int)RoundCall && otherHandsHaveTrumps && myStrength > otherHandsMaxBySuit[suit])
                {
                    winners[i] = 1;
                }
                else if ((int)suit == (int)RoundCall && myStrength > otherHandsMaxBySuit[suit])
                {
                    winners[i] = 2;
                }
            }

            return winners;
        }

        //public int RemainingCardsInASuit(int suit, int player)
        //{
        //    int cards = 0;
        //    for (int i = 0; i < 8; i++)
        //    {
        //        if (suit == BelotHelpers.GetSuitFromCard(Hand[player][i])) cards++;
        //    }
        //    return cards;
        //}

        public int[] InvalidatePlayedCards(int[] validcards)
        {
            for (int i = 0; i < 8; i++)
            {
                if (Hand[Turn][i].Played)
                {
                    validcards[i] = 0;
                }
            }
            return validcards;
        }

        public bool PlayerHasCardsOfSuit(Suit suit)
        {
            return Hand[Turn].Any(c => c.Suit == suit && !c.Played);
        }

        public int[] InvalidateCardsNotOfSuit(int[] validcards, Suit suit)
        {
            for (int i = 0; i < 8; i++)
            {
                if (validcards[i] == 1)
                {
                    if (Hand[Turn][i].Suit != suit)
                    {
                        validcards[i] = 0;
                    }
                }
            }
            return validcards;
        }

        public bool PlayerHasHigherTrump()
        {
            foreach (var card in Hand[Turn])
            {
                if (TrumpStrength(card) > HighestTrumpInTrick)
                {
                    return true;
                }
            }
            return false;
        }

        public int[] InvalidateLowerTrumps(int[] validcards)
        {
            for (int i = 0; i < 8; i++)
            {
                if (validcards[i] == 1)
                {
                    if (TrumpStrength(Hand[Turn][i]) < HighestTrumpInTrick)
                    {
                        validcards[i] = 0;
                    }
                }
            }
            return validcards;
        }

        public int TrumpStrength(Card card)
        {
            int trumpstrength = 0;

            if (card.Played)
            {
                return trumpstrength;
            }

            if ((BelotHelpers.IsSuit(RoundCall) && card.Suit == (Suit)RoundCall) || (RoundCall == Call.AllTrumps && card.Suit == TrickSuit))
            {
                int index = Array.IndexOf(Enum.GetValues(typeof(Rank)), card.Rank);
                trumpstrength = BelotHelpers.onSuitTrumpStrength[index];
            }
            return trumpstrength;
        }

        public bool CheckBelot(Card card)
        {
            bool canDeclare = false;
            if (RoundCall != Call.NoTrumps)
            {
                if (card.Rank == Rank.Queen || card.Rank == Rank.King)
                {
                    var belots = Belots[Turn].Where(b => b.Suit == card.Suit && b.Declarable);
                    if (belots.Any())
                    {
                        belots.First().Declarable = false;
                        if (BelotHelpers.IsSuit(RoundCall) || (RoundCall == Call.AllTrumps && card.Suit == TrickSuit))
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
            if (BelotHelpers.IsSuit(RoundCall))
            {
                if (Belots[Turn].Where(s => s.Suit == (Suit)RoundCall).Count() > 0) Belots[Turn].Where(s => s.Suit == (Suit)RoundCall).First().Declared = declared;
                if (EnableLogging && declared) Log.Information("Belot: {0}", Turn);
            }
            else if (RoundCall == Call.AllTrumps)
            {
                if (Belots[Turn].Where(s => s.Suit == TrickSuit).Count() > 0) Belots[Turn].Where(s => s.Suit == TrickSuit).First().Declared = declared;
                if (EnableLogging && declared) Log.Information("Belot: {0}", Turn);
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
                    if (EnableLogging && Runs[Turn][i].Declared) Log.Information("{0}: {1}", BelotHelpers.GetRunNameFromLength(Runs[Turn][i].Length), Turn);
                }
            }
        }

        public void DeclareCarres(bool[] declared = null)
        {
            for (int i = 0; i < Carres[Turn].Count; i++)
            {
                if (declared == null) Carres[Turn][i].Declared = true;
                else Carres[Turn][i].Declared = declared[i];
                if (EnableLogging && Carres[Turn][i].Declared) Log.Information("Carre: {0}", Turn);
            }
        }

        public void PlayCard(Card card)
        {
            var handCard = Hand[Turn].FirstOrDefault(c => c.Suit == card.Suit && c.Rank == card.Rank);

            if (handCard != null)
            {
                card = handCard;
            }

            TableCards[Turn] = card;
            //if (EnableLogging) Log.Information("Play {0}, {1}", Turn, card);

            NumCardsPlayed++;

            TrickSuit ??= (Suit)card.Suit; // first card of a trick determines suit

            int trumpstrength = TrumpStrength(card);
            if (HighestTrumpInTrick < trumpstrength)
            {
                HighestTrumpInTrick = trumpstrength;
            }

            CardsPlayedThisRound.Add(BelotHelpers.GetCardNumber(card));

            card.Played = true;

            //if (EnableLogging && NumCardsPlayed % 4 == 0) Log.Information(GetDisplayName(DetermineWinner()) + " wins trick " + NumCardsPlayed / 4 + ", worth " + CalculateTrickPoints() + " points.");
        }

        #region Declarations

        public void FindRuns()
        {
            for (int i = 0; i < 4; i++)
            {
                List<Card> hand = OrderCardsForRuns(Hand[i]);
                for (int j = 0; j < 6; j++)
                {
                    int runLength = 1;
                    Suit suit = (Suit)hand[j].Suit;
                    Rank rank = (Rank)hand[j].Rank;
                    for (int k = 0; k < runLength; k++)
                    {
                        if (j + k + 1 > 7)
                        {
                            break;
                        }

                        if (hand[j + k + 1].Suit == suit) // if two adjacent cards are of the same suit
                        {

                            if (hand[j + k + 1].Rank == rank + k + 1) // if second card is adjacent in rank to the first card
                            {
                                runLength++; // consider the next card
                            }
                        }
                    }

                    if (runLength == 8)
                    {
                        Runs[i].Add(new Run(3, suit, Rank.Nine, false, false));
                        Runs[i].Add(new Run(5, suit, Rank.Ace, false, false));
                    }
                    else if (runLength > 2 && runLength < 6)
                    {
                        Runs[i].Add(new Run(runLength, suit, rank + runLength - 1, false, false));
                    }
                    else if (runLength > 5)
                    {
                        Runs[i].Add(new Run(5, suit, rank + runLength - 1, false, false));
                    }
                    j += runLength - 1;
                }
            }
        }

        public void FindCarres()
        {
            Rank[] validCarreRanks = [Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King, Rank.Ace];

            for (int i = 0; i < 4; i++) // players
            {
                foreach (var rank in validCarreRanks)
                {
                    if (Hand[i].Count(c => c.Rank == rank) == 4)
                    {
                        Carres[i].Add(new Carre(rank, false));
                    }
                }
            }
        }

        public void TruncateRuns() // reduce overlapping runs where this still rewards points, and invalidate other runs
        {
            for (int i = 0; i < 4; i++) // players
            {
                for (int j = 0; j < Runs[i].Count; j++)
                {
                    Rank upper = Runs[i][j].Rank;
                    Rank lower = upper - Runs[i][j].Length + 1;
                    if (Carres[i].Count == 1) // if zero carres: no overlap, if two: can't have any runs
                    {
                        if (Carres[i][0].Rank >= lower && Carres[i][0].Rank <= upper)
                        {
                            if (Runs[i][j].Length == 3)
                            {
                                Runs[i][j].Declarable = false;
                            }
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
                                    if (last)
                                    {
                                        Runs[i][j].Rank -= 1;
                                    }
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
                                        if (secondLast)
                                        {
                                            Runs[i][j].Rank -= 2;
                                        }
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
            for (int i = 0; i < 4; i++) // players
            {
                foreach (var suit in new Suit[] { Suit.Clubs, Suit.Diamonds, Suit.Hearts, Suit.Spades })
                {
                    if ((BelotHelpers.IsSuit(RoundCall) && (Suit)RoundCall == suit) || RoundCall == Call.AllTrumps)
                    {
                        if (Hand[i].Any(c => c.Rank == Rank.Queen && c.Suit == suit) && Hand[i].Any(c => c.Rank == Rank.King && c.Suit == suit))
                        {
                            Belots[i].Add(new Belot(suit, false, true));
                        }
                    }
                }
            }
        }

        #endregion

        public int DetermineWinner()
        {
            int winner = Turn;
            int bestValue = 0;

            if (TrickSuit != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (!TableCards[i].IsNull())
                    {
                        int value = BelotHelpers.GetCardStrength(TableCards[i], RoundCall, TrickSuit);
                        if (value > bestValue)
                        {
                            bestValue = value;
                            winner = i;
                        }
                    }
                }
            }
            if (EnableLogging && !TableCards.Any(c => c.IsNull()))
            {
                //Log.Information("Play: {0}", String.Join(",", TableCards));
                Log.Information("Play: {0} XXXXXXXXXX");
                Log.Information("Trick: {0}", winner);
            }
            return winner;
        }

        public int CalculateTrickPoints() // C,D,H,S = 162 (65 vs 97 would result in 7 & 10 in pure rounding), A = 260 (x2), J = 258
        {
            int points = 0;

            for (int i = 0; i < 4; i++)
            {
                points += CalculateCardPoints(TableCards[i]);
            }

            if (NumCardsPlayed == 32) points += 10;

            return points;
        }

        public int CalculateCardPoints(Card card) // C,D,H,S = 162 (65 vs 97 would result in 7 & 10 in pure rounding), A = 260 (x2), J = 258
        {
            int[] nontrump = [0, 0, 0, 10, 2, 3, 4, 11];
            int[] trump = [0, 0, 14, 10, 20, 3, 4, 11];

            int points = 0;

            if (card.Suit is not Suit suit || card.Rank is not Rank rank)
            {
                return points;
            }

            if ((BelotHelpers.IsSuit(RoundCall) && (Suit)RoundCall == suit) || RoundCall == Call.AllTrumps)
            {
                points += trump[(int)rank];
            }
            else
            {
                points += nontrump[(int)rank];
            }

            return points;
        }

        public string FinalisePoints()
        {
            TrickPoints = [EWRoundPoints, NSRoundPoints];
            DeclarationPoints = [0, 0];
            BelotPoints = [0, 0];
            Result = ["", "Success"];
            string[] message = ["N/S", "call", "succeeded"];
            if (EWCalled)
            {
                Result = ["Success", ""];
                message[0] = "E/W";
            }

            if (RoundCall == Call.NoTrumps) // no-trumps points are always doubled
            {
                EWRoundPoints *= 2;
                NSRoundPoints *= 2;
            }
            else
            {
                // Runs
                List<Run> EWRuns = [.. Runs[0].Where(d => d.Declared), .. Runs[2].Where(d => d.Declared)];
                List<Run> NSRuns = [.. Runs[1].Where(d => d.Declared), .. Runs[3].Where(d => d.Declared)];
                if (EWRuns.Count + NSRuns.Count > 0)
                {
                    // Compare runs by length first, and then by rank in case of a tie
                    int[] runComparer = [0, 0]; // EW, NS
                    if (EWRuns.Count > 0)
                    {
                        runComparer[0] = EWRuns.OrderByDescending(r => r.Length).First().Length;
                    }

                    if (NSRuns.Count > 0)
                    {
                        runComparer[1] = NSRuns.OrderByDescending(r => r.Length).First().Length;
                    }

                    if (runComparer[0] == runComparer[1])
                    {
                        runComparer[0] = (int)EWRuns.Where(r => r.Length == runComparer[0]).OrderByDescending(r => r.Rank).First().Rank;
                        runComparer[1] = (int)NSRuns.Where(r => r.Length == runComparer[1]).OrderByDescending(r => r.Rank).First().Rank;
                    }

                    if (runComparer[0] == runComparer[1])
                    {
                        if (EnableLogging) Log.Information("The Runs were tied. No extra points awarded for Runs.");
                    }
                    else if (runComparer[0] > runComparer[1])
                    {
                        DeclarationPoints[0] += 20 * EWRuns.Count(r => r.Length == 3);
                        DeclarationPoints[0] += 50 * EWRuns.Count(r => r.Length == 4);
                        DeclarationPoints[0] += 100 * EWRuns.Count(r => r.Length == 5);
                    }
                    else
                    {
                        DeclarationPoints[1] += 20 * NSRuns.Count(r => r.Length == 3);
                        DeclarationPoints[1] += 50 * NSRuns.Count(r => r.Length == 4);
                        DeclarationPoints[1] += 100 * NSRuns.Count(r => r.Length == 5);
                    }
                }

                // Carres
                List<Carre> EWCarres = [.. Carres[0].Where(d => d.Declared), .. Carres[2].Where(d => d.Declared)];
                List<Carre> NSCarres = [.. Carres[1].Where(d => d.Declared), .. Carres[3].Where(d => d.Declared)];
                if (EWCarres.Count + NSCarres.Count > 0)
                {
                    int[] carreStrength = [0, 0, 5, 3, 6, 1, 2, 4];
                    int[] carreComparer = [0, 0]; // EW, NS

                    if (EWCarres.Count > 0)
                    {
                        carreComparer[0] = carreStrength[(int)EWCarres.OrderByDescending(r => r.Rank).First().Rank];
                    }

                    if (NSCarres.Count > 0)
                    {
                        carreComparer[1] = carreStrength[(int)NSCarres.OrderByDescending(r => r.Rank).First().Rank];
                    }

                    if (carreComparer[0] > carreComparer[1])
                    {
                        DeclarationPoints[0] += 200 * EWCarres.Count(c => c.Rank == Rank.Jack);
                        DeclarationPoints[0] += 150 * EWCarres.Count(c => c.Rank == Rank.Nine);
                        DeclarationPoints[0] += 100 * EWCarres.Count(c => c.Rank != Rank.Jack && c.Rank != Rank.Nine);
                    }
                    else
                    {
                        DeclarationPoints[1] += 200 * NSCarres.Count(c => c.Rank == Rank.Jack);
                        DeclarationPoints[1] += 150 * NSCarres.Count(c => c.Rank == Rank.Nine);
                        DeclarationPoints[1] += 100 * NSCarres.Count(c => c.Rank != Rank.Jack && c.Rank != Rank.Nine);
                    }
                }

                EWRoundPoints += DeclarationPoints[0];
                NSRoundPoints += DeclarationPoints[1];

                // Belots
                BelotPoints[0] += 20 * (Belots[0].Count(d => d.Declared) + Belots[2].Count(d => d.Declared));
                BelotPoints[1] += 20 * (Belots[1].Count(d => d.Declared) + Belots[3].Count(d => d.Declared));

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
            else
            {
                Capot = false;
            }

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

            //if (EWRoundPoints > 1000 || NSRoundPoints > 1000)
            //{

            //}

            EWTotal += EWRoundPoints;
            NSTotal += NSRoundPoints;

            if (EnableLogging)
            {
                Log.Information(String.Join(" ", message) + ".");
                Log.Information("Round: {0},{1}", NSRoundPoints, EWRoundPoints);
            }

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
            if (Log != null)
            {
                Log.Dispose();
                if (!IsNewGame)
                {
                    string source = LogPath + GameId + ".txt";
                    string destination = LogPath + "Incomplete\\" + GameId + ".txt";
                    if (File.Exists(source) && !File.Exists(destination))
                    {
                        try
                        {
                            File.Move(source, destination);
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }
            }
        }
    }

    public class BelotLobbyGame
    {
        public BelotLobbyGame(BelotGame g)
        {
            West = g.Players[0].Username != "" ? g.GetDisplayName(0) : "Empty";
            North = g.Players[1].Username != "" ? g.GetDisplayName(1) : "Empty";
            East = g.Players[2].Username != "" ? g.GetDisplayName(2) : "Empty";
            South = g.Players[3].Username != "" ? g.GetDisplayName(3) : "Empty";
            Started = !g.IsNewGame;
            RoomId = g.RoomId;
        }
        public string West { get; set; }
        public string North { get; set; }
        public string East { get; set; }
        public string South { get; set; }
        public bool Started { get; set; }
        public string RoomId { get; set; }
    }

}