using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Replays
{
    public class BelotReplayDiff
    {
        public BelotReplayDiff()
        {
            Before = new();
            After = new();
        }

        public BelotStateDiff Before { get; set; }
        public BelotStateDiff After { get; set; }

        public void SetCaller(BelotStateDiff replayState, int caller)
        {
            if (replayState.Caller != caller)
            {
                Before.Caller = replayState.Caller;
                After.Caller = caller;
            }
        }

        public void SetTurn(BelotStateDiff replayState, int turn)
        {
            if (replayState.Turn != turn)
            {
                Before.Turn = replayState.Turn;
                After.Turn = turn;
            }
        }

        public void SetDealer(BelotStateDiff replayState, int dealer)
        {
            if (replayState.Dealer != dealer)
            {
                Before.Dealer = replayState.Dealer;
                After.Dealer = dealer;
            }
        }

        public void SetRoundCall(BelotStateDiff replayState, Call roundCall)
        {
            if (replayState.RoundCall != roundCall)
            {
                Before.RoundCall = replayState.RoundCall;
                After.RoundCall = roundCall;
            }
        }

        public void ClearEmotes(BelotStateDiff replayState)
        {
            if (replayState.Emotes.Count != 0)
            {
                Before.Emotes ??= [];
                After.Emotes ??= [];

                foreach (var oldEmote in replayState.Emotes.Where(e => e.Emote != null))
                {
                    Before.Emotes.Add(new(oldEmote.Player, oldEmote.Emote));
                    After.Emotes.Add(new(oldEmote.Player, null));
                }
            }
        }

        public void SetEmote(BelotStateDiff replayState, int turn, string newEmote)
        {
            Before.Emotes ??= [];
            After.Emotes ??= [];

            foreach (var oldEmote in replayState.Emotes.Where(e => e.Emote != null))
            {
                Before.Emotes.Add(new(oldEmote.Player, oldEmote.Emote));
                After.Emotes.Add(new(oldEmote.Player, null));
            }

            Before.Emotes.Add(new(turn, null));
            After.Emotes.Add(new(turn, newEmote));
        }

        public void ClearTableCards(Card[] tableCards)
        {
            if (tableCards.Any(c => !c.IsNull()))
            {
                Before.TableCards ??= [];
                After.TableCards ??= [];

                for (int i = 0; i < 4; i++)
                {
                    if (!tableCards[i].IsNull())
                    {
                        Before.TableCards.Add(new(i, tableCards[i].Clone()));
                        After.TableCards.Add(new(i, null));
                    }
                }
            }
        }

        public void SetTableCard(int turn, Card newTableCard)
        {
            Before.TableCards ??= [];
            After.TableCards ??= [];

            Before.TableCards.Add(new(turn, null));
            After.TableCards.Add(new(turn, newTableCard.Clone()));
        }

        public static List<ReplayHandCard> CopyHandCards(List<Card>[] oldHandCards)
        {
            List<ReplayHandCard> oldReplayHandCards = [];

            for (int i = 0; i < 4; i++) // players
            {
                for (int j = 0; j < oldHandCards[i].Count; j++)
                {
                    var oldCard = oldHandCards[i][j].Played ? null : oldHandCards[i][j].Clone();
                    oldReplayHandCards.Add(new(i, j, oldCard));
                }
            }

            return oldReplayHandCards;
        }

        public void SetHandCards(List<ReplayHandCard>? oldReplayHandCards, List<Card>[] newHandCards)
        {
            Before.HandCards ??= [];
            After.HandCards ??= [];

            for (int i = 0; i < 4; i++) // players
            {
                //for (int j = 0; j < newHandCards[i].Count; j++) // cards
                for (int j = 0; j < 8; j++) // cards
                {
                    var newCard = j < newHandCards[i].Count ? newHandCards[i][j].Clone() : null;
                    var oldCard = oldReplayHandCards?.FirstOrDefault(c => c.Player == i && c.Index == j)?.Card?.Clone();
                    if ((oldCard != null || newCard != null) && !(oldCard?.Suit == newCard?.Suit && oldCard?.Rank == newCard?.Rank))
                    {
                        Before.HandCards.Add(new(i, j, oldCard));
                        After.HandCards.Add(new(i, j, newCard));
                    }
                }
            }
        }

        public void SetHandCard(int turn, int index, Card card)
        {
            Before.HandCards ??= [];
            After.HandCards ??= [];

            Before.HandCards.Add(new(turn, index, card.Clone()));
            After.HandCards.Add(new(turn, index, null));
        }
    }
}
