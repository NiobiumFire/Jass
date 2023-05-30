using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.Identity;
using BelotWebApp.Models;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using Serilog;
using System.Configuration;

namespace BelotWebApp
{
    [HubName("replayroom")] // Attribute -> client-side name for the class may differ from server-side name
    public class ReplayRoom : Hub
    {
        public static List<BelotReplay> replays;

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
        //public static double pauseTime = 0.0;

        public static int punishment = 5000;
        public static int reward = 1;
        public static int maxRounds = 50;

        public ReplayRoom()
        {
            //log.Information("Creating new Chat Room");
        }

        // -------------------- Load Replay --------------------

        public void ResetReplay()
        {
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

        public async Task LoadGame(string guid)
        {
            ResetReplay();
            string path = ConfigurationManager.AppSettings["logfilepath"] + guid + ".txt";
            string[] lines = new string[] { "" };
            try
            {
                lines = System.IO.File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                return;
            }
            SetPlayers(lines[0]);

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
            Clients.Caller.TogglePauseEnabled();
            botDelay = 600;

            await Task.Run(() =>
            {
                //GameController(game);
            });
        }

        public void SetPlayers(string line)
        {
            int s = line.IndexOf("Players : ") + "Players : ".Length;
            line = line.Substring(s, line.Length - 1);
            string[] names = line.Split(new[] { ", " }, StringSplitOptions.None);
            for (int i = 0; i < 4; i++)
            {
                Clients.Caller.SetPlayers(names[i]);
            }
        }

        public void AddDealer(string line)
        {
            int i = line.IndexOf("The dealer is ") + "The dealer is ".Length;
            line = line.Substring(i);
            line = line.Substring(0, line.Length - 1);
            //dealers.Add(players.ToList().FindIndex(n => n.Username == line));
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
            BelotReplay replay = GetReplay();
            int roundsCount = 0;

            // Rounds
            while (replay.CurrentRound < roundsCount && !replay.Paused)
            {
                Clients.Caller.SetScores(replay.Scores[replay.CurrentRound][0], replay.Scores[replay.CurrentRound][1]); // NS, EW
                Clients.Caller.SetCaller(-1); // blank
                Clients.Caller.SetSuit(-1); // blank
                // show five card hands
                // Calls
                if (replay.CurrentlyCalling && !replay.Paused)
                {
                    while (replay.CurrentCall < replay.Calls[replay.CurrentRound].Count() && !replay.Paused)
                    {
                        // show next call
                        Thread.Sleep(replay.Speed);
                        replay.CurrentCall++;
                    }
                    if (replay.CurrentCall == replay.Calls[replay.CurrentRound].Count()) replay.CurrentlyCalling = false;
                }
                // Tricks
                if (!replay.CurrentlyCalling && !replay.Paused)
                {
                    while (replay.CurrentTrick < 8 && !replay.Paused)
                    {
                        while (replay.CurrentCard < 4 && !replay.Paused)
                        {
                            // show next card
                            Clients.Caller.RenderState(replay.State[replay.CurrentRound][replay.CurrentTrick, replay.CurrentCard]);
                            Thread.Sleep(replay.Speed);
                            replay.CurrentCard++;
                        }
                        if (replay.CurrentCard == 4 && !replay.Paused)
                        {
                            replay.CurrentCard = 0;
                            replay.CurrentTrick++;
                        }
                    }
                }

                if (!replay.Paused)
                {
                    replay.CurrentlyCalling = true;
                    replay.CurrentCall = 0;
                    replay.CurrentTrick = 0;
                    replay.CurrentRound++;
                }

            }
        }

        // -------------------- Reset --------------------



        public void EndGame(BelotGame game)
        {
            //log.Debug("Entering EndGame.");
            Clients.Caller.SetDealerMarker(4);
            Clients.Caller.NewRound();
            Clients.Caller.SetTurnIndicator(4);
            // fancy animation and modal to indicate winning team

            bool deleteLog = true;

            if (game.EWTotal > game.NSTotal)
            {
                Clients.Caller.ShowGameWinner(0);
                Clients.Caller.ShowGameWinner(2);
            }
            else
            {
                Clients.Caller.ShowGameWinner(1);
                Clients.Caller.ShowGameWinner(3);
            }
            Clients.Caller.TogglePauseEnabled();
            if (game.EnableLogging) game.CloseLog();
            if (game.EnableLogging && deleteLog) System.IO.File.Delete(System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/" + game.GameId + ".txt"));
            //log.Debug("Leaving EndGame.");
        }

        // -------------------- Setup --------------------

        // -------------------- Suit Nomination --------------------

        public void AnnounceSuit(List<int> suitCall, int turn)
        {
            //log.Debug("Entering AnnounceSuit.");

            int suit = suitCall[suitCall.Count - 1];

            if (suit > 0)
            {
                Clients.Caller.SuitNominated(suit);
                Clients.Caller.setCallerIndicator(turn);
            }

            string[] seatPos = new string[] { "w", "n", "e", "s" };
            Clients.Caller.EmoteSuit(suit, seatPos[turn]);
            Emote(seatPos[turn], botDelay);
            //log.Debug("Leaving AnnounceSuit.");
        }

        // -------------------- Gameplay --------------------

        public void CardPlayEnd(BelotGame game)
        {
            //log.Debug("Entering CardPlayEnd.");
            Clients.Caller.SetTableCard(game.Turn, game.PlayedCards[game.Turn]);
            Thread.Sleep(botDelay);
            if (game.NumCardsPlayed % 4 != 0) if (--game.Turn == -1) game.Turn = 3;
            if (game.NumCardsPlayed < 32) Clients.Caller.SetTurnIndicator(game.Turn);
            //log.Debug("Leaving CardPlayEnd.");
        }

        // -------------------- Visual Replay --------------------

        public void AnnounceExtras(int turn)
        {
            //log.Debug("Entering AnnounceExtras.");
            string[] seatPos = new string[] { "w", "n", "e", "s" };
            Clients.Caller.SetExtrasEmote(new JavaScriptSerializer().Serialize(declarations[round][playTracker]), seatPos[turn]);
            Emote(seatPos[turn], botDelay);
            //log.Debug("Leaving AnnounceExtras.");
        }

        public void AnnounceExtras(BelotGame game, bool belotDeclared)
        {
            //log.Debug("Entering AnnounceExtras.");
            List<string> emotes = new List<string>();
            if (belotDeclared) emotes.Add("Belot");
            if (game.NumCardsPlayed < 5)
            {
                foreach (Run run in game.Runs[game.Turn])
                {
                    if (run.Declared) emotes.Add(BelotHelpers.GetRunNameFromLength(run.Length));
                }
                foreach (Carre carre in game.Carres[game.Turn])
                {
                    if (carre.Declared) emotes.Add("Carre");
                }
            }
            if (emotes.Count > 0)
            {
                string[] seatPos = new string[] { "w", "n", "e", "s" };
                Clients.Caller.SetExtrasEmote(new JavaScriptSerializer().Serialize(emotes), seatPos[game.Turn]);
                Emote(seatPos[game.Turn], botDelay);
            }
            //log.Debug("Leaving AnnounceExtras.");
        }

        public void Emote(string seat, int duration)
        {
            //log.Debug("Entering Emote.");
            Clients.Caller.ShowEmote(seat);
            Thread.Sleep(duration);
            Clients.Caller.HideEmote(seat);
            //log.Debug("Leaving Emote.");
        }

        // -------------------- Points --------------------

        // -------------------- Get Stuff --------------------

        //public string GetDisplayName(int pos)
        //{
        //    return players[pos].Username;
        //}

        public void SetReplaySpeed(int speed) // delay in ms
        {
            GetReplay().Speed = speed;
        }

        public void PauseReplay(bool paused)
        {
            GetReplay().Paused = paused;
        }

        public BelotReplay GetReplay()
        {
            return replays.Where(i => i.ReplayId == Context.User.Identity.Name).First();
        }

        public override Task OnConnected()
        {
            if (GetReplay() == null) replays.Add(new BelotReplay(Context.User.Identity.Name));
            else GetReplay().PlayerIsActive = true;
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled = true)
        {
            GetReplay().PlayerIsActive = false;
            Thread.Sleep(5000);
            if (!GetReplay().PlayerIsActive) replays.Remove(replays.Where(i => i.ReplayId == Context.User.Identity.Name).First());
            return base.OnDisconnected(stopCalled);
        }
    }
}