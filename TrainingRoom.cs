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
using Serilog;

namespace ChatWebApp
{
    [HubName("trainingroom")] // Attribute -> client-side name for the class may differ from server-side name
    public class TrainingRoom : Hub
    {
        public static BelotGame game;

        public static Player[] players;
        public static List<int> dealers;
        public static List<List<string>> decks;
        public static List<List<int>> calls;
        public static List<int> callList;
        public static List<List<string>> plays;
        public static List<string> playList;
        public static List<List<List<string>>> declarations;
        public static List<List<string>> declarePlays;
        public static List<string> declareList;
        public static List<int[]> points;
        public static int round;
        public static int callTracker;
        public static int playTracker;
        public static int declareTracker;

        public static int scoreTarget = 1501;
        public static string botGUID = "7eae0694-38c9-48c0-9016-40e7d9ab962c";
        public static int botDelay = 700;
        public static bool pause = false;
        public static double pauseTime = 0.0;


        public static BelotHelpers helpers = new BelotHelpers();

        public static AgentBasic basic = new AgentBasic();

        //public static string logPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/BelotServerLog-.txt");
        //public static Serilog.Core.Logger log = new LoggerConfiguration().WriteTo.File(logPath, rollingInterval: RollingInterval.Day).CreateLogger();

        //public static logPath

        public TrainingRoom()
        {
            //log.Information("Creating new Chat Room");
        }

        public void ResetReplay()
        {
            players = new Player[] { new Player(), new Player(), new Player(), new Player() };
            dealers = new List<int>();
            decks = new List<List<string>>();
            calls = new List<List<int>>();
            callList = new List<int>();
            plays = new List<List<string>>();
            playList = new List<string>();
            declarations = new List<List<List<string>>>();
            declarePlays = new List<List<string>>();
            declareList = new List<string>();
            points = new List<int[]>();
            round = 0;
            callTracker = 0;
            playTracker = 0;
            declareTracker = 0;
        }

        public void LoadGame(string guid)
        {
            ResetReplay();
            string path = "~/Logs/" + guid + ".txt";
            string[] lines = System.IO.File.ReadAllLines(System.Web.Hosting.HostingEnvironment.MapPath(path));
            SetPlayers(lines[0]);
            game = new BelotGame(players, "", false);

            Clients.Caller.SeatBooked(0, GetDisplayName(0));
            Clients.Caller.SeatBooked(1, GetDisplayName(1));
            Clients.Caller.SeatBooked(2, GetDisplayName(2));
            Clients.Caller.SeatBooked(3, GetDisplayName(3));

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].IndexOf("The dealer is ") > -1)
                {
                    AddDealer(lines[i]);
                    if (callList.Count > 0)
                    {
                        calls.Add(callList);
                        callList = new List<int>();
                        plays.Add(playList);
                        playList = new List<string>();
                        declarations.Add(declarePlays);
                        declarePlays = new List<List<string>>();
                        points.Add(new int[] { 0, 0 });
                    }
                }
                if (lines[i].IndexOf("E/W win ") > -1)
                {
                    AddPoints(lines[i]);
                    if (callList.Count > 0)
                    {
                        calls.Add(callList);
                        callList = new List<int>();
                        plays.Add(playList);
                        playList = new List<string>();
                        declarations.Add(declarePlays);
                        declarePlays = new List<List<string>>();
                        //points.Add(new int[] { 0, 0 });
                    }
                }
                else if (lines[i].IndexOf("Shuffled deck: ") > -1)
                {
                    AddDeck(lines[i]);
                }
                else if (lines[i].IndexOf("passed.") > -1)
                {
                    callList.Add(0);
                }
                else if (lines[i].IndexOf("called Clubs") > -1)
                {
                    callList.Add(1);
                }
                else if (lines[i].IndexOf("called Diamonds") > -1)
                {
                    callList.Add(2);
                }
                else if (lines[i].IndexOf("called Hearts") > -1)
                {
                    callList.Add(3);
                }
                else if (lines[i].IndexOf("called Spades") > -1)
                {
                    callList.Add(4);
                }
                else if (lines[i].IndexOf("called No Trumps") > -1)
                {
                    callList.Add(5);
                }
                else if (lines[i].IndexOf("called All Trumps") > -1)
                {
                    callList.Add(6);
                }
                else if (lines[i].IndexOf(" doubled") > -1)
                {
                    callList.Add(7);
                }
                else if (lines[i].IndexOf("redoubled") > -1)
                {
                    callList.Add(8);
                }
                else if (lines[i].IndexOf("plays ") > -1)
                {
                    if (playList.Count > 0)
                    {
                        declarePlays.Add(declareList);
                        declareList = new List<string>();
                    }
                    AddPlay(lines[i]);

                }
                else if (lines[i].IndexOf("declares a ") > -1)
                {
                    AddDeclaration(lines[i]);
                }
            }

            GameController();
        }

        public void SetPlayers(string line)
        {
            int i = line.IndexOf("The players are ") + "The players are ".Length;
            line = line.Substring(i);
            line = line.Substring(0, line.Length - 1);
            string[] names = line.Split(new[] { ", " }, StringSplitOptions.None);
            for (int j = 0; j < 4; j++)
            {
                players[j] = new Player(names[j], "", false);
            }
        }

        public void AddDealer(string line)
        {
            int i = line.IndexOf("The dealer is ") + "The dealer is ".Length;
            line = line.Substring(i);
            line = line.Substring(0, line.Length - 1);
            dealers.Add(players.ToList().FindIndex(n => n.Username == line));
        }

        public void AddDeck(string line)
        {
            int i = line.IndexOf("Shuffled deck: ") + "Shuffled deck: ".Length;
            line = line.Substring(i);
            line = line.Substring(0, line.Length - 1);
            string[] cards = line.Split(',');
            List<string> deck = new List<string>();
            foreach (string card in cards)
            {
                deck.Add(card);
            }
            decks.Add(deck);
        }

        public void AddPlay(string line)
        {
            int i = line.IndexOf("plays ") + "plays ".Length;
            line = line.Substring(i);
            line = line.Substring(0, line.Length - 1);
            playList.Add(line);
        }

        public void AddDeclaration(string line)
        {
            int i = line.IndexOf("declares a ") + "declares a ".Length;
            line = line.Substring(i);
            line = line.Substring(0, line.Length - 1);
            declareList.Add(line);
        }

        public void AddPoints(string line)
        {
            int[] p = { 0, 0 };
            int i = line.IndexOf("E/W win ") + "E/W win ".Length;
            int j = line.IndexOf(" points");
            p[0] = Int32.Parse(line.Substring(i, j - i));

            int k = line.IndexOf("N/S win ") + "N/S win ".Length;
            int l = line.IndexOf(" points", j + 1);
            p[1] = Int32.Parse(line.Substring(k, l - k));

            points.Add(p);
        }

        // -------------------- Main --------------------

        public async Task GameController()
        {
            //log.Debug("Entering GameController.");
            //game = new BelotGame(players, Guid.NewGuid().ToString(), true);
            game.NewGame();
            game.FirstPlayer = dealers[0];
            Clients.All.NewGame(); // reset score totals (card table), hide winner markers
            while (((game.EWTotal < scoreTarget && game.NSTotal < scoreTarget) || game.EWTotal == game.NSTotal || game.Capot))
            {
                await Task.Run(() => RoundController());
            }

            EndGame();
            //log.Debug("Leaving GameController.");
        }

        public void RoundController()
        {
            //log.Debug("Entering RoundController.");

            game.NewRound();
            Clients.Caller.SetTurnIndicator(game.Turn); // show dealer
            Clients.Caller.SetDealerMarker(game.Turn);
            Clients.Caller.NewRound(); // reset table, reset board, disable cards, reset suit selection 
            Thread.Sleep(botDelay);

            //game.Shuffle();
            game.Deck = decks[round];
            game.Deal(5);
            for (int i = 0; i < 4; i++)
            {
                Clients.Caller.Deal(new JavaScriptSerializer().Serialize(game.Hand[i]), i);
            }

            while (!game.SuitDecided())
            {
                Clients.All.SetTurnIndicator(game.Turn);
                CallController();
            }

            if (game.RoundSuit != 0)
            {

                game.Turn = game.FirstPlayer;
                Clients.All.SetTurnIndicator(game.Turn);
                game.Deal(3);
                for (int i = 0; i < 4; i++)
                {
                    Clients.Caller.Deal(new JavaScriptSerializer().Serialize(game.Hand[i]), i);
                }
                //if (game.RoundSuit != 5)
                //{
                //    game.FindRuns();
                //    game.FindCarres();
                //    game.TruncateRuns();
                //    game.FindBelots();
                //}

                while (game.NumCardsPlayed < 32)
                {
                    Clients.All.SetTurnIndicator(game.Turn);
                    TrickController();
                }

                Clients.All.NewRound();
                game.EWTotal += points[round][0];
                game.NSTotal += points[round][1];
                Clients.All.UpdateScoreTotals(game.EWTotal, game.NSTotal);
                //Clients.All.ShowRoundSummary(game.TrickPoints, game.DeclarationPoints, game.BelotPoints, game.Result, game.EWRoundPoints, game.NSRoundPoints);
                //Thread.Sleep(6000);
                //Clients.All.HideRoundSummary();

            }
            round++;
            callTracker = 0;
            playTracker = 0;
            //log.Debug("Leaving RoundController.");
        }

        public void CallController()
        {

            while (pause)
            {

            }

            //log.Debug("Entering CallController.");
            //int[] validCalls = game.ValidCalls();
            //if (validCalls.Sum() == 0) game.NominateSuit(0); // auto-pass

            //Thread.Sleep(botDelay);

            //else // bot
            //{
            //game.NominateSuit(basic.CallSuit(game.Hand[game.Turn], validCalls));
            //Thread.Sleep(botDelay);
            //}

            game.NominateSuit(calls[round][callTracker]);

            AnnounceSuit();
            if (--game.Turn == -1) game.Turn = 3;
            callTracker++;
            //log.Debug("Leaving CallController.");
        }

        public void TrickController()
        {
            //log.Debug("Entering TrickController.");
            while (game.PlayedCards.Where(c => c != "c0-00").Count() < 4)
            {
                while (pause)
                {

                }

                //if (game.Hand[game.Turn].Where(c => c != "c0-00").Count() == 1) // auto-play last card
                //{
                //    game.PlayCard(game.Hand[game.Turn].Where(c => c != "c0-00").First()); // no extra declaration is possible on last card -> skip straight to PlayCardRequest
                //    CardPlayEnd();
                //    continue;
                //}
                //bool belot = false;
                //int[] validCards = game.ValidCards();

                //string card = new AgentBasic().SelectCard(game.Hand[game.Turn], validCards, game.PlayedCards, game.Turn, game.DetermineWinner(), game.RoundSuit, game.TrickSuit, game.EWCalled);
                string card = plays[round][playTracker];

                game.PlayCard(card);

                //List<string> extras = new List<string>();

                //if (game.RoundSuit != 5)
                //{
                //    if (game.CheckBelot(card))
                //    {
                //        extras.Add("Belot");
                //        game.DeclareBelot();
                //        belot = true;
                //    }

                //    if (game.NumCardsPlayed < 5)
                //    {
                //        game.DeclareRuns();
                //        game.DeclareCarres();
                //    }
                //    AnnounceExtras(belot);
                //}

                if (playTracker < 29)
                    if (declarations[round][playTracker].Count > 0)
                        AnnounceExtras();

                CardPlayEnd();

                playTracker++;
            }

            int winner = game.DetermineWinner();
            //int pointsBefore = game.EWRoundPoints + game.NSRoundPoints;
            //if (winner == 0 || winner == 2)
            //{
            //    game.EWRoundPoints += game.CalculateTrickPoints();
            //    game.EWWonATrick = true;
            //}
            //else
            //{
            //    game.NSRoundPoints += game.CalculateTrickPoints();
            //    game.NSWonATrick = true;
            //}

            Clients.All.ShowWinner(winner);
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(400);
                Clients.All.ShowWinner(winner);
            }

            if (game.NumCardsPlayed < 32)
            {
                Clients.All.ResetTable();
                game.Turn = winner;
                game.PlayedCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
            }
            game.HighestTrumpInTrick = 0;
            game.TrickSuit = 0;
            //log.Debug("Leaving TrickController.");
        }

        // -------------------- Reset --------------------



        public void EndGame()
        {
            //log.Debug("Entering EndGame.");
            Clients.All.SetDealerMarker(4);
            Clients.All.NewRound();
            Clients.All.SetTurnIndicator(4);
            // fancy animation and modal to indicate winning team
            if (game.EWTotal > game.NSTotal)
            {
                Clients.All.ShowWinner(0);
                Clients.All.ShowWinner(2);
            }
            else
            {
                Clients.All.ShowWinner(1);
                Clients.All.ShowWinner(3);
            }
            //log.Debug("Leaving EndGame.");
        }

        // -------------------- Setup --------------------

        // -------------------- Suit Nomination --------------------

        public void AnnounceSuit()
        {
            //log.Debug("Entering AnnounceSuit.");

            int suit = game.SuitCall[game.SuitCall.Count - 1];

            if (suit > 0)
            {
                Clients.All.SuitNominated(suit);
                Clients.All.setCallerIndicator(game.Turn);
            }

            string[] seatPos = new string[] { "w", "n", "e", "s" };
            Clients.All.EmoteSuit(suit, seatPos[game.Turn]);
            Emote(seatPos[game.Turn], botDelay);
            //log.Debug("Leaving AnnounceSuit.");
        }

        // -------------------- Card Validation --------------------

        // -------------------- Gameplay --------------------

        public void CardPlayEnd()
        {
            //log.Debug("Entering CardPlayEnd.");
            Clients.Caller.SetTableCard(game.Turn, game.PlayedCards[game.Turn]);
            Thread.Sleep(botDelay);
            if (game.NumCardsPlayed % 4 != 0) if (--game.Turn == -1) game.Turn = 3;
            if (game.NumCardsPlayed < 32) Clients.All.SetTurnIndicator(game.Turn);
            //log.Debug("Leaving CardPlayEnd.");
        }

        public void AnnounceExtras()
        {
            //log.Debug("Entering AnnounceExtras.");
            string[] seatPos = new string[] { "w", "n", "e", "s" };
            Clients.All.SetExtrasEmote(new JavaScriptSerializer().Serialize(declarations[round][playTracker]), seatPos[game.Turn]);
            Emote(seatPos[game.Turn], botDelay);
            //log.Debug("Leaving AnnounceExtras.");
        }

        public void Emote(string seat, int duration)
        {
            //log.Debug("Entering Emote.");
            Clients.All.ShowEmote(seat);
            Thread.Sleep(duration);
            Clients.All.HideEmote(seat);
            //log.Debug("Leaving Emote.");
        }

        // -------------------- Points --------------------

        // -------------------- Seat Management --------------------


        // -------------------- Messaging & Alerts --------------------

        // -------------------- Get Stuff --------------------

        public string GetDisplayName(int pos)
        {
            return players[pos].Username;
        }

        public void SetGameSpeed(int delay) // delay in ms
        {
            botDelay = delay;
        }

        public void PauseGame(bool setPause)
        {
            pause = setPause;
        }
    }
}