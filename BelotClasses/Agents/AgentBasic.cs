using BelotWebApp.BelotClasses.Cards;
using System.Diagnostics;

namespace BelotWebApp.BelotClasses.Agents
{
    public static class AgentBasic
    {
        // ignores extra points in suit nomination (for now)
        // doesn't double or redouble
        // always accepts all available extras
        // never throws the cards
        // never calls five-under-nine

        private static readonly Random globalSeedRng = new();
        private static readonly ThreadLocal<Random> threadLocalRng = new(() =>
        {
            int seed;
            lock (globalSeedRng)
            {
                seed = globalSeedRng.Next();
            }
            return new Random(seed);
        });

        public static Call CallSuit(List<Card> hand, int[] validCalls)
        {
            const double NO_TRUMP_MULTIPLIER = 1.5;
            const double SINGLE_TRUMP_THRESHOLD = 10;
            const double NO_TRUMP_THRESHOLD = 14.5;
            const double ALL_TRUMP_THRESHOLD = 15;

            Call bestCall = Call.Pass;
            double highestScore = 0;

            int[] singleTrumpValue = [1, 1, 2, 2, 4, 1, 1, 2];
            int[] noTrumpValue = [0, 0, 0, 1, 0, 0, 0, 3];
            int[] allTrumpValue = [0, 0, 2, 0, 4, 0, 0, 1];

            Call[] calls = [Call.Clubs, Call.Diamonds, Call.Hearts, Call.Spades, Call.NoTrumps, Call.AllTrumps];

            for (int i = 0; i < calls.Length; i++)
            {
                if (validCalls[i] != 1)
                {
                    continue;
                }

                Call currentCall = calls[i];
                double score = 0;
                int trumpLength = 0;
                var j9Tracker = new Dictionary<Suit, HashSet<Rank>>();

                foreach (var card in hand)
                {
                    if (card.Played || card.Suit is not Suit suit || card.Rank is not Rank rank)
                    {
                        continue;
                    }

                    if (BelotHelpers.IsSuit(currentCall) && suit == (Suit)currentCall)
                    {
                        trumpLength++;
                        score += singleTrumpValue[(int)rank];

                        if (rank is Rank.Jack or Rank.Nine)
                        {
                            if (!j9Tracker.TryGetValue(suit, out HashSet<Rank>? value))
                            {
                                value = ([]);
                                j9Tracker[suit] = value;
                            }

                            value.Add(rank);

                            if (value.Count == 2)
                            {
                                score += 1;
                            }
                        }
                    }
                    else if (currentCall == Call.NoTrumps)
                    {
                        score += noTrumpValue[(int)rank];
                    }
                    else if (currentCall == Call.AllTrumps)
                    {
                        score += allTrumpValue[(int)rank];

                        if (!j9Tracker.ContainsKey(suit))
                        {
                            j9Tracker[suit] = new HashSet<Rank>();
                        }

                        if (rank is Rank.Jack or Rank.Nine)
                        {
                            j9Tracker[suit].Add(rank);
                            if (j9Tracker[suit].Contains(Rank.Jack) && j9Tracker[suit].Contains(Rank.Nine))
                            {
                                score += 2;
                            }
                        }
                    }
                }

                if (trumpLength > 2)
                    score += trumpLength;

                if (currentCall == Call.NoTrumps)
                    score *= NO_TRUMP_MULTIPLIER;

                if (score > highestScore)
                {
                    highestScore = score;
                    bool isThresholdMet =
                        currentCall < Call.NoTrumps && score > SINGLE_TRUMP_THRESHOLD ||
                        currentCall == Call.NoTrumps && score > NO_TRUMP_THRESHOLD ||
                        currentCall == Call.AllTrumps && score > ALL_TRUMP_THRESHOLD;

                    if (isThresholdMet)
                    {
                        bestCall = currentCall;
                    }
                }
            }

            return bestCall;
        }

        public static Card SelectCard(List<Card> hand, int[] validCards, int[] winners, Card[] tableCards, int turn, int curWinner, Call roundCall, Suit? trickSuit, bool ewCalled, int caller)
        {
            int validCount = validCards.Sum();
            if (validCount == 1)
            {
                int fallback = Array.IndexOf(validCards, 1);
                Debug.Print("Only valid card");
                return hand[fallback];
            }

            int trickNumber = hand.Count(c => c.Played);
            int cardsPlayed = tableCards.Count(c => !c.IsNull());

            if (cardsPlayed == 0)
            {
                return SelectCardFirst(hand, validCards, winners, trickNumber, turn, roundCall, ewCalled, caller);
            }
            else if (cardsPlayed == 1)
            {
                int prevTurn = turn == 3 ? 0 : turn + 1;
                return SelectCardSecond(hand, validCards, winners, tableCards[prevTurn], roundCall, trickSuit);
            }
            else // 3rd or 4th to play
            {
                if (turn % 2 != curWinner % 2) // other team is currently winning trick
                {
                    int chanceToTryWin = cardsPlayed == 2 ? 60 : 100;
                    return SelectCardWhenOpponentsWinning(hand, validCards, tableCards, roundCall, trickSuit, chanceToTryWin);
                }
                else // partner is currently winning trick
                {
                    int chanceToWin = cardsPlayed == 2 ? 65 : 80;
                    var hardWinCard = TryBeatPartnerWithHardWinner(hand, validCards, tableCards, winners, roundCall, trickSuit, chanceToWin);
                    if (hardWinCard != null)
                    {
                        Debug.Print("3,4: Beat partner with hard winner");
                        return hardWinCard;
                    }

                    if (cardsPlayed == 3)
                    {
                        return SelectDiscardWhenPartnerWinning(hand, validCards, winners, roundCall, trickSuit);
                    }
                    return SelectRandomValidCard(hand, validCards);
                }
            }
        }

        private static Card SelectCardFirst(List<Card> hand, int[] validCards, int[] winners, int trickNumber, int turn, Call roundCall, bool ewCalled, int caller)
        {
            if (MyTeamCalled(ewCalled, turn) && roundCall != Call.NoTrumps)
            {
                int? jass = TryPlayTrumpJack(hand, validCards, roundCall);
                if (jass.HasValue)
                {
                    Debug.Print("1: Play trump jack");
                    return hand[jass.Value];
                }
            }

            if (trickNumber < 3 && MyTeamCalled(ewCalled, turn) && turn != caller && BelotHelpers.IsSuit(roundCall) && Roll(80))
            {
                int? trumpLead = TryPlayTrumpControl(hand, validCards, (Suit)roundCall);
                if (trumpLead.HasValue)
                {
                    Debug.Print("1: Play high-value trump");
                    return hand[trumpLead.Value];
                }
            }

            if (roundCall < Call.AllTrumps)
            {
                int? ace = TryPlayNonTrumpAce(hand, validCards, roundCall);
                if (ace.HasValue)
                {
                    Debug.Print("1: Play nontrump ace");
                    return hand[ace.Value];
                }
            }

            int? hardWinner = TryLeadWinner(hand, validCards, winners, roundCall, winnerLevel: 2, chance: 90);
            if (hardWinner.HasValue)
            {
                Debug.Print("1: Play hard winner");
                return hand[hardWinner.Value];
            }

            int? softWinner = TryLeadWinner(hand, validCards, winners, roundCall, winnerLevel: 1, chance: 70);
            if (softWinner.HasValue)
            {
                Debug.Print("1: Play soft winner");
                return hand[softWinner.Value];
            }

            Debug.Print("1: Play random");
            return SelectRandomValidCard(hand, validCards);
        }

        private static Card SelectCardSecond(List<Card> hand, int[] validCards, int[] winners, Card leadCard, Call roundCall, Suit? trickSuit)
        {
            int leadStrength = BelotHelpers.GetCardStrength(leadCard, roundCall, trickSuit);

            List<int> hardWinners = [];
            List<int> softWinners = [];
            List<(int i, int power)> beaters = [];
            List<(int i, int power)> losers = [];

            for (int i = 0; i < hand.Count; i++)
            {
                if (validCards[i] == 0)
                {
                    continue;
                }

                int power = BelotHelpers.GetCardStrength(hand[i], roundCall, trickSuit);
                var suit = hand[i].Suit;

                if ((int?)suit != (int?)roundCall && suit == trickSuit) // non-trump only and of the led suit
                {
                    if (winners[i] == 2 && power > leadStrength)
                    {
                        hardWinners.Add(i);
                        continue;
                    }
                    if (winners[i] == 1 && power > leadStrength)
                    {
                        softWinners.Add(i);
                        continue;
                    }
                }

                if (power > leadStrength)
                {
                    beaters.Add((i, power));
                }
                else
                {
                    losers.Add((i, power));
                }
            }

            var rnd = threadLocalRng.Value!;

            if (hardWinners.Count > 0 && Roll(80))
            {
                Debug.Print("2: Play hard winner");
                return hand[hardWinners[rnd.Next(hardWinners.Count)]];
            }

            if (softWinners.Count > 0 && Roll(75))
            {
                Debug.Print("2: Play soft winner");
                return hand[softWinners[rnd.Next(softWinners.Count)]];
            }

            if (beaters.Count > 0 && Roll(70)) // 70% chance to try and beat the led card with the lowest strength beater
            {
                Debug.Print("2: Play lowest beater");
                int choice = beaters.OrderBy(c => c.power).First().i;
                return hand[choice];
            }

            if (losers.Count > 0 && Roll(90)) // If no winners, or decide not to try win, discard lowest valid option 90% of the time
            {
                Debug.Print("2: Play lowest loser");
                int choice = losers.OrderBy(c => c.power).First().i;
                return hand[choice];
            }

            Debug.Print("2: Play random");
            return SelectRandomValidCard(hand, validCards);
        }

        private static Card SelectCardWhenOpponentsWinning(List<Card> hand, int[] validCards, Card[] tableCards, Call roundCall, Suit? trickSuit, int chance)
        {
            int bestOnTable = tableCards.Where(c => !c.IsNull()).Max(c => BelotHelpers.GetCardStrength(c, roundCall, trickSuit)); // get highest winning power of cards played so far in trick

            List<int> winning = [];
            List<(int i, int power)> discards = [];

            for (int i = 0; i < hand.Count; i++)
            {
                if (validCards[i] == 0)
                {
                    continue;
                }

                int power = BelotHelpers.GetCardStrength(hand[i], roundCall, trickSuit);
                if (power > bestOnTable)
                {
                    winning.Add(i);
                }
                else
                {
                    discards.Add((i, power));
                }
            }

            var rnd = threadLocalRng.Value!;

            if (winning.Count > 0 && Roll(chance)) // if any of my cards can beat the current best trick card and I am last to play, play a random one of them. Do the same 60% of the time if I am 3rd to play
            {
                Debug.Print("3,4: Play random winner");
                return hand[winning[rnd.Next(winning.Count)]];
            }

            if (discards.Count > 0)
            {
                Debug.Print("3,4: Play lowest discard");
                return hand[discards.OrderBy(c => c.power).First().i];
            }

            Debug.Print("3,4: Play random");
            return SelectRandomValidCard(hand, validCards);
        }

        private static Card? TryBeatPartnerWithHardWinner(List<Card> hand, int[] validCards, Card[] tableCards, int[] winners, Call roundCall, Suit? trickSuit, int chance)
        {
            // must have a winner for current trick and at least one more winner for a future trick

            var validWinners = Enumerable.Range(0, hand.Count).Where(i => validCards[i] == 1 && winners[i] == 2).ToList();
            // validWinners doesn't need to consider strength of what has been played so far this trick, because it is also for future tricks

            if (validWinners.Count < 2) // current trick and at least one more
            {
                return null;
            }

            var bestOnTablePower = tableCards.Where(c => !c.IsNull()).Max(c => BelotHelpers.GetCardStrength(c, roundCall, trickSuit));

            // Get all hard winners in hand that are valid and can beat the winning card (including trumps), order by lowest strength
            var candidates = validWinners.Where(i => BelotHelpers.GetCardStrength(hand[i], roundCall, trickSuit) > bestOnTablePower && hand[i].Suit is Suit suit && (suit == trickSuit || (BelotHelpers.IsSuit(roundCall) && suit == (Suit)roundCall)))
                .OrderBy(c => BelotHelpers.GetCardStrength(hand[c], roundCall, trickSuit)).ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            if (validWinners.Count > 2)
            {
                if (Roll(chance + 10))
                {
                    return hand[candidates[0]];
                }
            }
            else if (validWinners.Count == 2)
            {
                if (Roll(chance))
                {
                    return hand[candidates[0]];
                }
            }

            return null;
        }

        private static Card SelectDiscardWhenPartnerWinning(List<Card> hand, int[] validCards, int[] winners, Call roundCall, Suit? trickSuit)
        {
            var candidates = Enumerable.Range(0, hand.Count).Where(i => validCards[i] == 1 && hand[i].Suit is Suit s && (int)s != (int)roundCall && winners[i] == 0)
                .OrderByDescending(i => BelotHelpers.GetCardStrength(hand[i], roundCall, trickSuit)).ToList(); // non-trump,  not winner

            if (candidates.Count > 0 && Roll(80))
            {
                Debug.Print("4: Play best nonwinner");
                return hand[candidates[0]]; // highest strength among candidates
            }

            Debug.Print("4: Play random");
            return SelectRandomValidCard(hand, validCards);
        }

        private static bool MyTeamCalled(bool ewCalled, int turn)
        {
            return (ewCalled && turn % 2 == 0) || (!ewCalled && turn % 2 == 1);
        }

        private static int? TryPlayTrumpJack(List<Card> hand, int[] validCards, Call roundCall)
        {
            var jacks = Enumerable.Range(0, hand.Count).Where(i => validCards[i] == 1 && hand[i].Rank == Rank.Jack && hand[i].Suit is Suit).ToList();

            foreach (int i in jacks)
            {
                Suit suit = hand[i].Suit!.Value;
                if (BelotHelpers.IsSuit(roundCall) && suit == (Suit)roundCall && Roll(90)) // 90% of the time in a single trump suit, I will lead the Jass here if I have it (I may still end up playing it)
                {
                    return i;
                }

                if (roundCall == Call.AllTrumps && Roll(80)) // 80% of the time in all trumps, I will lead a Jass here if I have one (I may still end up playing it)
                {
                    return i;
                }
            }

            return null;
        }

        private static int? TryPlayTrumpControl(List<Card> hand, int[] validCards, Suit trump)
        {
            // Call and Suit enums are explicitly aligned and roundCall is checked to be a single suit (not AllTrumps or NoTrumps) before getting here, so (Call)trump should be fine
            var trumps = Enumerable.Range(0, hand.Count).Where(i => validCards[i] == 1 && hand[i].Suit == trump).OrderByDescending(i => BelotHelpers.GetCardStrength(hand[i], (Call)trump, trump)).ToList();

            if (trumps.Count == 0)
            {
                return null;
            }

            // Avoid leading the 9 unless it's the only trump
            if (trumps.Count == 1 || hand[trumps[0]].Rank != Rank.Nine)
            {
                return trumps[0];
            }

            return trumps[1];
        }

        private static int? TryPlayNonTrumpAce(List<Card> hand, int[] validCards, Call roundCall)
        {
            var aces = Enumerable.Range(0, hand.Count).Where(i => validCards[i] == 1 && hand[i].Rank == Rank.Ace && hand[i].Suit is Suit suit && (int)suit != (int)roundCall).ToList();

            foreach (int i in aces)
            {
                if (Roll(70)) // 70% of the time, I will lead an Ace (not of trumps) if I have one (I may still end up playing one)
                {
                    return i;
                }
            }

            return null;
        }

        private static int? TryLeadWinner(List<Card> hand, int[] validCards, int[] winners, Call roundCall, int winnerLevel, int chance)
        {
            var candidates = Enumerable.Range(0, hand.Count).Where(i => validCards[i] == 1 && winners[i] == winnerLevel && hand[i].Suit is Suit suit && (int)suit != (int)roundCall).ToList();

            if (candidates.Count > 0)
            {
                if (Roll(chance))
                {
                    return candidates[threadLocalRng.Value!.Next(candidates.Count)];
                }
            }

            return null;
        }

        private static Card SelectRandomValidCard(List<Card> hand, int[] validCards)
        {
            var options = Enumerable.Range(0, hand.Count).Where(i => validCards[i] == 1).ToList();
            Debug.Print("Play random");
            return hand[options[threadLocalRng.Value!.Next(options.Count)]];
        }

        private static bool Roll(int chance)
        {
            return threadLocalRng.Value!.Next(100) < chance;
        }
    }
}

// consider having bots try to play the jack in a single trump suit if they themselves called. Maybe only if the tricksuit is also trumps. This gets them to take lead if anyone happens to lead trumps, which partner does sometimes
// factor in soft winners for TryBeatPartnerWithHardWinner. If partner leads a random card and I have the Ace, I should try win