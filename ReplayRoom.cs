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
using System.IO;

namespace BelotWebApp
{
    [HubName("replayroom")] // Attribute -> client-side name for the class may differ from server-side name
    public class ReplayRoom : Hub
    {
        public static List<BelotReplay> replays = new List<BelotReplay>();

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

        public ReplayRoom()
        {

        }

        // -------------------- Find Player's Replays --------------------

        public void FindReplays()
        {
            List<string[]> myReplays = new List<string[]>();
            string[] allFiles = Directory.GetFiles(ConfigurationManager.AppSettings["logfilepath"], "*.txt");

            foreach (string log in allFiles)
            {
                try
                {
                    if (!log.Contains("BelotServerLog"))
                    {
                        string line = File.ReadLines(log).First();
                        if (line.Contains(Context.User.Identity.Name))
                        {
                            int l = log.IndexOf(ConfigurationManager.AppSettings["logfilepath"]) + ConfigurationManager.AppSettings["logfilepath"].Length;
                            string[] names = GetPlayers(line);
                            string creation = File.GetCreationTime(log).ToString("yyMMdd-HH");
                            string id = log.Substring(l, 36);
                            myReplays.Add(new string[] { creation, names[0], names[1], names[2], names[3], id });
                        }
                    }
                }
                catch (Exception e)
                {

                }
            }
            Clients.Caller.AppendReplayList(new JavaScriptSerializer().Serialize(myReplays));
        }

        // -------------------- Load Replay --------------------

        public async Task LoadGame(string guid)
        {
            BelotReplayState currentState = new BelotReplayState(new int[] { 0, 0 }, 4, 0, 4, 4, new string[] { "", "", "", "" },
                new string[] { "c0-00", "c0-00", "c0-00", "c0-00" }, new string[][] { new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" },
                new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" }, new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" },
                new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" } }, false);
            BelotReplay replay = GetReplay();
            replay.States = new List<BelotReplayState>();
            replay.States.Add(new BelotReplayState(currentState));

            string path = ConfigurationManager.AppSettings["logfilepath"] + guid + ".txt";
            string[] lines = new string[] { "" };
            try
            {
                lines = File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                return;
            }

            string[] names = GetPlayers(lines[0]);

            for (int i = 0; i < 4; i++)
            {
                replay.Players[i] = new Player(names[i], "", false);
            }

            Clients.Caller.SetPlayers(new string[] { names[0], names[1], names[2], names[3] });

            int handsDealt = 0;

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].IndexOf("Dealer:") > -1)
                {
                    currentState.Dealer = GetDealer(lines[i]);
                    //currentState.Turn = dealer;
                    //replay.States.Add(new BelotReplayState(currentState));
                }
                else if (lines[i].IndexOf("Hand ") > -1)
                {
                    int pos = GetHandPos(lines[i]);
                    currentState.Hand[pos] = GetHand(lines[i]);
                    handsDealt++;
                    if (handsDealt == 4)
                    {
                        currentState.Turn = currentState.Dealer;
                        replay.States.Add(new BelotReplayState(currentState));
                        //if (--currentState.Turn == -1) currentState.Turn = 3;
                        handsDealt = 0;
                    }
                }
                else if (lines[i].IndexOf("Call: ") > -1)
                {
                    int[] calls = GetCalls(lines[i]);
                    //if (calls == new int[] { 0, 0, 0, 0 })
                    if (string.Join("", calls.ToArray()) == "0000")
                    {
                        currentState.Emotes = new string[] { "Pass", "Pass", "Pass", "Pass" };
                        //currentState.Turn = currentState.Dealer;
                        replay.States.Add(new BelotReplayState(currentState));
                    }
                    else
                    {
                        foreach (int call in calls)
                        {
                            if (--currentState.Turn == -1) currentState.Turn = 3;
                            if (call > 0)
                            {
                                currentState.Caller = currentState.Turn;
                                currentState.RoundSuit = call;
                            }
                            currentState.Emotes = BuildEmotes(currentState.Turn, call);
                            replay.States.Add(new BelotReplayState(currentState));

                        }
                    }
                    currentState.Turn = currentState.Dealer;
                    currentState.Emotes = new string[] { "", "", "", "" };
                    //replay.States.Add(new BelotReplayState(currentState));
                    if (--currentState.Turn == -1) currentState.Turn = 3;
                }
                else if (lines[i].IndexOf("Play: ") > -1)
                {
                    string[] plays = GetPlays(lines[i]);
                    for (int j = 0; j < 4; j++)
                    {
                        if (!replay.States[replay.States.Count - 1].ShowTrickWinner) if (--currentState.Turn == -1) currentState.Turn = 3;
                        currentState.TableCards[currentState.Turn] = plays[currentState.Turn];
                        int card = Array.IndexOf(currentState.Hand[currentState.Turn], currentState.Hand[currentState.Turn].Where(c => c == plays[currentState.Turn]).First());
                        currentState.Hand[currentState.Turn][card] = "c0-00";
                        replay.States.Add(new BelotReplayState(currentState));
                    }
                    //replay.States.Add(new BelotReplayState(currentState));
                }
                else if (lines[i].IndexOf("Trick: ") > -1)
                {
                    currentState.ShowTrickWinner = true;
                    currentState.Turn = GetWinner(lines[i]);
                    replay.States.Add(new BelotReplayState(currentState));
                    currentState.TableCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
                    currentState.ShowTrickWinner = false;
                    //replay.States.Add(new BelotReplayState(currentState));
                }
                else if (lines[i].IndexOf("Round: ") > -1)
                {
                    int[] points = GetPoints(lines[i]);
                    currentState.Scores[0] += points[0];
                    currentState.Scores[1] += points[1];
                    replay.States.Add(new BelotReplayState(current: currentState));
                }
            }
            replay.CurrentState = 0;
            replay.Paused = true;
            replay.Speed = 1000;
            Clients.Caller.SetState(new JavaScriptSerializer().Serialize(replay.States[replay.CurrentState]), replay.Speed);
            Clients.Caller.EnablePauseBtn(true);
            Clients.Caller.EnableFwdBtn(true);
            Clients.Caller.EnableBackBtn(false);

            //await Task.Run(() =>
            //{
            //    GameController(game);
            //});
        }

        public string[] GetPlayers(string line)
        {
            int s = line.IndexOf("Players: ") + "Players: ".Length;
            line = line.Substring(s);
            string[] names = line.Split(new[] { "," }, StringSplitOptions.None);
            return names;
        }

        public int GetDealer(string line)
        {
            int l = line.IndexOf("Dealer: ") + "Dealer: ".Length;
            line = line.Substring(l, 1);
            return int.Parse(line);
        }

        public int GetHandPos(string line)
        {
            int l = line.IndexOf("Hand ") + "Hand ".Length;
            line = line.Substring(l, 1);
            return int.Parse(line);
        }

        public string[] GetHand(string line)
        {
            int l = line.IndexOf("Hand ") + "Hand ".Length + 3;
            line = line.Substring(l);
            string[] hand = { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" };
            string[] cards = line.Split(',');
            for (int i = 0; i < cards.Count(); i++)
            {
                hand[i] = cards[i];
            }
            return hand;
        }

        public int[] GetCalls(string line)
        {
            int l = line.IndexOf("Call: ") + "Call: ".Length;
            line = line.Substring(l);
            int[] calls = Array.ConvertAll(line.Split(','), c => int.Parse(c));
            return calls;
        }

        public string[] BuildEmotes(int pos, int call)
        {
            string[] emotes = { "", "", "", "" };
            string[] calls = { "Pass", "Clubs", "Diamonds", "Hearts", "Spades", "No Trumps", "All Trumps", "Double!", "Redouble!", "⁹⁄₅" };
            emotes[pos] = calls[call];
            return emotes;
        }

        public string[] GetPlays(string line)
        {
            int l = line.IndexOf("Play: ") + "Play: ".Length;
            line = line.Substring(l);
            string[] plays = line.Split(',');
            return plays;
        }

        public int GetWinner(string line)
        {
            int l = line.IndexOf("Trick: ") + "Trick: ".Length;
            line = line.Substring(l, 1);
            return int.Parse(line);
        }

        public int[] GetPoints(string line)
        {
            int l = line.IndexOf("Round: ") + "Round: ".Length;
            line = line.Substring(l);
            int[] points = Array.ConvertAll(line.Split(','), p => int.Parse(p));
            return points;
        }


        // -------------------- Main --------------------

        public async Task GameController()
        {
            BelotReplay replay = GetReplay();

            while (replay.CurrentState < replay.States.Count && !replay.Paused)
            {
                await Task.Run(() =>
                {
                    Clients.Caller.SetState(new JavaScriptSerializer().Serialize(replay.States[replay.CurrentState]), replay.Speed);
                    replay.CurrentState++;
                    if (replay.CurrentState < replay.States.Count) Thread.Sleep(replay.Speed);
                });

            }
            if (replay.CurrentState == replay.States.Count)
            {
                replay.CurrentState--;
                replay.Paused = true;
                Clients.Caller.SetPausedTrue();
                Clients.Caller.EnableBackBtn(true);
                Clients.Caller.EnableFwdBtn(false);
                Clients.Caller.EnablePauseBtn(false);
                //Clients.Caller.PauseReplay(true);
            }
        }

        public void SetReplaySpeed(int speed) // delay in ms
        {
            GetReplay().Speed = speed;
        }

        public void PauseReplay(bool paused)
        {
            BelotReplay replay = GetReplay();
            replay.Paused = paused;
            if (paused && replay.CurrentState > 0)
            {
                replay.CurrentState--;
                if (replay.CurrentState > 0) Clients.Caller.EnableBackBtn(true);
            }
            if (paused && replay.CurrentState < replay.States.Count() - 1)
            {
                Clients.Caller.EnableFwdBtn(true);
            }
            if (paused && replay.CurrentState == replay.States.Count())
            {
                replay.CurrentState--;
                Clients.Caller.EnableBackBtn(true);
            }
            if (!paused)
            {
                Clients.Caller.EnableBackBtn(false);
                Clients.Caller.EnableFwdBtn(false);
                replay.CurrentState++;
                GameController();
            }
        }

        public void ReplayFwd()
        {
            BelotReplay replay = GetReplay();
            if (replay.CurrentState < replay.States.Count() - 1)
            {
                replay.CurrentState++;
                if (replay.States[replay.CurrentState].ShowTrickWinner == true) replay.CurrentState++;
                if (replay.CurrentState == replay.States.Count() - 1)
                {
                    Clients.Caller.EnableFwdBtn(false);
                    Clients.Caller.EnableBackBtn(true);
                    Clients.Caller.EnablePauseBtn(false);
                }
                Clients.Caller.EnableBackBtn(true);
                Clients.Caller.SetState(new JavaScriptSerializer().Serialize(replay.States[replay.CurrentState]), replay.Speed);
            }
        }

        public void ReplayBack()
        {
            BelotReplay replay = GetReplay();
            if (replay.CurrentState > 0)
            {
                replay.CurrentState--;
                if (replay.States[replay.CurrentState].ShowTrickWinner == true) replay.CurrentState--;
                if (replay.CurrentState == 0) Clients.Caller.EnableBackBtn(false);
                Clients.Caller.EnableFwdBtn(true);
                Clients.Caller.EnablePauseBtn(true);
                Clients.Caller.SetState(new JavaScriptSerializer().Serialize(replay.States[replay.CurrentState]), replay.Speed);
            }
        }

        public BelotReplay GetReplay()
        {
            return replays.Where(i => i.ReplayId == Context.User.Identity.Name).First();
        }

        public override Task OnConnected()
        {
            if (replays.Where(i => i.ReplayId == Context.User.Identity.Name).Count() == 0) replays.Add(new BelotReplay(Context.User.Identity.Name));
            else GetReplay().PlayerIsActive = true;
            FindReplays();
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