using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BelotWebApp
{
    public class AgentBasic
    {
        public static Random rnd = new Random();

        // ignores extra points in suit nomination (for now)
        // doesn't double or redouble
        // always accepts all available extras
        // never throws the cards
        // never calls five-under-nine
        public int CallSuit(List<string> hand, int[] validCalls)
        {
            int call = 0;
            //return call;
            double mostPoints = 0;
            int[] singleTrumpValue = { 1, 1, 2, 2, 4, 1, 1, 2 }; // considering length and strength
            int[] noTrumpValue = { 0, 0, 0, 1, 0, 0, 0, 3 };
            int[] allTrumpValue = { 0, 0, 2, 0, 4, 0, 0, 1 };
            for (int i = 1; i < 7; i++) // calls
            {
                if (validCalls[i - 1] == 1)
                {
                    int length = 0;
                    double points = 0;
                    int hasJ9 = 0;
                    int J9suit = 0;
                    for (int j = 0; j < hand.Count; j++)
                    {
                        int suit = Int32.Parse(hand[j].Substring(1, 1));
                        int rank = Int32.Parse(hand[j].Substring(3, 2));
                        if (suit > 0)
                        {
                            if (suit == i)
                            {
                                length++;
                                points += singleTrumpValue[rank - 6];
                                if (rank == 8 || rank == 10)
                                {
                                    hasJ9++;
                                    if (hasJ9 == 2) points++;
                                }
                            }
                            else if (i < 6) // aces have less value in all trumps
                            {
                                points += noTrumpValue[rank - 6];
                            }
                            else if (i == 6)
                            {
                                points += allTrumpValue[rank - 6];
                                if (suit != J9suit)
                                {
                                    hasJ9 = 0;
                                    J9suit = suit;
                                }
                                if (suit == J9suit)
                                {
                                    if (rank == 8 || rank == 10) hasJ9++;
                                    if (hasJ9 == 2) points += 2;
                                }
                            }
                        }
                    }
                    if (length > 2) points += length;

                    if (i == 5) points *= 1.5;
                    if (points > mostPoints)
                    {
                        mostPoints = points;
                        if ((points > 10 && i < 5) || (points > 14.5 && i == 5) || (points > 15 && i == 6)) call = i;
                    }
                }
            }
            return call;
        }

        public string SelectCard(List<string> hand, int[] validCards, int[] winners, string[] cardPlayed, int turn, int curWinner, int roundSuit, int trickSuit, bool ewCalled)
        {
            int choice = Array.IndexOf(validCards, 1);

            if (validCards.Sum() > 1)
            {
                int cardsPlayedInTrick = 4 - cardPlayed.Where(c => c == "c0-00").Count();

                if (cardsPlayedInTrick == 0) // if I'm to lead
                {
                    if (((ewCalled && turn % 2 == 0) || (!ewCalled && turn % 2 == 1)) && roundSuit != 5) // if we called, try play the Jass
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            int rank = Int32.Parse(hand[i].Substring(3, 2));
                            int suit = Int32.Parse(hand[i].Substring(1, 1));
                            if (rank == 10 && validCards[i] == 1)
                            {
                                lock (rnd)
                                {
                                    if (rnd.Next(100) + 1 > 10 && suit == roundSuit)  // 90% of the time in a single trump suit, I will lead the Jass here if I have it (I may still end up playing it)
                                    {
                                        return hand[i];
                                    }
                                    else if (rnd.Next(100) + 1 > 20 && roundSuit == 6)  // 80% of the time in all trumps, I will lead a Jass here if I have one (I may still end up playing it)
                                    {
                                        return hand[i];
                                    }
                                    else
                                    {

                                    }
                                }
                            }
                        }
                    }
                    //// if my team called and I am the first to play in a trick, for the first 3? tricks, play the highest trump
                    if (roundSuit < 6) // Try play a non-trump Ace (works for no trumps)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            int rank = Int32.Parse(hand[i].Substring(3, 2));
                            int suit = Int32.Parse(hand[i].Substring(1, 1));
                            lock (rnd)
                            {
                                if (rank == 13 && suit != roundSuit && validCards[i] == 1 && rnd.Next(100) + 1 > 30) // 70% of the time, I will lead an Ace if I have one (I may still end up playing one)
                                {
                                    return hand[i];
                                }
                                else if (rank == 13 && suit != roundSuit && validCards[i] == 1)
                                {

                                }
                            }
                        }
                    }
                    bool hardNontrumpWinner = false;
                    bool softNontrumpWinner = false;
                    if (winners.Where(w => w > 0).Count() > 0)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            int suit = Int32.Parse(hand[i].Substring(1, 1));
                            if (suit != roundSuit && validCards[i] == 1)
                            {
                                if (winners[i] == 2) hardNontrumpWinner = true;
                                if (winners[i] == 1) softNontrumpWinner = true;
                                if (hardNontrumpWinner && softNontrumpWinner) break;
                            }
                        }
                    }
                    lock (rnd)
                    {
                        if (hardNontrumpWinner && rnd.Next(100) + 1 > 10) // 90% of the time, I will lead a nontrump hard winner
                        {
                            while (true)
                            {
                                choice = rnd.Next(8);
                                if (winners[choice] == 2 && validCards[choice] == 1 && Int32.Parse(hand[choice].Substring(1, 1)) != roundSuit) return hand[choice];
                            }
                        }
                        if (softNontrumpWinner && rnd.Next(100) + 1 > 30) // 70% of the time, I will lead a nontrump soft winner
                        {
                            while (true)
                            {
                                choice = rnd.Next(8);
                                if (winners[choice] == 1 && validCards[choice] == 1 && Int32.Parse(hand[choice].Substring(1, 1)) != roundSuit) return hand[choice];
                            }
                        }
                    }
                }
                if (cardsPlayedInTrick > 1 && turn % 2 != curWinner % 2) // if I am 3rd or 4th to play and the other team is winning
                {
                    bool discard = false;
                    int bestValue = 0;
                    for (int i = 0; i < 4; i++) // get highest winning power of cards played so far in trick
                    {
                        int value = 0;
                        if (cardPlayed[i] != "c0-00") value = BelotHelpers.DetermineCardPower(cardPlayed[i], roundSuit, trickSuit);
                        if (value > bestValue)
                        {
                            bestValue = value;
                        }
                    }

                    int[] winningCards = { 0, 0, 0, 0, 0, 0, 0, 0 };
                    int[] myCardPower = { 0, 0, 0, 0, 0, 0, 0, 0 };
                    int minValue = 25;

                    for (int i = 0; i < 8; i++) // determine my cards' power
                    {
                        if (validCards[i] == 1)
                        {
                            myCardPower[i] = BelotHelpers.DetermineCardPower(hand[i], roundSuit, trickSuit);
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
                                else if ((myCardPower[i] == minValue - 1 || myCardPower[i] == minValue) && (winners[i] == 0 || (winners[i] > 0 && winners[choice] > 0))) discard_i = true;
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
                                choice = rnd.Next(winningCards.Count());
                                if (winningCards[choice] == 1) return hand[choice];
                            }
                        }
                        // The below does not account for potential winners, so I may discard something of low value which could win a future trick
                        else if (discard) return hand[choice]; // if I can't win the trick, then regardless of whether I am playing 4th or 3rd, play the chosen discard
                    }
                }
                while (true)
                {
                    lock (rnd) choice = rnd.Next(validCards.Count());
                    if (validCards[choice] == 1)
                    {
                        return hand[choice];
                    }
                }

            }
            return hand[choice];
        }
    }
}