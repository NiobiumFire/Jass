using BelotWebApp.BelotClasses.Agents;
using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Users;
using BelotWebApp.BelotClasses.Turn;
using Microsoft.AspNetCore.SignalR;
using BelotWebApp.BelotClasses.Declarations;

namespace BelotWebApp.BelotClasses.Observers
{
    public class LiveBelotObserver : IBelotObserver
    {
        private readonly BelotRoom _room;
        private readonly BelotGame _game;
        private IHubCallerClients _clients;
        private IClientProxy _group;
        private readonly object _lock = new();
        public RoundSummaryGate RoundSummaryGate = new();

        public LiveBelotObserver(BelotRoom room, IHubCallerClients clients)
        {
            _room = room;
            _game = room.Game;
            UpdateClients(clients);
        }

        public void UpdateClients(IHubCallerClients clients)
        {
            lock (_lock)
            {
                _clients = clients;
                _group = clients.Group(_room.RoomId);
            }
        }

        private IHubCallerClients GetClients()
        {
            lock (_lock)
            {
                return _clients;
            }
        }

        public async Task OnTurnChanged(TurnActionType turnActionType)
        {
            await _group.SendAsync("SetTurnIndicator", _game.Turn, turnActionType.ToString().ToLower()).ConfigureAwait(false);
        }

        public async Task OnNewGame()
        {
            _game.AddInitialState(Enumerable.Range(0, 4).Select(_room.GetDisplayName).ToArray());
            await _group.SendAsync("HideDeck", true).ConfigureAwait(false);
            await _group.SendAsync("DisableNewGame").ConfigureAwait(false);
            await _group.SendAsync("CloseModalsAndButtons").ConfigureAwait(false);
            await _group.SendAsync("DisableRadios").ConfigureAwait(false);
            await _group.SendAsync("NewGame", _game.GameId).ConfigureAwait(false); // reset score table (offcanvas), reset score totals (card table), hide winner markers, set game id
            await SysAnnounce("--- New game started ---");
        }

        public async Task OnNewRound()
        {
            await _group.SendAsync("SetTurnIndicator", _game.Turn, TurnActionType.Deal.ToString().ToLower()).ConfigureAwait(false); // show dealer
            await _group.SendAsync("SetDealerMarker", _game.Turn).ConfigureAwait(false);
            await _group.SendAsync("NewRound").ConfigureAwait(false); // reset table, reset board, disable cards, reset suit selection 

            var player = _game.Players[_game.Turn];
            if (player != null)
            {
                var clients = GetClients();

                if (player.PlayerType == PlayerType.Human)
                {
                    var user = _room.GetUserBySeat(_game.Turn);
                    if (user != null)
                    {
                        await clients.Client(user.ConnectionId).SendAsync("EnableDealBtn").ConfigureAwait(false);
                    }
                }
                else
                {
                    await Task.Delay(_game.BotDelay).ConfigureAwait(false);
                }
            }
        }

        public async Task OnDeal()
        {
            var clients = GetClients();

            for (int i = 0; i < 4; i++)
            {
                if (_game.Players[i]?.PlayerType == PlayerType.Human)
                {
                    var user = _room.GetUserBySeat(i);
                    if (user != null)
                    {
                        await clients.Client(user.ConnectionId).SendAsync("Deal", _game.Hand[i]).ConfigureAwait(false);
                        await clients.Client(user.ConnectionId).SendAsync("RotateCards").ConfigureAwait(false);
                    }
                }
            }
        }

        public async Task OnPendingSuitNomination(int[] validCalls)
        {
            bool fiveUnderNine = _game.Calls.Count < 4 && BelotHelpers.FiveUnderNine(_game.Hand[_game.Turn]);

            var user = _room.GetUserBySeat(_game.Turn);
            if (user != null)
            {
                var clients = GetClients();
                await clients.Client(user.ConnectionId).SendAsync("ShowSuitModal", validCalls, fiveUnderNine).ConfigureAwait(false);
            }
        }

        public async Task OnSuitNomination()
        {
            await AnnounceSuit().ConfigureAwait(false);
        }

        public async Task OnNoCallDecided()
        {
            await SysAnnounce("No suit chosen.").ConfigureAwait(false);
        }

        public async Task OnCallDecided()
        {
            await SysAnnounce($"--- Round {_game.ScoreHistory.Count + 1} started ---");
            await SysAnnounce("The round will be played in " + BelotHelpers.GetSuitNameFromNumber(_game.RoundCall) + ".").ConfigureAwait(false);
            await OnTurnChanged(TurnActionType.Deal).ConfigureAwait(false);
        }

        public async Task OnPendingCardPlay(int[] validCards)
        {
            var user = _room.GetUserBySeat(_game.Turn);
            if (user != null)
            {
                var clients = GetClients();

                if (_game.TableCards.All(c => c.IsNull()))
                {
                    if (_game.GetWinners(_game.Turn).Count(w => w == 2) == _game.Hand[_game.Turn].Count(c => !c.Played) && _game.NumCardsPlayed > 3)
                    {
                        await clients.Client(user.ConnectionId).SendAsync("ShowThrowBtn").ConfigureAwait(false);
                    }
                }
                await clients.Client(user.ConnectionId).SendAsync("EnableCards", validCards).ConfigureAwait(false);
                // once a card is clicked, declarable extras are calculated in hub method, human selects and declares extras, then the card is played and game loop reinitiates
            }
        }

        public Card OnBotSelectCard(BelotGame game, int[] validCards)
        {
            //if (game.Players[game.Turn].PlayerType == PlayerType.Basic)
            //{
            return AgentBasic.SelectCard(game.Hand[game.Turn], validCards, game.GetWinners(game.Turn), game.TableCards, game.Turn, game.DetermineWinner(), game.RoundCall, game.TrickSuit, game.EWCalled, game.Caller);
            //}
        }

        public async Task<List<string>> OnDeclaration(List<Declaration> declaredDeclarations)
        {
            var (messages, emotes) = BelotHelpers.GetDeclarationMessagesAndEmotes(declaredDeclarations, _room);

            foreach (var message in messages)
            {
                await SysAnnounce(message).ConfigureAwait(false);
            }

            if (emotes.Count > 0)
            {
                var clients = GetClients();
                await clients.Group(_room.RoomId).SendAsync("SetExtrasEmote", emotes, _game.Turn).ConfigureAwait(false);
                await Emote(_game.Turn, _game.BotDelay).ConfigureAwait(false);
            }

            return emotes;
        }

        public async Task OnCardPlayEnd()
        {
            await _group.SendAsync("SetTableCard", _game.Turn, _game.TableCards[_game.Turn]).ConfigureAwait(false);
            await Task.Delay(_game.BotDelay).ConfigureAwait(false);
        }

        public async Task OnHumanLastCard()
        {
            var user = _room.GetUserBySeat(_game.Turn);
            if (user != null)
            {
                var clients = GetClients();

                await clients.Client(user.ConnectionId).SendAsync("PlayFinalCard").ConfigureAwait(false);
            }
        }

        public async Task OnTrickWinnerDetermined(int winner)
        {
            await _group.SendAsync("ShowTrickWinner", winner).ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
        }

        public async Task OnResetTable()
        {
            await _group.SendAsync("ResetTable").ConfigureAwait(false);
        }

        public async Task OnGameComplete()
        {
            string winner = _game.EWTotal > _game.NSTotal ? "E/W" : "N/S";

            await SysAnnounce(winner + " win the game: " + _game.EWTotal + " to " + _game.NSTotal + ".").ConfigureAwait(false);

            await _group.SendAsync("SetDealerMarker", 4).ConfigureAwait(false);
            await _group.SendAsync("NewRound").ConfigureAwait(false);
            await _group.SendAsync("SetTurnIndicator", 4, null).ConfigureAwait(false);
            // animation and modal to indicate winning team?
            if (_game.EWTotal > _game.NSTotal)
            {
                await _group.SendAsync("ShowGameWinner", 0).ConfigureAwait(false);
                await _group.SendAsync("ShowGameWinner", 2).ConfigureAwait(false);
            }
            else
            {
                await _group.SendAsync("ShowGameWinner", 1).ConfigureAwait(false);
                await _group.SendAsync("ShowGameWinner", 3).ConfigureAwait(false);
            }
            await _group.SendAsync("EnableNewGame").ConfigureAwait(false);
            await _group.SendAsync("EnableRadios").ConfigureAwait(false);
        }

        public async Task OnRoundComplete(string message)
        {
            await _group.SendAsync("NewRound").ConfigureAwait(false);
            await SysAnnounce(message).ConfigureAwait(false);
            await _group.SendAsync("UpdateScoreTotals", _game.EWTotal, _game.NSTotal).ConfigureAwait(false);
            await _group.SendAsync("UpdateScoreHistoryTable").ConfigureAwait(false);

            var requiredContinueVotes = _game.Players.Count(p => p?.PlayerType == PlayerType.Human && !p.IsDisconnected);
            if (_room.ConnectedUsers.Count > 0)
            {
                var token = RoundSummaryGate.BeginRoundSummary(requiredContinueVotes);

                void ContinueVotesUpdated(int current, int expected)
                {
                    _ = _group.SendAsync("RoundSummaryContinueVotesUpdate", current, expected);
                }

                RoundSummaryGate.ContinueVotesUpdated += ContinueVotesUpdated;
                try
                {
                    var (spectators, players) = _room.GetSpectatorsAndConnectedHumanPlayers();
                    foreach (var player in players) // can vote to continue
                    {
                        await _clients.Client(player.ConnectionId).SendAsync("ShowRoundSummary", _game.TrickPoints, _game.DeclarationPoints, _game.BelotPoints, _game.Result, _game.EWRoundPoints, _game.NSRoundPoints, token, RoundSummaryGate.RoundSummaryDelay, requiredContinueVotes, false).ConfigureAwait(false);
                    }
                    foreach (var spectator in spectators) // cannot vote to continue
                    {
                        await _clients.Client(spectator.ConnectionId).SendAsync("ShowRoundSummary", _game.TrickPoints, _game.DeclarationPoints, _game.BelotPoints, _game.Result, _game.EWRoundPoints, _game.NSRoundPoints, token, RoundSummaryGate.RoundSummaryDelay, requiredContinueVotes, true).ConfigureAwait(false);
                    }

                    await RoundSummaryGate.WaitAsync();
                }
                finally
                {
                    RoundSummaryGate.ContinueVotesUpdated -= ContinueVotesUpdated;
                }

                await _group.SendAsync("HideRoundSummary").ConfigureAwait(false);
            }
        }

        // --------------------------------------

        public async Task SysAnnounce(string message)
        {
            await _group.SendAsync("AppendGameLog", $"[{GetServerDateTime()}] {message}").ConfigureAwait(false);
        }

        private static string GetServerDateTime()
        {
            //return DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            return DateTime.Now.ToString("HH:mm");
        }

        public async Task AnnounceSuit()
        {
            string username = _room.GetDisplayName(_game.Turn);

            string message = username;

            Call call = _game.Calls[^1];

            if (call == Call.Pass)
            {
                message += " passed.";
            }
            else
            {
                await _group.SendAsync("SuitNominated", call).ConfigureAwait(false);
                await _group.SendAsync("setCallerIndicator", _game.Turn).ConfigureAwait(false);

                if (call == Call.Double)
                {
                    message += " doubled!";
                }
                else if (call == Call.Redouble)
                {
                    message += " redoubled!!";
                }
                else if (call == Call.FiveUnderNine)
                {
                    message += " called five-under-nine.";
                }
                else
                {
                    message += " called " + BelotHelpers.GetSuitNameFromNumber(call) + ".";
                }
            }

            await SysAnnounce(message).ConfigureAwait(false);
            await _group.SendAsync("EmoteSuit", call, _game.Turn).ConfigureAwait(false);
            await Emote(_game.Turn, _game.BotDelay).ConfigureAwait(false);
        }

        private async Task Emote(int seat, int duration)
        {
            await _group.SendAsync("ShowEmote", seat).ConfigureAwait(false);
            await Task.Delay(duration).ConfigureAwait(false);
            await _group.SendAsync("HideEmote", seat).ConfigureAwait(false);
        }
    }
}
