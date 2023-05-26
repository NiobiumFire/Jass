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
        //public static BelotGame game;

        public static Player[] players;

        public static bool isReplay = false;
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

        public static Random rnd = new Random();

        public static AgentBasic basic = new AgentBasic();

        public static int punishment = 5000;
        public static int reward = 1;
        public static int maxRounds = 50;
        //public static AgentAdvanced advanced = new AgentAdvanced(103, 103, 17);

        //public static string logPath = System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/BelotServerLog2-.txt");
        //public static Serilog.Core.Logger log = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(logPath, rollingInterval: RollingInterval.Day).CreateLogger();

        //public static logPath

        public TrainingRoom()
        {
            //log.Information("Creating new Chat Room");
        }

        // -------------------- Load Replay --------------------

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

        public async Task LoadGame(string guid)
        {
            isReplay = true;
            ResetReplay();
            string path = "~/Logs/" + guid + ".txt";
            string[] lines = new string[] { "" };
            try
            {
                lines = System.IO.File.ReadAllLines(System.Web.Hosting.HostingEnvironment.MapPath(path));
            }
            catch (Exception e)
            {
                return;
            }
            SetPlayers(lines[0]);
            BelotGame game = new BelotGame(players, "", false);

            for (int i = 0; i < 4; i++)
            {
                Clients.Caller.SeatBooked(i, GetDisplayName(i));
            }

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
                GameController(game);
            });
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

        // -------------------- Generate Game --------------------

        public async Task GenerateGame(int numGames = 1)
        {
            botDelay = 0;

            List<BelotGame> games = new List<BelotGame>();
            AgentAdvanced[] agents = new AgentAdvanced[numGames * 4];
            int generation = 0;
            AgentAdvanced bestAgent;
            double bestFitness = 0;
            int bestErrors = 0;
            double averageErrors = 0.0;
            string bestGuid = "";
            int mostGames = 0;

            int k = 0;
            while (k < 2)
            {
                for (int i = 0; i < numGames; i++)
                {
                    games.Add(new BelotGame(new Player[] {
                new Player(botGUID, "", false), new Player(botGUID, "", false), new Player(botGUID, "", false), new Player(botGUID, "", false) },
                        Guid.NewGuid().ToString(),
                        true));
                    for (int j = 0; j < 4; j++)
                    {
                        //if (agents[i * 4 + j] == null) agents[i * 4 + j] = new AgentAdvanced(103, 103, 17);
                        //if (agents[i * 4 + j] == null) agents[i * 4 + j] = new AgentAdvanced(45, 64, 17);
                        if (agents[i * 4 + j] == null) agents[i * 4 + j] = new AgentAdvanced(9, 32, 9);
                        games[i].Players[j].Agent = agents[i * 4 + j];
                    }
                }

                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                try
                {
                    await Task.Run(() =>
                    {
                        Parallel.ForEach(games, game =>
                        {
                            GameController(game);
                        });
                    });
                }
                catch (Exception e)
                {

                }

                watch.Stop();

                agents = agents.OrderByDescending(f => f.Fitness).ToArray();

                bestFitness = agents[0].Fitness;
                bestErrors = agents[0].Errors;
                averageErrors = Math.Round((Double)agents.Sum(e => e.Errors) / agents.Count(), 2);
                mostGames = agents.OrderByDescending(g => g.GamesPlayed).First().GamesPlayed;

                for (int i = 0; i < games.Count; i++)
                {
                    if (games[i].Players[0].Agent == agents[0] || games[i].Players[1].Agent == agents[0] ||
                        games[i].Players[2].Agent == agents[0] || games[i].Players[3].Agent == agents[0])
                    {
                        bestGuid = games[i].GameId;
                        break;
                    }
                }

                //bestGuid = 

                Clients.Caller.AppendTrainingTable(new double[] { ++generation, watch.ElapsedMilliseconds / 1000.0, numGames * 4, bestFitness, bestErrors, mostGames, averageErrors }, bestGuid);

                int tenPercent = agents.Length / 10;

                // keep top 10% agents

                // breed these for another 10%

                // 0 - 10% = copy
                // 10% - 20% = copy with mutation
                // 20% - 30% = copy with crossover
                // 30% - 40% = copy with crossover and mutation

                for (int i = 0; i < tenPercent; i++)
                {
                    agents[i].Errors = 0;
                    agents[i].Fitness = 0;
                    agents[i].CallVariation = new List<int>();
                }

                for (int i = tenPercent; i < tenPercent * 2; i++)
                {
                    agents[i] = new AgentAdvanced(agents[i - tenPercent]);
                    agents[i].Mutate(5);
                }

                for (int i = tenPercent * 2; i < tenPercent * 3; i++)
                {
                    agents[i] = new AgentAdvanced(agents[i - tenPercent * 2]);
                    int m;
                    do
                    {
                        lock (rnd) m = rnd.Next(tenPercent);
                    }
                    while (m != i - tenPercent * 2);
                    agents[i].CrossOver(agents[m], 50);
                }

                for (int i = tenPercent * 3; i < tenPercent * 4; i++)
                {
                    agents[i] = new AgentAdvanced(agents[i - tenPercent]);
                    agents[i].Mutate(5);
                }

                // randomly mutate retained parents = 10%
                // randomly mutate duplicated children = 40%
                // initialise new generation from above 40% + 60% new random agents

                for (int i = tenPercent * 4; i < agents.Length; i++)
                {
                    agents[i] = null;
                }

                // repeat

                // save top 10% of agents brains
                // load brain

                games = new List<BelotGame>();
                k++;
            }
        }

        // -------------------- Main --------------------

        public void GameController(BelotGame game)
        {
            //log.Debug("Entering GameController.");
            game.NewGame();
            if (isReplay) game.FirstPlayer = dealers[0];
            Clients.Caller.NewGame(); // reset score totals (card table), hide winner markers
            while (((game.EWTotal < scoreTarget && game.NSTotal < scoreTarget) || game.EWTotal == game.NSTotal || game.Capot) && game.Rounds < maxRounds)
            {
                //System.Diagnostics.Debug.WriteLine("Entering GameController -----------------------------------" + game.GameId);
                RoundController(game);
            }
            if (game.Rounds == maxRounds)
            {
                for (int i = 0; i < 4; i++)
                {
                    game.Players[i].Agent.ModifyFitness(-50 * punishment);
                    game.Players[i].Agent.Errors += 50;
                }
            }
            EndGame(game);
            //log.Debug("Leaving GameController.");
        }

        public void RoundController(BelotGame game)
        {
            //log.Debug("Entering RoundController.");
            Thread.Sleep(10);
            game.NewRound();
            if (isReplay)
            {
                Clients.Caller.SetTurnIndicator(game.Turn); // show dealer
                Clients.Caller.SetDealerMarker(game.Turn);
                Clients.Caller.NewRound(); // reset table, reset board, disable cards, reset suit selection 
                Thread.Sleep(botDelay); // for showing dealer
            }

            if (isReplay) game.Deck = decks[round];
            else
            {
                //log.Debug("Entering Shuffle");
                game.Shuffle();
                //log.Debug("Leaving Shuffle");
            }
            //else
            //log.Debug("Entering Deal");
            game.Deal(5);
            //log.Debug("Leaving Deal");

            if (isReplay)
            {
                for (int i = 0; i < 4; i++)
                {
                    Clients.Caller.Deal(new JavaScriptSerializer().Serialize(game.Hand[i]), i);
                }
            }

            while (!game.SuitDecided())
            {
                if (isReplay) Clients.Caller.SetTurnIndicator(game.Turn);
                CallController(game);
            }

            if (game.RoundSuit != 0)
            {

                game.Turn = game.FirstPlayer;
                if (isReplay) Clients.Caller.SetTurnIndicator(game.Turn);
                game.Deal(3);
                if (isReplay)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Clients.Caller.Deal(new JavaScriptSerializer().Serialize(game.Hand[i]), i);
                    }
                }
                if (!isReplay && game.RoundSuit != 5)
                {
                    game.FindRuns();
                    game.FindCarres();
                    game.TruncateRuns();
                    game.FindBelots();
                }

                while (game.NumCardsPlayed < 32)
                {
                    if (isReplay) Clients.Caller.SetTurnIndicator(game.Turn);
                    TrickController(game);
                }

                Clients.Caller.NewRound();
                if (isReplay)
                {
                    game.EWTotal += points[round][0];
                    game.NSTotal += points[round][1];
                }
                else game.FinalisePoints();
                //if (game.Inside) game.Players[game.Caller].Agent.ModifyFitness(-0.5 * reward);

                if (!isReplay)
                {
                    if (game.Caller == 0 && game.EWRoundPoints > game.NSRoundPoints) game.Players[0].Agent.ModifyFitness(5 * reward);
                    else if (game.Caller == 1 && game.NSRoundPoints > game.EWRoundPoints) game.Players[1].Agent.ModifyFitness(5 * reward);
                    else if (game.Caller == 2 && game.EWRoundPoints > game.NSRoundPoints) game.Players[2].Agent.ModifyFitness(5 * reward);
                    else if (game.Caller == 3 && game.NSRoundPoints > game.EWRoundPoints) game.Players[3].Agent.ModifyFitness(5 * reward);
                }

                if (isReplay) Clients.Caller.UpdateScoreTotals(game.EWTotal, game.NSTotal);
                //Clients.All.ShowRoundSummary(game.TrickPoints, game.DeclarationPoints, game.BelotPoints, game.Result, game.EWRoundPoints, game.NSRoundPoints);
                //Thread.Sleep(6000);
                //Clients.All.HideRoundSummary();

            }
            if (isReplay)
            {
                round++;
                callTracker = 0;
                playTracker = 0;
            }
            //log.Debug("Leaving RoundController.");
        }

        public void CallController(BelotGame game)
        {

            while (pause)
            {

            }

            //log.Debug("Entering CallController.");

            if (isReplay)
            {
                game.NominateSuit(calls[round][callTracker]);
                AnnounceSuit(game.SuitCall, game.Turn);
            }
            else
            {
                int[] validCalls = game.ValidCalls();
                if (validCalls.Sum() == 0) game.NominateSuit(0); // auto-pass
                else // bot
                {

                    int suit = game.Players[game.Turn].Agent.CallSuit(GetNNInput(game));
                    if (suit == 0) game.NominateSuit(0);
                    else if (validCalls[suit - 1] == 1)
                    {
                        //game.Players[game.Turn].Agent.ModifyFitness(reward);
                        if (!game.Players[game.Turn].Agent.CallVariation.Contains(suit)) game.Players[game.Turn].Agent.CallVariation.Add(suit);
                        if (game.EnableLogging)
                        {
                            if (suit < 7) game.Log.Information(game.GetDisplayName(game.Turn) + " attempted to call " + BelotHelpers.GetSuitNameFromNumber(suit) + ".");
                            else if (suit == 7) game.Log.Information(game.GetDisplayName(game.Turn) + " attempted to double.");
                            else game.Log.Information(game.GetDisplayName(game.Turn) + " attempted to redouble.");
                        }
                        game.NominateSuit(suit);
                    }
                    else
                    {
                        game.Players[game.Turn].Agent.ModifyFitness(-10 * reward);
                        game.Players[game.Turn].Agent.Errors++;
                        game.NominateSuit(basic.CallSuit(game.Hand[game.Turn], validCalls));
                    }
                }
                //Thread.Sleep(botDelay);
            }

            if (--game.Turn == -1) game.Turn = 3;
            if (isReplay) callTracker++;
            //log.Debug("Leaving CallController.");
        }

        public void TrickController(BelotGame game)
        {
            //log.Debug("Entering TrickController.");
            while (game.PlayedCards.Where(c => c != "c0-00").Count() < 4)
            {
                while (pause)
                {

                }

                string card;

                if (isReplay)
                {
                    card = plays[round][playTracker];
                }
                else
                {
                    if (game.Hand[game.Turn].Where(c => c != "c0-00").Count() == 1) // auto-play last card
                    {
                        game.PlayCard(game.Hand[game.Turn].Where(c => c != "c0-00").First()); // no extra declaration is possible on last card -> skip straight to PlayCardRequest
                        CardPlayEnd(game);
                        continue;
                    }

                    int[] validCards = game.ValidCards();

                    card = new AgentBasic().SelectCard(game.Hand[game.Turn], validCards, game.GetWinners(game.Turn), game.PlayedCards, game.Turn, game.DetermineWinner(), game.RoundSuit, game.TrickSuit, game.EWCalled);
                    //int choice = game.Players[game.Turn].Agent.PlayCard(GetNNInput(game));
                    //if (validCards[choice] == 0)
                    //{
                    //    game.Players[game.Turn].Agent.ModifyFitness(-punishment);
                    //    game.Players[game.Turn].Agent.Errors++;
                    //    if (game.EnableLogging) game.Log.Information(game.GetDisplayName(game.Turn) + " attempted to play " + game.Hand[game.Turn][choice]);
                    //    card = game.Hand[game.Turn][Array.IndexOf(validCards, 1)];
                    //}
                    //else card = game.Hand[game.Turn][choice];
                }

                game.PlayCard(card);

                if (isReplay)
                {
                    if (playTracker < 29) if (declarations[round][playTracker].Count > 0) AnnounceExtras(game.Turn);
                }
                else
                {
                    if (game.RoundSuit != 5)
                    {
                        //bool belot = false;

                        if (game.CheckBelot(card))
                        {
                            game.DeclareBelot();
                            //belot = true;
                        }

                        if (game.NumCardsPlayed < 5)
                        {
                            game.DeclareRuns();
                            game.DeclareCarres();
                        }
                        //AnnounceExtras(game, belot);
                    }
                }

                CardPlayEnd(game);

                if (isReplay) playTracker++;
            }

            int winner = game.DetermineWinner();

            if (!isReplay)
            {
                if (winner == 0 || winner == 2)
                {
                    game.EWRoundPoints += game.CalculateTrickPoints();
                    game.EWWonATrick = true;
                }
                else
                {
                    game.NSRoundPoints += game.CalculateTrickPoints();
                    game.NSWonATrick = true;
                }
            }

            if (botDelay > 0)
            {
                Clients.All.ShowTrickWinner(winner);
                Thread.Sleep(1000);
            }

            if (game.NumCardsPlayed < 32)
            {
                if (isReplay) Clients.Caller.ResetTable();
                game.Turn = winner;
                game.PlayedCards = new string[] { "c0-00", "c0-00", "c0-00", "c0-00" };
            }
            game.HighestTrumpInTrick = 0;
            game.TrickSuit = 0;
            //log.Debug("Leaving TrickController.");
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

            if (isReplay)
            {
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
            }
            else
            {
                if (game.Players[0].Agent.Errors == 0 && game.Players[1].Agent.Errors == 0 && game.Players[2].Agent.Errors == 0 && game.Players[3].Agent.Errors == 0)
                {
                    deleteLog = false;
                }

                for (int i = 0; i < 4; i++)
                {
                    int diff = game.NSTotal - game.EWTotal;
                    diff *= (int)Math.Pow(-1, i % 2 + 1);
                    //game.Players[i].Agent.ModifyFitness(diff);
                    game.Players[i].Agent.ModifyFitness(game.Players[i].Agent.CallVariation.Count);

                    //if (Math.Abs(diff) > 2000)
                    //{
                    //    deleteLog = false;
                    //}
                }

                for (int i = 0; i < 4; i++)
                {
                    game.Players[i].Agent.GamesPlayed++;
                }
            }
            if (game.EnableLogging) game.CloseLog();
            if (game.EnableLogging && deleteLog) System.IO.File.Delete(System.Web.Hosting.HostingEnvironment.MapPath("~/Logs/" + game.GameId + ".txt"));
            isReplay = false;
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

        // -------------------- Agent Advanced --------------------

        public double[] GetNNInput(BelotGame game) // 107 inputs with suitCall, 50 without
        {
            List<double> input = new List<double>();
            input.Add(game.RoundSuit);
            //input.Add(game.TrickSuit);
            input.Add(game.Turn);
            for (int i = 0; i < game.Hand[game.Turn].Count; i++)
            {
                input.Add(BelotHelpers.GetCardNumber(game.Hand[game.Turn][i]));
            }
            //if (game.Hand[game.Turn].Count == 5)
            //{
            //    input.Add(0);
            //    input.Add(0);
            //    input.Add(0);
            //}
            //int[] cardsFromPreviousTricks = CompleteList(game.AllPlayedCards, 32);
            //for (int i = 0; i < 32; i++)
            //{
            //    input.Add(cardsFromPreviousTricks[i]);
            //}
            input.Add(game.EWTotal);
            input.Add(game.NSTotal);
            //input.Add(scoreTarget);
            //int[] suitCall = CompleteList(game.SuitCall, 57);
            //for (int i = 0; i < 57; i++)
            //{
            //    input.Add(suitCall[i]);
            //}
            return input.ToArray();
        }

        public int[] CompleteList(List<int> list, int max) // max = 57 for suitCall, 32 for cardsFromPreviousTricks
        {
            return list.ToArray().Concat(new int[max - list.Count]).ToArray();
        }


        // -------------------- Gameplay --------------------

        public void CardPlayEnd(BelotGame game)
        {
            //log.Debug("Entering CardPlayEnd.");
            if (isReplay) Clients.Caller.SetTableCard(game.Turn, game.PlayedCards[game.Turn]);
            Thread.Sleep(botDelay);
            if (game.NumCardsPlayed % 4 != 0) if (--game.Turn == -1) game.Turn = 3;
            if (game.NumCardsPlayed < 32 && isReplay) Clients.Caller.SetTurnIndicator(game.Turn);
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