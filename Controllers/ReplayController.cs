﻿using BelotWebApp.BelotClasses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;
using System.Diagnostics.Metrics;

namespace BelotWebApp.Controllers
{
    [Authorize(Roles = "Player")]
    public class ReplayController : Controller
    {

        private readonly IConfiguration _config;

        public ReplayController(IConfiguration config)
        {
            _config = config;

        }

        public IActionResult Index()
        {
            return View();
        }

        public string GetReplay(string replayId)
        {
            BelotReplay replay = new BelotReplay();
            BelotReplayState currentState = new BelotReplayState();
            replay.States.Add(new BelotReplayState(currentState));

            string path = _config.GetSection("SerilogPath:Path").Value + replayId + ".txt";
            string[] lines;
            try
            {
                lines = System.IO.File.ReadAllLines(path);
            }
            catch (Exception e)
            {
                return "";
            }

            string[] names = BelotReplay.GetPlayers(lines[0]);

            for (int i = 0; i < 4; i++)
            {
                replay.Players[i] = names[i];
            }

            int handsDealt = 0;
            string[] declarations = new string[] { "", "", "", "" };

            for (int i = 1; i < lines.Length; i++)
            {
                if (lines[i].IndexOf("Dealer:") > -1)
                {
                    currentState.Dealer = BelotReplay.GetPosition(lines[i], "Dealer");
                    currentState.Caller = 4;
                    currentState.RoundSuit = 0;
                }
                else if (lines[i].IndexOf("Hand ") > -1)
                {
                    int pos = BelotReplay.GetHandPos(lines[i]);
                    currentState.Hand[pos] = BelotReplay.GetHand(lines[i]);
                    handsDealt++;
                    if (handsDealt == 4)
                    {
                        currentState.Turn = currentState.Dealer;
                        replay.States.Add(new BelotReplayState(currentState));
                        handsDealt = 0;
                    }
                }
                else if (lines[i].IndexOf("Call: ") > -1)
                {
                    int[] calls = BelotReplay.GetIntArray(lines[i], "Call");
                    if (string.Join("", calls.ToArray()) == "0000")
                    {
                        currentState.Emotes = new string[] { "0", "0", "0", "0" };
                        replay.States.Add(new BelotReplayState(currentState));
                    }
                    else
                    {
                        foreach (int call in calls)
                        {
                            if (--currentState.Turn == -1) currentState.Turn = 3;
                            if (call > 0 && call < 9)
                            {
                                currentState.Caller = currentState.Turn;
                                currentState.RoundSuit = call;
                            }
                            currentState.Emotes = new string[] { "", "", "", "" };
                            currentState.Emotes[currentState.Turn] = call.ToString();
                            replay.States.Add(new BelotReplayState(currentState));

                        }
                    }
                    currentState.Turn = currentState.Dealer;
                    currentState.Emotes = new string[] { "", "", "", "" };
                    if (--currentState.Turn == -1) currentState.Turn = 3;
                }
                else if (lines[i].IndexOf("Tierce: ") > -1)
                {
                    declarations[BelotReplay.GetPosition(lines[i], "Tierce")] += "Tierce\n";
                }
                else if (lines[i].IndexOf("Quarte: ") > -1)
                {
                    declarations[BelotReplay.GetPosition(lines[i], "Quarte")] += "Quarte\n";
                }
                else if (lines[i].IndexOf("Quint: ") > -1)
                {
                    declarations[BelotReplay.GetPosition(lines[i], "Quint")] += "Quint\n";
                }
                else if (lines[i].IndexOf("Carre: ") > -1)
                {
                    declarations[BelotReplay.GetPosition(lines[i], "Carre")] += "Carre\n";
                }
                else if (lines[i].IndexOf("Belot: ") > -1)
                {
                    declarations[BelotReplay.GetPosition(lines[i], "Belot")] += "Belot\n";
                }
                else if (lines[i].IndexOf("Play: ") > -1)
                {
                    string[] plays = BelotReplay.GetPlays(lines[i]);
                    for (int j = 0; j < 4; j++)
                    {
                        if (!replay.States[replay.States.Count - 1].ShowTrickWinner)
                        {
                            if (--currentState.Turn == -1) currentState.Turn = 3;
                        }
                        currentState.TableCards[currentState.Turn] = plays[currentState.Turn];
                        int card = Array.IndexOf(currentState.Hand[currentState.Turn], currentState.Hand[currentState.Turn].Where(c => c == plays[currentState.Turn]).First());
                        currentState.Hand[currentState.Turn][card] = "c0-00";
                        currentState.Emotes[currentState.Turn] = declarations[currentState.Turn];
                        replay.States.Add(new BelotReplayState(currentState));
                        currentState.Emotes[currentState.Turn] = "";
                        declarations[currentState.Turn] = "";
                    }
                }
                else if (lines[i].IndexOf("Throw: ") > -1)
                {
                    currentState.Emotes[currentState.Turn] = "Throws\ncards";
                    replay.States.Add(new BelotReplayState(currentState));
                    currentState.Emotes[currentState.Turn] = "";
                    currentState.Hand = new string[][] { new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" },
                new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" }, new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" },
                new string[] { "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00", "c0-00" } };
                }
                else if (lines[i].IndexOf("Trick: ") > -1)
                {
                    currentState.ShowTrickWinner = true;
                    currentState.Turn = BelotReplay.GetPosition(lines[i], "Trick");
                    replay.States.Add(new BelotReplayState(currentState));
                    currentState.TableCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
                    currentState.ShowTrickWinner = false;
                    //replay.States.Add(new BelotReplayState(currentState));
                }
                else if (lines[i].IndexOf("Round: ") > -1)
                {
                    int[] points = BelotReplay.GetIntArray(lines[i], "Round");
                    currentState.Scores[0] += points[0];
                    currentState.Scores[1] += points[1];
                    replay.States.Add(new BelotReplayState(current: currentState));
                }
            }
            return JsonSerializer.Serialize(replay);
        }

        public string GetMyReplays()
        {
            List<string[]> myReplays = new List<string[]>();
            string[] allFiles = new DirectoryInfo(_config.GetSection("SerilogPath:Path").Value).GetFiles()
                .OrderByDescending(f => f.LastWriteTime)
                .Select(f => f.FullName)
                .Where(f => !f.Contains("BelotServerLog"))
                .ToArray();
            foreach (string log in allFiles)
            {
                try
                {
                    string line = System.IO.File.ReadLines(log).First();
                    if (line.Contains(User.Identity.Name))
                    {
                        string[] names = BelotReplay.GetPlayers(line);
                        string creation = System.IO.File.GetCreationTime(log).ToString("yyyy-MM-dd HH:mm");
                        string id = Path.GetFileNameWithoutExtension(log);
                        myReplays.Add(new string[] { names[0], names[1], names[2], names[3], creation, id });
                    }
                }
                catch (Exception e)
                {

                }
            }
            return JsonSerializer.Serialize(myReplays);
        }
    }
}
