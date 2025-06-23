using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using BelotWebApp.BelotClasses.Players;
using BelotWebApp.BelotClasses.Replays;
using BelotWebApp.Configuration;
using System.Text.Json;

namespace BelotWebApp.BelotClasses
{
    public class BelotGame
    {
        public BelotGame(Player[] players, string roomId, string? logPath = null)
        {
            IsRunning = true;
            Players = players;
            RoomId = roomId;
            Spectators = [];
            RecordReplay = !string.IsNullOrEmpty(logPath);
            LogPath = logPath;
        }

        public static readonly int scoreTarget = 1501;

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
        public List<Declaration> Declarations { get; set; }
        public bool IsNewRound { get; set; } = true;
        public bool IsNewGame { get; set; } = true;
        public bool WaitDeal { get; set; }
        public bool WaitCall { get; set; }
        public bool WaitCard { get; set; }
        public bool IsRunning { get; set; }
        public string? LogPath { get; set; }
        public bool RecordReplay { get; set; }
        public BelotStateDiff ReplayState { get; set; }

        public int WinnerDelay { get; set; } = 400;
        public int BotDelay { get; set; } = 800;
        public int RoundSummaryDelay { get; set; } = 6000;

        private static readonly Random rnd = new();

        public void NewGame()
        {
            GameId = Guid.NewGuid().ToString();
            lock (rnd)
            {
                FirstPlayer = rnd.Next(4);
            }
            //FirstPlayer = 0;
            WaitDeal = false;
            WaitCall = false;
            WaitCard = false;
            IsNewRound = true;
            EWTotal = 0;
            NSTotal = 0;
            ScoreHistory = [];

            if (RecordReplay)
            {
                SetLogger();
                AddInitialState();
            }
        }

        public void NewRound() // set new first player
        {
            Rounds++;
            Turn = FirstPlayer;

            if (--FirstPlayer == -1) FirstPlayer = 3;
            Deck = [];
            Hand = new List<Card>[4];
            Declarations = [];
            for (int i = 0; i < 4; i++)
            {
                Hand[i] = [];
            }
            TableCards = [new(), new(), new(), new()];
            CardsPlayedThisRound = [];
            CardsDealt = 0;
            NumCardsPlayed = 0;
            TrickSuit = null;
            HighestTrumpInTrick = 0;
            RoundCall = Call.Pass;
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
                lock (rnd)
                {
                    p = rnd.Next(masterDeck.Count);
                }
                Deck.Add(masterDeck[p]);
                masterDeck.RemoveAt(p);
            }
            //Deck = [
            //    new(Suit.Clubs, Rank.Seven), new(Suit.Clubs, Rank.Eight), new(Suit.Clubs, Rank.Nine), new(Suit.Clubs, Rank.Ten), new(Suit.Clubs, Rank.Jack),
            //    new(Suit.Diamonds, Rank.Seven), new(Suit.Diamonds, Rank.Eight), new(Suit.Diamonds, Rank.Ten), new(Suit.Diamonds, Rank.Jack), new(Suit.Diamonds, Rank.Queen),
            //    new(Suit.Hearts, Rank.Seven), new(Suit.Hearts, Rank.Eight), new(Suit.Hearts, Rank.Ten), new(Suit.Hearts, Rank.Jack), new(Suit.Hearts, Rank.Queen),
            //    new(Suit.Spades, Rank.Seven), new(Suit.Spades, Rank.Eight), new(Suit.Spades, Rank.Ten), new(Suit.Spades, Rank.Jack), new(Suit.Spades, Rank.Queen),
            //    new(Suit.Diamonds, Rank.Nine), new(Suit.Hearts, Rank.Nine), new(Suit.Spades, Rank.Nine),
            //    new(Suit.Clubs, Rank.Queen), new(Suit.Clubs, Rank.King), new(Suit.Diamonds, Rank.King),
            //    new(Suit.Hearts, Rank.King), new(Suit.Spades, Rank.King), new(Suit.Clubs, Rank.Ace),
            //    new(Suit.Diamonds, Rank.Ace), new(Suit.Hearts, Rank.Ace), new(Suit.Spades, Rank.Ace) ];
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
            List<ReplayHandCard>? oldHandCards = null;

            if (RecordReplay) // Hand is already cleared by this point when dealing 5 -> use ReplayState to record the before state of the hand when dealing 5
            {
                oldHandCards = numCards == 3 ? BelotReplayDiff.CopyHandCards(Hand) : ReplayState.HandCards;
            }

            Turn = FirstPlayer;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < numCards; j++)
                {
                    Hand[Turn].Add(Deck[CardsDealt++]);
                }

                Hand[Turn] = OrderCardsForHand(Hand[Turn]);

                if (--Turn == -1) Turn = 3;
            }

            if (RecordReplay)
            {
                BelotReplayDiff diff = new();

                if (numCards == 5)
                {
                    diff.SetCaller(ReplayState, 4); // no caller

                    diff.SetRoundCall(ReplayState, Call.NoCall);

                    if (NSTotal != ReplayState.Scores[0] || EWTotal != ReplayState.Scores[1])
                    {
                        diff.Before.Scores = ReplayState.Scores;
                        diff.After.Scores = [NSTotal, EWTotal];
                    }

                    ReplayState.HandCards = BelotReplayDiff.CopyHandCards(Hand);
                }

                diff.SetHandCards(oldHandCards, Hand);

                diff.ClearEmotes(ReplayState);

                int turn = FirstPlayer == 3 ? 0 : FirstPlayer + 1;
                diff.SetTurn(ReplayState, turn);

                diff.SetDealer(ReplayState, turn);

                AddState(diff);
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
            if (RecordReplay)
            {
                BelotReplayDiff diff = new();

                diff.SetEmote(ReplayState, Turn, ((int)call).ToString()); // JS checks if the emote is a number to determine the icon/symbol/text to show (as it can also be a declaration, throw cards etc)

                diff.SetTurn(ReplayState, Turn);

                if (call > Call.Pass)
                {
                    diff.SetCaller(ReplayState, Turn);

                    if (call <= Call.AllTrumps)
                    {
                        diff.SetRoundCall(ReplayState, call);
                    }
                }

                AddState(diff);
            }

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
            return RoundCall == Call.FiveUnderNine || (Calls.Count > 3 && Calls.TakeLast(3).All(c => c == Call.Pass));
        }

        public int[] ValidCards()
        {
            int[] validCards = [1, 1, 1, 1, 1, 1, 1, 1];
            validCards = InvalidatePlayedCards(validCards);
            if (TrickSuit != null) // if it's not the first card of the trick
            {
                if (BelotHelpers.IsSuit(RoundCall) && (Suit)RoundCall == TrickSuit && PlayerHasCardsOfSuit((Suit)RoundCall)) // RoundSuit is C,D,H,S
                {
                    validCards = InvalidateCardsNotOfSuit(validCards, (Suit)RoundCall);
                    if (PlayerHasHigherTrump())
                    {
                        validCards = InvalidateLowerTrumps(validCards);
                    }
                }
                else if (PlayerHasCardsOfSuit((Suit)TrickSuit))
                {
                    validCards = InvalidateCardsNotOfSuit(validCards, (Suit)TrickSuit);
                    if (RoundCall == Call.AllTrumps && PlayerHasHigherTrump())
                    {
                        validCards = InvalidateLowerTrumps(validCards);
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
                            validCards = InvalidateLowerTrumps(validCards);
                        }
                    }
                }
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

        public void DeclareCurrentDeclarables(List<Belot>? belots = null, List<Run>? runs = null, List<Carre>? carres = null)
        {
            belots ??= Declarations.Where(b => b is Belot).Cast<Belot>().ToList();
            runs ??= Declarations.Where(b => b is Run).Cast<Run>().ToList();
            carres ??= Declarations.Where(b => b is Carre).Cast<Carre>().ToList();

            foreach (var belot in belots)
            {
                belot.Declared = true;
            }

            foreach (var run in runs)
            {
                run.Declared = true;
            }

            foreach (var carre in carres)
            {
                carre.Declared = true;
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

            NumCardsPlayed++;

            TrickSuit ??= (Suit)card.Suit; // first card of a trick determines suit

            int trumpstrength = TrumpStrength(card);
            if (HighestTrumpInTrick < trumpstrength)
            {
                HighestTrumpInTrick = trumpstrength;
            }

            CardsPlayedThisRound.Add(BelotHelpers.GetCardIndex(card));

            card.Played = true;
        }

        #region Declarations

        public void FindCarres()
        {
            Rank[] validCarreRanks = [Rank.Nine, Rank.Ten, Rank.Jack, Rank.Queen, Rank.King, Rank.Ace];

            for (int i = 0; i < 4; i++) // players
            {
                foreach (var rank in validCarreRanks)
                {
                    if (Hand[i].Count(c => c.Rank == rank) == 4)
                    {
                        Declarations.Add(new Carre(this, i, rank));
                    }
                }
            }
        }

        public void FindRuns()
        {
            for (int i = 0; i < 4; i++) // players
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
                        Declarations.Add(new Run(this, i, 3, suit, Rank.Nine).TruncateAndValidate());
                        Declarations.Add(new Run(this, i, 5, suit, Rank.Ace).TruncateAndValidate());
                    }
                    else if (runLength > 2 && runLength < 6)
                    {
                        Declarations.Add(new Run(this, i, runLength, suit, rank + runLength - 1).TruncateAndValidate());
                    }
                    else if (runLength > 5)
                    {
                        Declarations.Add(new Run(this, i, 5, suit, rank + runLength - 1).TruncateAndValidate());
                    }
                    j += runLength - 1;
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
                        if (Hand[i].Count(c => c.Suit == suit && (c.Rank == Rank.Queen || c.Rank == Rank.King)) == 2)
                        {
                            Declarations.Add(new Belot(this, i, suit));
                        }
                    }
                }
            }
        }

        public List<Declaration> DeclareDeclarations(IEnumerable<Declaration> declarations)
        {
            List<Declaration> declaredDeclarations = [];

            foreach (var declaration in declarations.Where(d => d.Declared))
            {
                switch (declaration)
                {
                    case Belot declaredBelot:
                        var existingBelot = Declarations.OfType<Belot>().FirstOrDefault(b => b.Player == Turn && b.IsDeclarable && b.Suit == declaredBelot.Suit);
                        if (existingBelot != null)
                        {
                            existingBelot.Declared = true;
                            declaredDeclarations.Add(existingBelot);
                        }
                        break;
                    case Carre declaredCarre:
                        var existingCarre = Declarations.OfType<Carre>().FirstOrDefault(c => c.Player == Turn && c.IsDeclarable && c.Rank == declaredCarre.Rank);
                        if (existingCarre != null)
                        {
                            existingCarre.Declared = true;
                            declaredDeclarations.Add(existingCarre);
                        }
                        break;
                    case Run declaredRun:
                        var existingRun = Declarations.OfType<Run>().FirstOrDefault(r => r.Player == Turn && r.IsDeclarable && r.IsValid && r.Suit == declaredRun.Suit && r.Rank == declaredRun.Rank);
                        if (existingRun != null)
                        {
                            existingRun.Declared = true;
                            declaredDeclarations.Add(existingRun);
                        }
                        break;
                }
            }
            return declaredDeclarations;
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
                var EWRuns = Declarations.Where(r => r is Run && (r.Player == 0 || r.Player == 2) && r.Declared).Cast<Run>();
                var NSRuns = Declarations.Where(r => r is Run && (r.Player == 1 || r.Player == 3) && r.Declared).Cast<Run>();
                if (EWRuns.Count() + NSRuns.Count() > 0)
                {
                    // Compare runs by length first, and then by rank in case of a tie
                    int[] runComparer = [0, 0]; // EW, NS
                    if (EWRuns.Any())
                    {
                        runComparer[0] = EWRuns.OrderByDescending(r => r.Length).First().Length;
                    }

                    if (NSRuns.Any())
                    {
                        runComparer[1] = NSRuns.OrderByDescending(r => r.Length).First().Length;
                    }

                    if (runComparer[0] == runComparer[1])
                    {
                        runComparer[0] = (int)EWRuns.Where(r => r.Length == runComparer[0]).OrderByDescending(r => r.Rank).First().Rank;
                        runComparer[1] = (int)NSRuns.Where(r => r.Length == runComparer[1]).OrderByDescending(r => r.Rank).First().Rank;
                    }

                    // no points for a tie
                    if (runComparer[0] > runComparer[1])
                    {
                        DeclarationPoints[0] += 20 * EWRuns.Count(r => r.Length == 3);
                        DeclarationPoints[0] += 50 * EWRuns.Count(r => r.Length == 4);
                        DeclarationPoints[0] += 100 * EWRuns.Count(r => r.Length == 5);
                    }
                    else if (runComparer[0] < runComparer[1])
                    {
                        DeclarationPoints[1] += 20 * NSRuns.Count(r => r.Length == 3);
                        DeclarationPoints[1] += 50 * NSRuns.Count(r => r.Length == 4);
                        DeclarationPoints[1] += 100 * NSRuns.Count(r => r.Length == 5);
                    }
                }

                // Carres
                var EWCarres = Declarations.Where(c => c is Carre && (c.Player == 0 || c.Player == 2) && c.Declared).Cast<Carre>();
                var NSCarres = Declarations.Where(c => c is Carre && (c.Player == 1 || c.Player == 3) && c.Declared).Cast<Carre>();
                if (EWCarres.Count() + NSCarres.Count() > 0)
                {
                    int[] carreStrength = [0, 0, 5, 3, 6, 1, 2, 4];
                    int[] carreComparer = [0, 0]; // EW, NS

                    if (EWCarres.Any())
                    {
                        carreComparer[0] = carreStrength[(int)EWCarres.OrderByDescending(r => r.Rank).First().Rank];
                    }

                    if (NSCarres.Any())
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
                var EWBelots = Declarations.Where(b => b is Belot && (b.Player == 0 || b.Player == 2) && b.Declared).Cast<Belot>();
                var NSBelots = Declarations.Where(b => b is Belot && (b.Player == 1 || b.Player == 3) && b.Declared).Cast<Belot>();
                BelotPoints[0] += 20 * EWBelots.Count();
                BelotPoints[1] += 20 * NSBelots.Count();

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

            return String.Join(" ", message) + ".";
        }

        public string GetBotName(int pos)
        {
            string[] seat = { "West", "North", "East", "South" };
            return "Robot " + seat[pos];
        }

        public string GetDisplayName(int pos)
        {
            if (Players[pos].PlayerType == PlayerType.Human)
            {
                return Players[pos].Username;
            }
            else
            {
                return GetBotName(pos);
            }
        }

        #region Replay

        public void SetLogger()
        {
            var logPath = Path.Combine(LogPath, GameId + ".txt");
            File.Create(logPath).Close();
        }

        public void AddInitialState()
        {
            var logPath = Path.Combine(LogPath, GameId + ".txt");

            ReplayState = new()
            {
                Players = Enumerable.Range(0, 4).Select(GetDisplayName).ToArray(),
                Scores = [EWTotal, NSTotal],
                Dealer = FirstPlayer,
                RoundCall = Call.NoCall,
                Caller = 4, // no caller
                Turn = FirstPlayer
            };

            File.AppendAllText(logPath, JsonSerializer.Serialize(new BelotReplayDiff
            {
                Before = null,
                After = ReplayState
            }, JsonSettings.Compact) + "\n");

            ReplayState.Emotes = [];
        }

        public void AddState(BelotReplayDiff diff)
        {
            if (IsRunning)
            {
                var logPath = Path.Combine(LogPath, GameId + ".txt");

                File.AppendAllText(logPath, JsonSerializer.Serialize(diff, JsonSettings.Compact) + "\n");

                ApplyDiff(diff.After);
            }
        }

        public void RecordCardPlayed(List<string> emotes) // one frame including hand change, tableCard change, and declaration emotes
        {
            if (RecordReplay)
            {
                BelotReplayDiff diff = new();

                if (emotes.Count > 0) // clears previous turn declarations if any
                {
                    diff.SetEmote(ReplayState, Turn, string.Join("\n", emotes)); // JS checks if the emote is a number to determine the icon/symbol/text to show (as it can also be a declaration, throw cards etc)
                }
                else
                {
                    diff.ClearEmotes(ReplayState);
                }

                var card = TableCards[Turn];
                var pos = Hand[Turn].IndexOf(card);

                diff.SetTableCard(Turn, TableCards[Turn]);
                diff.SetHandCard(Turn, pos, TableCards[Turn]);

                diff.Before.Turn = ReplayState.Turn;
                diff.After.Turn = Turn;

                AddState(diff);
            }
        }

        public void RecordTrickEnd()
        {
            if (RecordReplay)
            {
                BelotReplayDiff diff = new();

                if (NumCardsPlayed == 32 && Hand.Any(h => h.Any(c => !c.Played))) // cards thrown
                {
                    diff.SetEmote(ReplayState, Turn, "Throw");
                    ReplayState.HandCards = BelotReplayDiff.CopyHandCards(Hand);
                }
                else
                {
                    diff.ClearEmotes(ReplayState);
                }

                diff.ClearTableCards(TableCards);

                diff.SetTurn(ReplayState, Turn);

                AddState(diff);
            }
        }

        public void RecordGameEnd()
        {
            if (RecordReplay)
            {
                BelotReplayDiff diff = new();

                diff.ClearEmotes(ReplayState);

                diff.ClearTableCards(TableCards);

                diff.SetCaller(ReplayState, 4);

                diff.SetRoundCall(ReplayState, Call.NoCall);

                diff.Before.Scores = ReplayState.Scores;
                diff.After.Scores = [NSTotal, EWTotal];

                AddState(diff);
            }
        }

        public void CloseLog()
        {
            if (!IsNewGame)
            {
                var source = Path.Combine(LogPath, GameId + ".txt");
                string destination = Path.Combine(LogPath, "Incomplete", GameId + ".txt");

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

        private void ApplyDiff(BelotStateDiff diff)
        {
            if (diff.Scores != null)
            {
                ReplayState.Scores = diff.Scores;
            }
            if (diff.Dealer != null)
            {
                ReplayState.Dealer = diff.Dealer;
            }
            if (diff.RoundCall != null)
            {
                ReplayState.RoundCall = (Call)diff.RoundCall;
            }
            if (diff.Caller != null)
            {
                ReplayState.Caller = diff.Caller;
            }
            if (diff.Turn != null)
            {
                ReplayState.Turn = diff.Turn;
            }
            if (diff.Emotes != null)
            {
                ReplayState.Emotes = diff.Emotes;
            }
        }

        #endregion
    }
}