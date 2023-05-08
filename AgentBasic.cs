using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatWebApp
{
    public class AgentBasic
    {
        public static Random rnd = new Random();

        // basic agent ignores extra points in suit nomination (for now)
        // basic agent doesn't double or redouble
        // basic agent always accepts all available extras
        public int CallSuit(List<string> hand, int[] validCalls)
        {
            int call = 0;
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

        public string SelectCard(List<string> hand, int[] validCards, string[] cardPlayed, int turn, int curWinner, int roundSuit, int trickSuit, bool ewCalled)
        {
            int choice = Array.IndexOf(validCards, 1);

            if (validCards.Sum() > 1)
            {

                int cardsPlayedInTrick = 4 - cardPlayed.Where(c => c == "c0-00").Count();
                //int cardsPlayedInRound = 8 - hand.Where(c => c == "c0-00").Count();

                if (((ewCalled && turn % 2 == 0) || (!ewCalled && turn % 2 == 1)) && cardsPlayedInTrick == 0 && roundSuit != 5) // if we called and I'm the first to play in the trick, try play the Jass
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
                if (cardsPlayedInTrick == 0 && roundSuit < 6) // if I'm the first to play, try play a non-trump Ace (works for no trumps)
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
                if (cardsPlayedInTrick == 3 && turn % 2 != curWinner % 2) // if I am the last to play and the other team is winning
                {
                    bool discard = false;
                    BelotHelpers cp = new BelotHelpers();
                    int bestValue = 0;
                    for (int i = 0; i < 4; i++) // get highest winning power of cards played so far in trick
                    {
                        int value = 0;
                        if (cardPlayed[i] != "c0-00") value = cp.DetermineCardPower(cardPlayed[i], roundSuit, trickSuit);
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
                            myCardPower[i] = cp.DetermineCardPower(hand[i], roundSuit, trickSuit);
                            if (myCardPower[i] > bestValue) // if I can win the trick, I will
                            {
                                winningCards[i] = 1;
                            }
                            if (myCardPower[i] < minValue) // in case I can't win the trick, track the card with lowest winning power
                            {
                                minValue = myCardPower[i];
                                choice = i;
                                discard = true;
                            }
                        }
                    }

                    if (winningCards.Sum() > 0) // if any of my cards can beat the currently best card played in trick, play a random one of them
                    {
                        while (true)
                        {
                            lock (rnd) choice = rnd.Next(winningCards.Count());
                            if (winningCards[choice] == 1) return hand[choice];
                        }
                    }
                    else if (discard) return hand[choice];
                }
                while (true)
                {
                    lock(rnd) choice = rnd.Next(validCards.Count());
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