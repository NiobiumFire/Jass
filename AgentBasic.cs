using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ChatWebApp
{
    public class AgentBasic
    {
        // basic agent ignores extra points in suit nomination (for now)
        // basic agent doesn't double or redouble
        // basic agent always accepts all available extras
        public int CallSuit(List<string> hand, int[] validCalls)
        {
            int call = 0;
            int mostPoints = 0;
            int[] trumpValue = { 1, 1, 2, 2, 4, 1, 1, 2 }; // considering length and strength
            int[] noTrumpValue = { 0, 0, 0, 1, 0, 0, 0, 3 };
            for (int i = 1; i < 7; i++) // calls
            {
                int length = 0;
                int points = 0;
                for (int j = 0; j < hand.Count; j++)
                {
                    int suit = Int32.Parse(hand[j].Substring(1, 1));
                    int rank = Int32.Parse(hand[j].Substring(3, 2));
                    if (suit > 0)
                    {
                        if (suit == i && i < 5)
                        {
                            length++;
                        }
                        if (suit == i || i == 6)
                        {
                            points += trumpValue[rank - 6];
                        }
                        else
                        {
                            points += noTrumpValue[rank - 6];
                        }
                    }
                }
                if (length > 2) points += length;
                if (points > mostPoints && validCalls[i - 1] == 1)
                {
                    mostPoints = points;
                    if ((points > 10 && i < 5) || (points > 9 && i == 5) || (points > 13 && i == 6)) call = i;
                }
            }
            return call;
        }

        public string PlayCard(List<string> hand, int[] validCards, string[] cardPlayed, int turn, int curWinner, int roundSuit, int trickSuit, bool ewCalled)
        {
            int choice = 8;

            Random rnd = new Random();

            int cardsPlayedInTrick = 4 - cardPlayed.Where(c => c == "c0-00").Count();
            //int cardsPlayedInRound = 8 - hand.Where(c => c == "c0-00").Count();

            if (((ewCalled && turn % 2 == 0) || (!ewCalled && turn % 2 == 1)) && cardsPlayedInTrick == 0 && roundSuit < 5) // if we called and I'm the first to play in the trick, try play the Jass
            {
                for (int i = 0; i < 8; i++)
                {
                    int rank = Int32.Parse(hand[i].Substring(3, 2));
                    int suit = Int32.Parse(hand[i].Substring(1, 1));
                    if (rank == 10 && suit == roundSuit && validCards[i] == 1) choice = i;
                }
            }
            //// if my team called and I am the first to play in a trick, for the first 3? tricks, play the highest trump
            if (cardsPlayedInTrick == 0 && roundSuit < 6 && choice == 8) // try play a non-trump Ace (works for no trumps)
            {
                for (int i = 0; i < 8; i++)
                {
                    int rank = Int32.Parse(hand[i].Substring(3, 2));
                    int suit = Int32.Parse(hand[i].Substring(1, 1));
                    if (rank == 13 && suit != roundSuit && validCards[i] == 1) choice = i;
                }
            }
            if (cardsPlayedInTrick == 3 && turn % 2 != curWinner % 2 && choice == 8) // if I am the last to play and the other team is winning
            {
                CardPower cp = new CardPower();
                int bestValue = 0;
                for (int i = 0; i < 4; i++) // get highest winning power of cards played so far in trick
                {
                    int value = 0;
                    if (cardPlayed[i] != "c0-00") cp.DetermineCardPower(cardPlayed[i], roundSuit, trickSuit);
                    if (value > bestValue)
                    {
                        bestValue = value;
                    }
                }

                int[] winningCards = { 0, 0, 0, 0, 0, 0, 0, 0 };

                for (int i = 0; i < 8; i++) // determine which of my cards wins
                {
                    if (validCards[i] == 1)
                    {
                        int value = cp.DetermineCardPower(hand[i], roundSuit, trickSuit);
                        if (value > bestValue)
                        {
                            winningCards[i] = 1;
                        }
                    }
                }

                if (winningCards.Sum() > 0) // if any of my cards can beat the currently best card played in trick, play a random one of them
                {
                    while (true)
                    {
                        choice = rnd.Next(winningCards.Count());
                        if (winningCards[choice] == 1) break;
                    }
                }
                else // if none of my cards can beat the currently best card played in trick, play the lowest value card
                {
                    int minValue = 17;
                    for (int i = 0; i < 8; i++)
                    {
                        if (validCards[i] == 1)
                        {
                            int value = cp.DetermineCardPower(hand[i], roundSuit, trickSuit);
                            if (value < minValue)
                            {
                                minValue = value;
                                choice = i;
                            }
                        }
                    }
                }
            }
            if (choice == 8)
            {
                while (true)
                {
                    choice = rnd.Next(validCards.Count());
                    if (validCards[choice] == 1) break;
                }
            }
            return hand[choice];
        }
    }
}