using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Agents
{
    public class AgentBasic
    {
        private static readonly Random rnd = new Random();

        // ignores extra points in suit nomination (for now)
        // doesn't double or redouble
        // always accepts all available extras
        // never throws the cards
        // never calls five-under-nine
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


        public static Card SelectCard(List<Card> hand, int[] validCards, int[] winners, Card[] tableCards, int turn, int curWinner, Call roundCall, Suit? trickSuit, bool ewCalled)
        {
            int choice = Array.IndexOf(validCards, 1);

            if (validCards.Sum() > 1)
            {
                int cardsPlayedInTrick = tableCards.Count(c => !c.IsNull());

                if (cardsPlayedInTrick == 0) // if I'm to lead
                {
                    int result = SelectCardForFirst(hand, validCards, winners, turn, roundCall, ewCalled);
                    if (result < 8)
                    {
                        return hand[result];
                    }
                }
                else if (cardsPlayedInTrick == 1) // if I'm second to play
                {
                    int result = SelectCardForSecond();
                    if (result < 8)
                    {
                        return hand[result];
                    }
                }
                if (cardsPlayedInTrick > 1 && turn % 2 != curWinner % 2) // if I am 3rd or 4th to play and the other team is winning
                {
                    bool discard = false;
                    int bestValue = 0;
                    for (int i = 0; i < 4; i++) // get highest winning power of cards played so far in trick
                    {
                        int value = 0;
                        if (!tableCards[i].IsNull())
                        {
                            value = BelotHelpers.GetCardStrength(tableCards[i], roundCall, trickSuit);
                        }
                        if (value > bestValue)
                        {
                            bestValue = value;
                        }
                    }

                    int[] winningCards = [0, 0, 0, 0, 0, 0, 0, 0];
                    int[] myCardPower = [0, 0, 0, 0, 0, 0, 0, 0];
                    int minValue = 25;

                    for (int i = 0; i < 8; i++) // determine my cards' power
                    {
                        if (validCards[i] == 1)
                        {
                            myCardPower[i] = BelotHelpers.GetCardStrength(hand[i], roundCall, trickSuit);
                            if (myCardPower[i] > bestValue) // determine if I can win the trick
                            {
                                winningCards[i] = 1;
                            }
                            // in case I can't or don't decide to win the trick, track which card I will discard:
                            // if card[i] value < minValue - 1, discard card[i] regardless of which are winners
                            // if card[i] == minValue - 1 or minValue, and card[i] is not a winner or both are winners, discard card[i]
                            // if card[i] == minValue, and card[i] is a winner and card[choice] is not, discard card[choice]
                            // if card[i] value = minValue - 1 but it's a winner and card[choice] is not, 50% chance to keep card[i]
                            // if card[i] value = minValue + 1 but it's not a winner and card[choice] is, 50% chance to discard card[i]
                            // if card[i] value >= minValue + 2, discard card[choice] regardless of which are winners
                            lock (rnd)
                            {
                                bool discard_i = false;
                                if (myCardPower[i] < minValue - 1) discard_i = true;
                                else if ((myCardPower[i] == minValue - 1 || myCardPower[i] == minValue) && (winners[i] == 0 || winners[i] > 0 && winners[choice] > 0)) discard_i = true;
                                else if (myCardPower[i] == minValue - 1 && winners[i] > 0 && winners[choice] == 0 && rnd.Next(100) + 1 > 50) discard_i = true;
                                //else if (myCardPower[i] == minValue && winners[i] > 0 && winners[choice] == 0) discard_i = false;
                                else if (myCardPower[i] == minValue + 1 && winners[i] == 0 && winners[choice] > 0 && rnd.Next(100) + 1 > 50) discard_i = true;
                                if (discard_i)
                                {
                                    minValue = myCardPower[i];
                                    choice = i;
                                    discard = true;
                                }
                            }
                        }
                    }

                    lock (rnd)
                    {
                        if (winningCards.Sum() > 0 && (cardsPlayedInTrick == 3 || rnd.Next(100) + 1 > 50)) // if any of my cards can beat the currently best card played in trick and I am last to play, play a random one of them. Do the same 50% of the time if I am 3rd to play
                        {
                            while (true)
                            {
                                choice = rnd.Next(winningCards.Length);
                                if (winningCards[choice] == 1) return hand[choice];
                            }
                        }
                        // The below does not account for potential winners, so I may discard something of low value which could win a future trick
                        else if (discard) return hand[choice]; // if I can't win the trick, then regardless of whether I am playing 4th or 3rd, play the chosen discard
                    }
                }
                while (true)
                {
                    lock (rnd)
                    {
                        choice = rnd.Next(validCards.Count());
                    }
                    if (validCards[choice] == 1)
                    {
                        return hand[choice];
                    }
                }
            }

            return hand[choice];
        }

        private static int SelectCardForFirst(List<Card> hand, int[] validCards, int[] winners, int turn, Call roundCall, bool ewCalled)
        {
            if ((ewCalled && turn % 2 == 0 || !ewCalled && turn % 2 == 1) && roundCall != Call.NoTrumps) // if we called, try play the Jass
            {
                for (int i = 0; i < 8; i++) // cards
                {
                    var card = hand[i];
                    if (card.Played || card.Suit is not Suit suit || card.Rank is not Rank rank)
                    {
                        continue;
                    }

                    if (rank == Rank.Jack && validCards[i] == 1)
                    {
                        lock (rnd)
                        {
                            if (BelotHelpers.IsSuit(roundCall) && suit == (Suit)roundCall && rnd.Next(100) + 1 > 10)  // 90% of the time in a single trump suit, I will lead the Jass here if I have it (I may still end up playing it)
                            {
                                return i;
                            }
                            else if (roundCall == Call.AllTrumps && rnd.Next(100) + 1 > 20)  // 80% of the time in all trumps, I will lead a Jass here if I have one (I may still end up playing it)
                            {
                                return i;
                            }
                        }
                    }
                }
            }
            //// if my team called and I am the first to play in a trick, for the first 3? tricks, play the highest trump
            if (roundCall < Call.AllTrumps) // Try play a non-trump Ace (works for no trumps)
            {
                for (int i = 0; i < 8; i++)
                {
                    var card = hand[i];
                    if (card.Played || card.Suit is not Suit suit || card.Rank is not Rank rank)
                    {
                        continue;
                    }

                    lock (rnd)
                    {
                        if (rank == Rank.Ace && (int)suit != (int)roundCall && validCards[i] == 1 && rnd.Next(100) + 1 > 30) // 70% of the time, I will lead an Ace if I have one (I may still end up playing one)
                        {
                            return i;
                        }
                        //else if (rank == Rank.Ace && (int)suit != (int)roundCall && validCards[i] == 1)
                        //{

                        //}
                    }
                }
            }
            bool hardNontrumpWinner = false;
            bool softNontrumpWinner = false;
            if (winners.Any(w => w > 0))
            {
                for (int i = 0; i < 8; i++)
                {
                    var card = hand[i];
                    if (card.Played || card.Suit is not Suit suit || card.Rank is not Rank rank)
                    {
                        continue;
                    }

                    if ((int)suit != (int)roundCall && validCards[i] == 1)
                    {
                        if (winners[i] == 2)
                        {
                            hardNontrumpWinner = true;
                        }
                        if (winners[i] == 1)
                        {
                            softNontrumpWinner = true;
                        }
                        if (hardNontrumpWinner && softNontrumpWinner)
                        {
                            break;
                        }
                    }
                }
            }
            lock (rnd)
            {
                if (hardNontrumpWinner && rnd.Next(100) + 1 > 10) // 90% of the time, I will lead a nontrump hard winner
                {
                    while (true)
                    {
                        int choice = rnd.Next(8);
                        if (winners[choice] == 2 && validCards[choice] == 1 && hand[choice].Suit is Suit suit && (int)suit != (int)roundCall)
                        {
                            return choice;
                        }
                    }
                }
                if (softNontrumpWinner && rnd.Next(100) + 1 > 30) // 70% of the time, I will lead a nontrump soft winner
                {
                    while (true)
                    {
                        int choice = rnd.Next(8);
                        if (winners[choice] == 1 && validCards[choice] == 1 && hand[choice].Suit is Suit suit && (int)suit != (int)roundCall)
                        {
                            return choice;
                        }
                    }
                }
            }
            return 8;
        }

        private static int SelectCardForSecond()
        {
            if (true)
            {

            }
            return 8;
        }

        private static int SelectCardForThird()
        {
            return 0;
        }

        private static int SelectCardForFourth()
        {
            return 0;
        }
    }
}