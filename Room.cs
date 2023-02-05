using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.Identity;
using ChatWebApp.Models;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

namespace ChatWebApp
{
    [HubName("room")] // Attribute -> client-side name for the class may differ from server-side name
    public class ChatRoom : Hub
    {
        public static List<string> connectedUsers = new List<string>();
        public static ConcurrentDictionary<string, string> connectionIDs = new ConcurrentDictionary<string, string>();
        public static ConcurrentDictionary<int, string> seatPositions = new ConcurrentDictionary<int, string>();
        public static List<string> deck;
        public static List<string>[] hand;
        public static int cardsDealt;
        public static int firstPlayer, turn;
        public static int numCardsPlayed;
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

        public ChatRoom()
        {

        }

        // -------------------- Main --------------------

        public void GameController()
        {
            while (((ewTotal < scoreTarget && nsTotal < scoreTarget) || ewTotal == nsTotal || capot) && !waitDeal && !waitCall & !waitCard)
            {
                RoundController();
            }

            if (!waitDeal && !waitCall & !waitCard) EndGame();
        }

        public void RoundController()
        {
            if (numCardsPlayed == 0 && hand[0].Count == 0)
            {
                if (GetDictionaryUsernameFromSeat(turn) != botGUID)
                {
                    Clients.Client(GetConnectionIDFromUsername(GetDictionaryUsernameFromSeat(turn))).EnableDealBtn();
                    waitDeal = true;
                    return;
                }
                else
                {
                    Thread.Sleep(botDelay);
                    Shuffle();
                    turn = firstPlayer;
                    Deal(5);
                }
            }

            if (numCardsPlayed == 0)
            {
                while (!SuitDecided() && !waitCall)
                {
                    Clients.All.SetTurnIndicator(turn);
                    CallController();
                }
            }

            if (roundSuit != 0 && !waitCall)
            {
                if (numCardsPlayed == 0)
                {
                    turn = firstPlayer;
                    Clients.All.SetTurnIndicator(turn);
                    Deal(3);
                    FindRuns();
                    FindCarres();
                    FindBelots();
                }
                while (numCardsPlayed < 32 & !waitCard)
                {
                    TrickController();
                }
                if (numCardsPlayed == 32)
                {
                    Clients.All.NewRound();
                    FinalisePoints();
                    NewRound();
                }
            }
        }

        public void CallController()
        {
            int[] validCalls = ValidCalls();
            if (validCalls.Sum() == 0)
            {
                NominateSuit(0); // auto-pass
                if (--turn == -1) turn = 3;
            }
            else if (GetDictionaryUsernameFromSeat(turn) != botGUID)
            {
                Clients.Client(GetConnectionIDFromUsername(GetDictionaryUsernameFromSeat(turn))).ShowSuitModal(validCalls);
                waitCall = true;
            }
            else // bot
            {
                NominateSuit(new AgentBasic().CallSuit(hand[turn], validCalls));
                if (--turn == -1) turn = 3;
            }
        }

        public void TrickController()
        {
            while (playedCards.Where(c => c != "c0-00").Count() < 4 && !waitCard)
            {
                if (hand[turn].Where(c => c != "c0-00").Count() == 1) // auto-play last card
                {
                    if (GetDictionaryUsernameFromSeat(turn) != botGUID) Clients.Client(GetConnectionIDFromUsername(GetDictionaryUsernameFromSeat(turn))).PlayFinalCard();
                    PlayCardRequest(hand[turn].Where(c => c != "c0-00").First()); // no extra declaration is possible on last card -> skip straight to PlayCardRequest
                    continue;
                }
                int[] validCards = ValidCards();
                if (GetDictionaryUsernameFromSeat(turn) != botGUID)
                {
                    Clients.Client(GetConnectionIDFromUsername(GetDictionaryUsernameFromSeat(turn))).enableCards(validCards);
                    waitCard = true;
                }
                else
                {
                    DeclareExtras(new AgentBasic().PlayCard(hand[turn], validCards, playedCards, turn, DetermineWinner(), roundSuit, trickSuit, ewCalled));
                }
            }
        }

        // -------------------- Reset --------------------

        public void NewGame()
        {
            if (seatPositions.Count == 4)
            {
                Clients.All.CloseModalsAndButtons();
                Clients.All.DisableRadios();
                SysAnnounce("Resetting for a new game.");
                Random rnd = new Random();
                firstPlayer = rnd.Next(4);
                ewTotal = 0;
                nsTotal = 0;
                Clients.All.NewGame(); // reset score table (offcanvas), reset score totals (card table), hide winner markers
                NewRound();
            }
        }

        public void NewRound() // set new first player
        {
            turn = firstPlayer;
            Clients.All.SetTurnIndicator(turn); // show dealer
            Clients.All.SetDealerMarker(turn);
            Clients.All.DisableDealBtn();

            if (--firstPlayer == -1) firstPlayer = 3;
            deck = new List<string>();
            hand = new List<string>[4];
            runs = new List<Run>[4];
            carres = new List<Carre>[4];
            belots = new List<Belot>[4];
            for (int i = 0; i < 4; i++)
            {
                hand[i] = new List<string>();
                runs[i] = new List<Run>();
                carres[i] = new List<Carre>();
                belots[i] = new List<Belot>();
            }
            playedCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
            cardsDealt = 0;
            numCardsPlayed = 0;
            trickSuit = 0;
            highestTrumpInTrick = 0;
            roundSuit = 0; // 0 = pass, 1 = clubs ... 5 = no trumps, 6 = all trumps
            suitCall = new List<int>();
            Clients.All.NewRound(); // reset table, reset board, disable cards, reset suit selection 
            ewRoundPoints = 0;
            nsRoundPoints = 0;
            multiplier = 1;
            ewWonATrick = false;
            nsWonATrick = false;
            capot = false;
            waitDeal = false;
            waitCall = false;
            waitCard = false;
        }

        public void EndGame()
        {
            Clients.All.SetDealerMarker(4);
            Clients.All.NewRound();
            Clients.All.SetTurnIndicator(4);
            // fancy animation and modal to indicate winning team
            if (ewTotal > nsTotal)
            {
                Clients.All.ShowWinner(0);
                Clients.All.ShowWinner(2);
            }
            else
            {
                Clients.All.ShowWinner(1);
                Clients.All.ShowWinner(3);
            }
            Clients.All.EnableNewGame();
            Clients.All.EnableRadios();
        }

        // -------------------- Setup --------------------

        public void Shuffle()
        {
            bool isHuman = false;
            if (GetDictionaryUsernameFromSeat(turn) != botGUID) isHuman = true;

            //var card = new List<string> { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12", "13" }; // Full deck
            var card = new List<string> { "06", "07", "08", "09", "10", "11", "12", "13" }; // Belot deck
            var suit = new List<int> { 1, 2, 3, 4 };

            Random rnd = new Random();

            while (deck.Count < card.Count * suit.Count)
            {
                int i = rnd.Next(card.Count); // 0 <= i <= 13
                int j = rnd.Next(suit.Count); // 0 <= i <= 4
                if (!deck.Contains("c" + suit[j] + "-" + card[i]))
                {
                    deck.Add("c" + suit[j] + "-" + card[i]);
                }
            }
            //deck = new List<string> {"c1-06", "c1-07", "c1-08", "c1-09", "c2-10", "c2-07", "c3-07", "c4-07",
            //    "c1-08", "c2-08", "c3-08", "c4-08", "c1-09", "c2-09", "c3-09", "c4-09",
            //    "c1-10", "c2-10", "c3-10", "c4-10", "c1-11", "c1-12", "c1-13", "c4-11",
            //    "c1-12", "c2-12", "c3-12", "c4-12", "c1-06", "c2-06", "c3-06", "c4-06", };

            if (isHuman)
            {
                turn = firstPlayer;
                Deal(5);
                waitDeal = false;
                GameController();
            }
        }

        public List<string> OrderCards(List<string> hand, bool forruns)
        {
            var nontrumporder = new List<string> { "06", "07", "08", "10", "11", "12", "09", "13" };
            var trumporder = new List<string> { "06", "07", "11", "12", "09", "13", "08", "10" };
            var runorder = new List<string> { "06", "07", "08", "09", "10", "11", "12", "13" };
            var nontrump = new List<int>();
            var trump = new List<int>();
            var masterlist = new List<string>();

            for (int i = 1; i < 5; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    if (!forruns)
                    {
                        if (roundSuit == i || roundSuit == 6)
                        {
                            masterlist.Add("c" + i + "-" + trumporder[j]);
                        }
                        else
                        {
                            masterlist.Add("c" + i + "-" + nontrumporder[j]);
                        }
                    }
                    else
                    {
                        masterlist.Add("c" + i + "-" + runorder[j]);
                    }
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
                    hand[turn].Add(deck[cardsDealt++]);
                }
                hand[turn] = OrderCards(hand[turn], false);
                if (GetDictionaryUsernameFromSeat(turn) != botGUID)
                {
                    Clients.Client(GetConnectionIDFromUsername(GetDictionaryUsernameFromSeat(turn))).Deal(new JavaScriptSerializer().Serialize(hand[turn]));
                }
                if (--turn == -1) turn = 3;
            }
        }

        // -------------------- Suit Nomination --------------------

        public int[] ValidCalls()
        {
            int[] validCalls = { 1, 1, 1, 1, 1, 1, 0, 0 }; // c, d, h, s, A, J, x2, x4

            if (roundSuit > 0)
            {
                for (int i = 0; i < roundSuit; i++)
                {
                    validCalls[i] = 0;
                }

                if ((turn % 2 == 0 && !ewCalled) || (turn % 2 == 1 && ewCalled))
                {
                    if (multiplier == 1)
                    {
                        validCalls[6] = 1;
                    }
                    else if (multiplier == 2)
                    {
                        validCalls[7] = 1;
                    }
                }
            }
            return validCalls;
        }

        public void NominateSuit(int suit)
        {

            bool isHuman = false;
            if (GetDictionaryUsernameFromSeat(turn) != botGUID) isHuman = true;

            suitCall.Add(suit);

            string username = GetGameplayUsernameFromTurn();

            string message = username + " passed.";
            string[] seatPos = new string[] { "w", "n", "e", "s" };
            Clients.All.EmoteSuit(suit, seatPos[turn]);
            Emote(seatPos[turn], 1200);
            if (suit > 0)
            {
                ewCalled = turn == 0 || turn == 2;
                Clients.All.SuitNominated(suit);
                Clients.All.setCallerIndicator(turn);
                if (suit < 7)
                {
                    roundSuit = suit;
                    multiplier = 1;
                    message = username + " called " + GetSuitNameFromNumber(suit) + ".";
                }
                else if (suit == 7)
                {
                    multiplier = 2;
                    message = username + " doubled!";
                }
                else
                {
                    multiplier = 4;
                    message = username + " redoubled!!";
                }
            }

            SysAnnounce(message);

            if (isHuman)
            {
                if (--turn == -1) turn = 3;
                waitCall = false;
                GameController();
            }
        }

        public bool SuitDecided()
        {
            if (suitCall.Count > 3)
            {
                if (string.Join("", suitCall.GetRange(suitCall.Count - 3, 3).ToArray()) == "000")
                {
                    if (suitCall[suitCall.Count - 4] == 0)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            hand[i] = new List<string>();
                        }
                        SysAnnounce("No suit chosen.");
                        NewRound();
                    }
                    else
                    {
                        SysAnnounce("The round will be played in " + GetSuitNameFromNumber(roundSuit) + ".");
                    }
                    return true;
                }
            }
            return false;
        }

        // -------------------- Card Validation --------------------

        public int[] ValidCards()
        {
            int[] validCards = { 1, 1, 1, 1, 1, 1, 1, 1 };
            validCards = RemovePlayedCards(validCards);
            if (trickSuit > 0) // if it's not the first card of the trick
            {
                if (roundSuit == trickSuit && PlayerHasCardsOfSuit(roundSuit))
                {
                    validCards = RemoveCardsNotOfSuit(validCards, roundSuit);
                    if (PlayerHasHigherTrump()) validCards = RemoveLowerTrumps(validCards);
                }

                else if (PlayerHasCardsOfSuit(trickSuit))
                {
                    validCards = RemoveCardsNotOfSuit(validCards, trickSuit);
                    if (roundSuit == 6 && PlayerHasHigherTrump()) validCards = RemoveLowerTrumps(validCards);
                }
                // condition 3 is why it's necessary to first check if trick suit is trumps
                else if (roundSuit < 5 && PlayerHasCardsOfSuit(roundSuit)) // if trumps (C,D,H,S) NOT lead, and player doesn't have any of the trick suit
                {
                    int currentwinner = DetermineWinner();
                    if ((turn % 2 == 0 && currentwinner % 2 != 0) || ((turn - 1) % 2 == 0 && (currentwinner - 1) % 2 != 0)) // if partner is not currently winning the trick
                    {
                        if (PlayerHasHigherTrump()) // if player has a higher trump than what has been played in this trick, must overtrump
                        {
                            validCards = RemoveCardsNotOfSuit(validCards, roundSuit);
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
                string card = hand[turn][i];
                if (Int32.Parse(card.Substring(1, 1)) == 0)
                {
                    validcards[i] = 0;
                }
            }
            return validcards;
        }
        public bool PlayerHasCardsOfSuit(int suit)
        {
            foreach (string card in hand[turn])
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
                    string card = hand[turn][i];
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
            foreach (string card in hand[turn])
            {
                if (TrumpStrength(card) > highestTrumpInTrick)
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
                    string card = hand[turn][i];
                    if (TrumpStrength(card) < highestTrumpInTrick)
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
            if (Int32.Parse(card.Substring(1, 1)) == roundSuit || (roundSuit == 6 && Int32.Parse(card.Substring(1, 1)) == trickSuit))
            {
                int[] strength = { 1, 2, 7, 5, 8, 3, 4, 6 };
                trumpstrength = strength[Int32.Parse(card.Substring(3, 2)) - 6];
            }
            return trumpstrength;
        }

        // -------------------- Gameplay --------------------

        public void DeclareExtras(string tableCard)
        {
            string username = GetDictionaryUsernameFromSeat(turn);

            if (roundSuit != 5)
            {
                List<string> extras = new List<string>();

                int rank = Int32.Parse(tableCard.Substring(3, 2));
                if (rank == 11 || rank == 12)
                {
                    int suit = Int32.Parse(tableCard.Substring(1, 1));
                    if (belots[turn].Where(s => s.Suit == suit).ToList().Where(d => d.Declarable == true).Count() > 0)
                    {
                        belots[turn].Where(s => s.Suit == suit).First().Declarable = false;
                        if ((suit == trickSuit || trickSuit == 0))
                        {
                            extras.Add("Belot: " + GetSuitNameFromNumber(suit));
                        }
                    }
                }

                if (numCardsPlayed < 4)
                {

                    if (runs[turn].Count > 0)
                    {
                        foreach (Run extra in runs[turn])
                        {
                            extras.Add(GetRunNameFromLength(extra.Length) + ": " + GetSuitNameFromNumber(extra.Suit) + " " + GetCardRankFromNumber(extra.Strength - extra.Length + 1) + "→" + GetCardRankFromNumber(extra.Strength));
                        }
                    }
                    if (carres[turn].Count > 0)
                    {
                        foreach (Carre extra in carres[turn])
                        {
                            extras.Add("Carre: " + GetCardRankFromNumber(extra.Rank));
                        }
                    }
                }

                if (extras.Count > 0)
                {
                    if (username == botGUID)
                    {
                        ExtrasDeclared(tableCard, extras.ToArray());
                    }
                    else
                    {
                        Clients.Caller.Extras(new JavaScriptSerializer().Serialize(extras), tableCard, FindRunCarreOverlap(extras));
                    }
                    // ExtrasDeclared > PlayCardRequest will be called by the modal instead of by this function
                }
                else
                {
                    PlayCardRequest(tableCard);
                }
            }
            else
            {
                PlayCardRequest(tableCard);
            }

        }

        public void ExtrasDeclared(string tableCard, string[] declared)
        {
            List<string> declaredExtraList = new List<string>();
            foreach (string declaration in declared)
            {
                declaredExtraList.Add(declaration.Split(new[] { ": " }, StringSplitOptions.None)[0]);
                if (declaration.Substring(0, 5) == "Belot")
                {
                    string suit = declaration.Split(new[] { ": " }, StringSplitOptions.None)[1];
                    belots[turn].Where(s => s.Suit == GetSuitNumberFromName(suit)).First().Declared = true;
                    SysAnnounce(GetGameplayUsernameFromTurn() + " called a Belot in " + suit + ".");
                }
                else if (declaration.Substring(0, 5) == "Carre")
                {
                    string rank = declaration.Split(new[] { ": " }, StringSplitOptions.None)[1];
                    carres[turn].Where(r => r.Rank == GetRankFromChar(rank)).First().Declared = true;
                    SysAnnounce(GetGameplayUsernameFromTurn() + " called a Carre.");
                }
                else
                {
                    string[] run = declaration.Split(new[] { ": ", " ", "→" }, StringSplitOptions.None);
                    int str = GetRankFromChar(run[3]);
                    runs[turn].Where(s => s.Suit == GetSuitNumberFromName(run[1])).Where(s => s.Strength == str).First().Declared = true;
                    SysAnnounce(GetGameplayUsernameFromTurn() + " called a " + run[0] + ".");
                }
            }
            if (declaredExtraList.Count > 0)
            {
                string[] seatPos = new string[] { "w", "n", "e", "s" };
                Clients.All.EmoteExtras(new JavaScriptSerializer().Serialize(declaredExtraList), seatPos[turn]);
                Emote(seatPos[turn], 1500);
            }
            PlayCardRequest(tableCard);
        }

        public void PlayCardRequest(string tableCard)
        {

            bool isHuman = false;
            if (GetDictionaryUsernameFromSeat(turn) != botGUID) isHuman = true;

            playedCards[turn] = tableCard;
            Clients.All.SetTableCard(turn, tableCard);
            Thread.Sleep(botDelay);

            for (int i = 0; i < hand[turn].Count; i++)
            {
                if (hand[turn][i] == playedCards[turn])
                {
                    hand[turn][i] = "c0-00";
                    break;
                }
            }

            numCardsPlayed++;

            if (trickSuit == 0) trickSuit = Int32.Parse(tableCard.Substring(1, 1)); // first card of a trick determines suit

            int trumpstrength = TrumpStrength(playedCards[turn]);
            if (highestTrumpInTrick < trumpstrength) highestTrumpInTrick = trumpstrength;

            if (numCardsPlayed % 4 == 0) // trick end
            {
                int winner = DetermineWinner();
                if (winner == 0 || winner == 2)
                {
                    ewRoundPoints += CalculateRoundPoints();
                    ewWonATrick = true;
                }
                else
                {
                    nsRoundPoints += CalculateRoundPoints();
                    nsWonATrick = true;
                }

                Clients.All.ShowWinner(winner);
                for (int i = 0; i < 5; i++)
                {
                    Thread.Sleep(400);
                    Clients.All.ShowWinner(winner);
                }

                if (numCardsPlayed < 32)
                {
                    Clients.All.ResetTable();
                    turn = winner;
                    playedCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
                }
                highestTrumpInTrick = 0;
                trickSuit = 0;
            }
            else
            {
                if (--turn == -1) turn = 3;
            }

            if (numCardsPlayed < 32) Clients.All.SetTurnIndicator(turn);
            if (isHuman && numCardsPlayed < 29) // on the last trick (cards 29, 30, 31, 32), cards are auto-played and the end of this method will return to TrickController
            {
                waitCard = false;
                GameController();
            }
        }

        public void Emote(string seat, int duration)
        {
            Clients.All.ShowEmote(seat);
            Thread.Sleep(duration);
            Clients.All.HideEmote(seat);
        }

        // -------------------- Points --------------------

        public void FindRuns()
        {
            for (int i = 0; i < 4; i++)
            {
                List<string> handforruns = OrderCards(hand[i], true);
                for (int j = 0; j < 6; j++)
                {
                    int maxrun = 1;
                    int suit = Int32.Parse(handforruns[j].Substring(1, 1));
                    int strength = Int32.Parse(handforruns[j].Substring(3, 2));
                    for (int k = 0; k < maxrun; k++)
                    {
                        if (j + k + 1 > 7) break;

                        if (Int32.Parse(handforruns[j + k + 1].Substring(1, 1)) == suit) // if two adjacent cards are of the same suit
                        {

                            if (Int32.Parse(handforruns[j + k + 1].Substring(3, 2)) == strength + k + 1) // if second card is adjacent in rank to the first card
                            {
                                maxrun++; // consider the next card
                            }
                        }
                    }
                    if (maxrun > 2 && maxrun < 6)
                    {
                        runs[i].Add(new Run(maxrun, suit, strength + maxrun - 1, false));
                    }
                    else if (maxrun > 5)
                    {
                        runs[i].Add(new Run(5, suit, strength + maxrun - 1, false));
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
                    int rank = Int32.Parse(hand[i][j].Substring(3, 2));
                    if (rank > 7) ranks[rank - 8]++;
                }
                for (int j = 0; j < 6; j++)
                {
                    if (ranks[j] == 4) carres[i].Add(new Carre(j + 8, false));
                }
            }
        }

        public bool[] FindRunCarreOverlap(List<string> extras)
        {
            bool[] overlaps = new bool[extras.Count];
            int runCount = 0;
            // Only occurs if player has Carre(s), as runs cannot overlap
            if (carres[turn].Count == 1) // if there are 2 carres, there are no runs and there is no overlap
            {
                for (int i = 0; i < extras.Count; i++)
                {
                    if (extras[i].Split(new[] { ": " }, StringSplitOptions.None)[0] != "Carre" && extras[i].Split(new[] { ": " }, StringSplitOptions.None)[0] != "Belot")
                    {
                        int upper = runs[turn][runCount].Strength;
                        int lower = upper - runs[turn][runCount].Length + 1;
                        if (carres[turn][0].Rank >= lower && carres[turn][0].Rank <= upper)
                        {
                            overlaps[i] = true;
                        }
                        else
                        {
                            overlaps[i] = false;
                        }
                        runCount++;
                    }
                    else
                    {
                        overlaps[i] = false;
                    }
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
                    int rank = Int32.Parse(hand[i][j].Substring(3, 2));
                    if (rank == 11 || rank == 12)
                    {
                        int suit = Int32.Parse(hand[i][j].Substring(1, 1));
                        newBelots[suit - 1]++;
                    }
                }
                for (int j = 0; j < 4; j++)
                {
                    if (newBelots[j] == 2 && (j + 1 == roundSuit || roundSuit == 6)) belots[i].Add(new Belot(j + 1, false, true));
                }
            }
        }

        public int DetermineWinner()
        {
            int winner = 0;
            int bestValue = 0;

            CardPower cp = new CardPower();

            if (trickSuit > 0)
            {
                for (int i = 0; i < 4; i++)
                {
                    int value = cp.DetermineCardPower(playedCards[i], roundSuit, trickSuit);
                    if (value > bestValue)
                    {
                        bestValue = value;
                        winner = i;
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

                int suit = Int32.Parse(playedCards[i].Substring(1, 1));
                int card = Int32.Parse(playedCards[i].Substring(3, 2)) - 6;
                if (roundSuit == 6 || roundSuit == suit)
                {
                    points += trump[card];
                }
                else
                {
                    points += nontrump[card];
                }
            }
            if (numCardsPlayed == 32) points += 10;

            return points;
        }

        public void FinalisePoints()
        {
            int[] TrickPoints = new int[] { ewRoundPoints, nsRoundPoints };
            int[] DeclarationPoints = new int[] { 0, 0 };
            int[] BelotPoints = new int[] { 0, 0 };
            string[] Result;
            if (ewCalled)
            {
                Result = new string[] { "Success", "" };
            }
            else
            {
                Result = new string[] { "", "Success" };
            }

            if (roundSuit == 5) // no trumps points are always doubled
            {
                ewRoundPoints *= 2;
                nsRoundPoints *= 2;
            }
            else
            {
                // Tierce
                List<Run> EWRuns = new List<Run>();
                EWRuns.AddRange(runs[0].Where(d => d.Declared == true));
                EWRuns.AddRange(runs[2].Where(d => d.Declared == true));
                List<Run> NSRuns = new List<Run>();
                NSRuns.AddRange(runs[1].Where(d => d.Declared == true));
                NSRuns.AddRange(runs[3].Where(d => d.Declared == true));
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
                        //SysAnnounce("The extras were tied. No extra points awarded.");
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
                EWCarres.AddRange(carres[0].Where(d => d.Declared == true));
                EWCarres.AddRange(carres[2].Where(d => d.Declared == true));
                List<Carre> NSCarres = new List<Carre>();
                NSCarres.AddRange(carres[1].Where(d => d.Declared == true));
                NSCarres.AddRange(carres[3].Where(d => d.Declared == true));
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

                ewRoundPoints += DeclarationPoints[0];
                nsRoundPoints += DeclarationPoints[1];

                // Belot
                BelotPoints[0] += 20 * (belots[0].Where(d => d.Declared == true).Count() + belots[2].Where(d => d.Declared == true).Count());
                BelotPoints[1] += 20 * (belots[1].Where(d => d.Declared == true).Count() + belots[3].Where(d => d.Declared == true).Count());

                ewRoundPoints += BelotPoints[0];
                nsRoundPoints += BelotPoints[1];
            }

            if (!ewWonATrick) // capot
            {
                nsRoundPoints += 90;
                Result[1] = "Capot";
                capot = true;
            }
            else if (!nsWonATrick)
            {
                ewRoundPoints += 90;
                Result[0] = "Capot";
                capot = true;
            }

            if (ewCalled && ewRoundPoints <= nsRoundPoints) // inside
            {
                nsRoundPoints += ewRoundPoints;
                ewRoundPoints = 0;
                Result[0] = "Inside";
            }
            else if (!ewCalled && nsRoundPoints <= ewRoundPoints)
            {
                ewRoundPoints += nsRoundPoints;
                nsRoundPoints = 0;
                Result[1] = "Inside";
            }

            if (multiplier > 1) // double and redouble
            {
                ewRoundPoints *= multiplier;
                nsRoundPoints *= multiplier;
                if (ewCalled && ewRoundPoints > nsRoundPoints)
                {
                    ewRoundPoints += nsRoundPoints;
                    nsRoundPoints = 0;
                }
                else if (!ewCalled && nsRoundPoints > ewRoundPoints)
                {
                    nsRoundPoints += ewRoundPoints;
                    ewRoundPoints = 0;
                }
            }
            ewTotal += ewRoundPoints;
            nsTotal += nsRoundPoints;
            Clients.All.AppendScoreTable(ewRoundPoints, nsRoundPoints);
            Clients.All.UpdateScoreTotals(ewTotal, nsTotal);
            Clients.All.ShowRoundSummary(TrickPoints, DeclarationPoints, BelotPoints, Result, ewRoundPoints, nsRoundPoints);
            Thread.Sleep(6000);
            Clients.All.HideRoundSummary();
        }

        // -------------------- Seat Management --------------------

        public void BookSeat(int position) // 0 = W, 1 = N, 2 = E, 3 = S, 4-7 = Robot
        {
            string[] seat = { "West", "North", "East", "South" };

            if (position == 8) // vacate to Spectator
            {
                UnbookSeat();
                Clients.Caller.SetRadio("x");
                return;
            }

            string requestor = GetCallerUsername();
            string occupier;
            if (position > 3)
            {
                occupier = GetDictionaryUsernameFromSeat(position - 4);
            }
            else
            {
                occupier = GetDictionaryUsernameFromSeat(position);
            }

            if ((occupier == "" || occupier == botGUID) && position < 4) // empty seat or bot-occupied requested by human
            {
                UnbookSeat();
                seatPositions.TryRemove(position, out _);
                seatPositions.TryAdd(position, requestor);
                Clients.All.SeatBooked(position, requestor);
                Clients.All.SetBotBadge(seat[position], false);
                Clients.Caller.SetRadio(seat[position]);
                SysAnnounce(requestor + " occupied the " + seat[position] + " seat.");
            }
            else if (occupier == "" && position > 3) // empty seat requested by bot
            {
                position -= 4;
                seatPositions.TryAdd(position, botGUID);
                Clients.All.SeatBooked(position, GetBotName(position));
                Clients.All.SetBotBadge(seat[position], true);
                SysAnnounce(GetBotName(position) + " occupied the " + seat[position] + " seat.");
            }
            // if bot occupied seat requested by bot -> do nothing
            else if (occupier == requestor && position > 3) // human assigns bot to his own occupied seat
            {
                position -= 4;
                UnbookSeat();
                seatPositions.TryAdd(position, botGUID);
                Clients.All.SeatBooked(position, GetBotName(position));
                Clients.Caller.SetRadio("x");
                Clients.All.SetBotBadge(seat[position], true);
                SysAnnounce(GetBotName(position) + " occupied the " + seat[position] + " seat.");
            }
            // if human tries to occupy his own seat, do nothing
            else if (occupier != "" && occupier != botGUID && occupier != requestor) // human-occupied seat is requested by another human or by a bot on behalf of another human
            {
                Clients.Caller.SeatAlreadyBooked(occupier);
            }

            if (seatPositions.Count() == 4) Clients.All.EnableNewGame();
        }

        public void UnbookSeat()
        {
            string username = GetCallerUsername();
            int position = GetSeatFromUsername(username);

            if (position < 4)
            {
                if (_ = seatPositions.TryRemove(position, out _))
                {
                    Clients.All.SeatUnbooked(position, username);
                    string[] seat = { "West", "North", "East", "South" };
                    SysAnnounce(username + " vacated the " + seat[position] + " seat.");

                }
                Clients.All.DisableNewGame();
            }
        }

        // -------------------- Messaging & Alerts --------------------

        public string MsgHead()
        {
            return GetServerDateTime() + ", " + GetCallerUsername();
        }

        [HubMethodName("announce")] //client-side name for the method may differ from server-side name
        public void Announce(string message)
        {
            Clients.All.Announce(MsgHead() + " >> " + message);
            Clients.Others.showChatNotification();
        }

        public void SysAnnounce(string message)
        {
            Clients.All.Announce(GetServerDateTime() + " >> " + message);
        }

        // -------------------- Get Stuff --------------------

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

        public string GetServerDateTime()
        {
            //return DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            return DateTime.Now.ToString("HH:mm");
        }

        public string GetCallerUsername()
        {
            if (Context.User.Identity.IsAuthenticated)
            {
                return Context.User.Identity.Name;
            }
            else
            {
                return "Unknown"; // This should never happen
            }
        }

        public string GetBotName(int pos)
        {
            string[] seat = { "West", "North", "East", "South" };
            return "Robot " + seat[pos];
        }

        public int GetSeatFromUsername(string username)
        {
            for (int i = 0; i < 4; i++)
            {
                seatPositions.TryGetValue(i, out string occupier);
                if (username == occupier)
                {
                    return i;
                }
            }

            return 4; // if the user has not booked a seat, return Spectator
        }
        public string GetDictionaryUsernameFromSeat(int seat)
        {
            if (seatPositions.TryGetValue(seat, out string username))
            {
                return username;
            }
            //Console.WriteLine("Failed to get username from seat number");
            return "";
        }
        public string GetGameplayUsernameFromTurn()
        {
            string username = GetDictionaryUsernameFromSeat(turn);
            if (username == botGUID)
            {
                username = GetBotName(turn);
            }
            return username;
        }

        public string GetConnectionIDFromUsername(string username)
        {
            if (connectionIDs.TryGetValue(username, out string connectionID))
            {
                return connectionID;
            }
            //Console.WriteLine("Failed to get connection ID from username");
            return "";
        }

        // -------------------- Connection --------------------

        public void LoadContext(string connectionID)
        {
            for (int i = 0; i < 4; i++)
            {
                seatPositions.TryGetValue(i, out string occupier);
                if (occupier == botGUID)
                {
                    string[] seat = { "West", "North", "East", "South" };
                    Clients.Caller.SeatBooked(i, GetBotName(i));
                    Clients.Caller.SetBotBadge(seat[i], true);
                }
                else if (occupier != null)
                {
                    Clients.Caller.SeatBooked(i, occupier);
                }
            }

            if (seatPositions.Count() == 4) Clients.Caller.EnableNewGame();
        }

        public override Task OnConnected()
        {
            string username = GetCallerUsername();
            connectedUsers.Add(username);
            connectionIDs.TryAdd(username, Context.ConnectionId);
            connectedUsers.Sort();
            SysAnnounce(username + " connected.");
            Clients.All.connectedUsers(new JavaScriptSerializer().Serialize(connectedUsers));
            LoadContext(Context.ConnectionId);
            return base.OnConnected();
        }
        public override Task OnDisconnected(bool stopCalled = true)
        {
            string username = GetCallerUsername();
            connectedUsers.Remove(username);
            _ = connectionIDs.TryRemove(username, out _);
            UnbookSeat();
            SysAnnounce(username + " disconnected.");
            Clients.All.connectedUsers((new JavaScriptSerializer()).Serialize(connectedUsers));

            return base.OnDisconnected(stopCalled);
        }
    }
}