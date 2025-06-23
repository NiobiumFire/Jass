using BelotWebApp.BelotClasses.Agents;
using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Training;

namespace BelotWebApp.BelotClasses.Observers
{
    public class TrainingBelotObserver : IBelotObserver
    {
        private readonly BelotGame _game;
        public BelotSimulationResult _result;

        public TrainingBelotObserver(BelotGame game)
        {
            _game = game;
            _result = new();
        }

        public Task OnTurnChanged() => Task.CompletedTask;
        public Task OnNewGame() => Task.CompletedTask;
        public Task OnNewRound() => Task.CompletedTask;
        public Task OnDeal() => Task.CompletedTask;
        public Task OnPendingSuitNomination(int[] validCalls) => Task.CompletedTask;
        public Task OnSuitNomination() => Task.CompletedTask;
        public Task OnNoCallDecided() => Task.CompletedTask;
        public Task OnCallDecided() => Task.CompletedTask;
        public Task OnPendingCardPlay(int[] validCards) => Task.CompletedTask;

        public Card OnBotSelectCard(BelotGame game, int[] validCards)
        {
            var player = game.Players[game.Turn];
            var hand = game.Hand[game.Turn];

            if (player.PlayerType == Players.PlayerType.Advanced)
            {
                float[] output = player.Agent.PlayCard(game);

                //float fitnessDelta = 0f;
                //for (int i = 0; i < 8; i++)
                //{
                //    fitnessDelta += validCards[i] == 1 ? 10 * output[i] : -output[i];
                //    fitnessDelta += validCards[i];
                //}

                //player.Agent.Fitness += fitnessDelta;

                int bestIndex = Array.IndexOf(output, output.Max());

                player.Agent.Fitness += validCards[bestIndex];

                if (validCards[bestIndex] == 1)
                {
                    return hand[bestIndex];
                }
            }

            return AgentBasic.SelectCard(
                hand,
                validCards,
                game.GetWinners(game.Turn),
                game.TableCards,
                game.Turn,
                game.DetermineWinner(),
                game.RoundCall,
                game.TrickSuit,
                game.EWCalled,
                game.Caller);
        }


        public Task OnDeclaration(List<string> messages, List<string> emotes) => Task.CompletedTask;
        public Task OnCardPlayEnd() => Task.CompletedTask;
        public Task OnHumanLastCard() => Task.CompletedTask;
        public Task OnTrickWinnerDetermined(int winner) => Task.CompletedTask;
        public Task OnResetTable() => Task.CompletedTask;
        public Task OnRoundComplete(string message) => Task.CompletedTask;
        public Task OnGameComplete()
        {
            //_result.e
            return Task.CompletedTask;
        }
    }
}
