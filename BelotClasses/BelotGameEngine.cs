using BelotWebApp.BelotClasses.Agents;
using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Declarations;
using BelotWebApp.BelotClasses.Observers;

namespace BelotWebApp.BelotClasses
{
    public class BelotGameEngine
    {
        private readonly BelotGame _game;
        private readonly IBelotObserver _observer;

        public BelotGameEngine(BelotGame game, IBelotObserver observer)
        {
            _game = game;
            _observer = observer;
        }

        public async Task GameController()
        {
            if (_game.IsNewGame)
            {
                _game.IsNewGame = false;
                _game.NewGame();

                await _observer.OnNewGame().ConfigureAwait(false);
            }

            while (_game.IsRunning && ((_game.EWTotal < BelotGame.scoreTarget && _game.NSTotal < BelotGame.scoreTarget) || _game.EWTotal == _game.NSTotal || _game.Capot) && !_game.WaitDeal && !_game.WaitCall && !_game.WaitCard)
            {
                await RoundController().ConfigureAwait(false);
            }

            if (!_game.WaitDeal && !_game.WaitCall && !_game.WaitCard)
            {
                _game.RecordGameEnd();
                await EndGame().ConfigureAwait(false);
            }
        }

        public async Task RoundController()
        {
            if (_game.IsNewRound)
            {
                _game.IsNewRound = false;
                _game.NewRound();

                if (_game.Players[_game.Turn].IsHuman)
                {
                    _game.WaitDeal = true;
                }

                await _observer.OnNewRound().ConfigureAwait(false);

                if (_game.Players[_game.Turn].IsHuman)
                {
                    return;
                }
            }

            if (!_game.WaitDeal && _game.CardsDealt == 0)
            {
                _game.Shuffle();
                _game.Deal(5);

                await _observer.OnDeal().ConfigureAwait(false);
            }

            if (_game.NumCardsPlayed == 0)
            {
                while (_game.IsRunning && !_game.SuitDecided() && !_game.WaitCall)
                {
                    await _observer.OnTurnChanged().ConfigureAwait(false);

                    await CallController().ConfigureAwait(false);
                }
            }

            if (_game.RoundCall == Call.Pass && !_game.WaitCall)
            {
                await _observer.OnNoCallDecided().ConfigureAwait(false);

                _game.IsNewRound = true;
            }
            else if (_game.RoundCall == Call.FiveUnderNine)
            {
                _game.IsNewRound = true;
            }
            else if (_game.RoundCall != 0 && !_game.WaitCall)
            {
                if (_game.NumCardsPlayed == 0)
                {
                    _game.Turn = _game.FirstPlayer;

                    await _observer.OnCallDecided().ConfigureAwait(false);

                    _game.Deal(3);

                    await _observer.OnDeal().ConfigureAwait(false);

                    if (_game.RoundCall != Call.NoTrumps)
                    {
                        _game.FindCarres();
                        _game.FindRuns();
                        _game.FindBelots();
                    }
                }

                while (_game.IsRunning && _game.NumCardsPlayed < 32 && !_game.WaitCard)
                {
                    await _observer.OnTurnChanged().ConfigureAwait(false);

                    await TrickController().ConfigureAwait(false);
                }

                if (_game.NumCardsPlayed == 32)
                {
                    string message = _game.FinalisePoints();
                    _game.ScoreHistory.Add([_game.EWRoundPoints, _game.NSRoundPoints]);

                    await _observer.OnRoundComplete(message).ConfigureAwait(false);

                    _game.IsNewRound = true;
                    _game.RecordTrickEnd();
                }
            }
        }
        
        public async Task CallController()
        {
            int[] validCalls = _game.ValidCalls();

            var player = _game.Players[_game.Turn];

            if (validCalls.Sum() == 0)
            {
                _game.NominateSuit(0); // auto-pass

                await _observer.OnSuitNomination().ConfigureAwait(false);

                if (--_game.Turn == -1) _game.Turn = 3;
            }
            else if (player.IsHuman)
            {
                _game.WaitCall = true;

                await _observer.OnPendingSuitNomination(validCalls).ConfigureAwait(false);
            }
            else // bot
            {
                _game.NominateSuit(AgentBasic.CallSuit(_game.Hand[_game.Turn], validCalls));

                await _observer.OnSuitNomination().ConfigureAwait(false);

                if (--_game.Turn == -1) _game.Turn = 3;
            }
        }

        public async Task TrickController()
        {
            while (_game.TableCards.Count(c => !c.IsNull()) < 4 && !_game.WaitCard)
            {
                var hand = _game.Hand[_game.Turn];
                if (hand.Count(c => !c.Played) == 1) // auto-play last card
                {
                    if (_game.Players[_game.Turn].IsHuman)
                    {
                        await _observer.OnHumanLastCard().ConfigureAwait(false);
                    }
                    _game.PlayCard(hand.FirstOrDefault(c => !c.Played)!); // no extra declaration is possible on last card -> skip straight to PlayCardRequest
                    _game.RecordCardPlayed([]);
                    await CardPlayEnd().ConfigureAwait(false);
                    continue;
                }
                int[] validCards = _game.ValidCards();
                if (_game.Players[_game.Turn].IsHuman)
                {
                    _game.WaitCard = true;

                    await _observer.OnPendingCardPlay(validCards).ConfigureAwait(false);
                }
                else
                {
                    var card = AgentBasic.SelectCard(_game.Hand[_game.Turn], validCards, _game.GetWinners(_game.Turn), _game.TableCards, _game.Turn, _game.DetermineWinner(), _game.RoundCall, _game.TrickSuit, _game.EWCalled, _game.Caller);

                    _game.PlayCard(card);

                    List<string> messages = [];
                    List<string> emotes = [];

                    if (_game.RoundCall != Call.NoTrumps)
                    {
                        List<Declaration> declaredDeclarations = [];
                        foreach (var declaration in _game.Declarations.Where(d => d.Player == _game.Turn && (d is not Run run || run.IsValid) && d.IsDeclarable))
                        {
                            declaration.Declared = true;
                            declaredDeclarations.Add(declaration);
                        }

                        (messages, emotes) = BelotHelpers.GetDeclarationMessagesAndEmotes(declaredDeclarations, _game);

                        await _observer.OnDeclaration(messages, emotes).ConfigureAwait(false);
                    }

                    _game.RecordCardPlayed(emotes);
                    await CardPlayEnd().ConfigureAwait(false);
                }
            }
            if (!_game.WaitCard) // trick end
            {
                int winner = _game.DetermineWinner();
                if (winner == 0 || winner == 2)
                {
                    _game.EWRoundPoints += _game.CalculateTrickPoints();
                    _game.EWWonATrick = true;
                }
                else
                {
                    _game.NSRoundPoints += _game.CalculateTrickPoints();
                    _game.NSWonATrick = true;
                }

                await _observer.OnTrickWinnerDetermined(winner).ConfigureAwait(false);

                if (_game.NumCardsPlayed < 32)
                {
                    await _observer.OnResetTable().ConfigureAwait(false);

                    _game.Turn = winner;
                    _game.RecordTrickEnd();
                    _game.TableCards = [new(), new(), new(), new()];
                }
                _game.HighestTrumpInTrick = 0;
                _game.TrickSuit = null;
            }
        }

        public async Task EndGame()
        {
            await _observer.OnGameComplete().ConfigureAwait(false);

            _game.IsNewGame = true;
            _game.CloseLog();
        }

        public async Task CardPlayEnd()
        {
            await _observer.OnCardPlayEnd().ConfigureAwait(false);

            if (_game.NumCardsPlayed % 4 != 0 && --_game.Turn == -1)
            {
                _game.Turn = 3;
            }
            if (_game.NumCardsPlayed < 32)
            {
                await _observer.OnTurnChanged().ConfigureAwait(false);
            }
        }
    }
}
