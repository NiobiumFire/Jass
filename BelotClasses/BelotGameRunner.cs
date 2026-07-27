using BelotWebApp.BelotClasses.Cards;
using BelotWebApp.BelotClasses.Observers;

namespace BelotWebApp.BelotClasses
{
    public static class BelotGameRunner
    {
        public static void ContinueFromDeal(BelotRoom room)
        {
            room.Game.WaitDeal = false;
            Continue(room);
        }

        public static async void ContinueFromCall(BelotRoom room, Call call)
        {
            room.Game.NominateSuit(call);
            room.Game.WaitCall = false;
            if (room.Observer is LiveBelotObserver live)
            {
                await live.AnnounceSuit();
            }
            if (--room.Game.Turn == -1) room.Game.Turn = 3;

            Continue(room);
        }

        public static async void ContinueFromCard(BelotRoom room)
        {
            if (room?.Game == null || room?.Observer == null)
            {
                return;
            }

            var game = room.Game;

            BelotGameEngine engine = new(game, room.Observer);
            await engine.CardPlayEnd();
            game.WaitCard = false;

            _ = Task.Run(async () =>
            {
                await engine.GameController();
            });
        }

        private static void Continue(BelotRoom room)
        {
            if (room?.Game == null || room?.Observer == null)
            {
                return;
            }

            BelotGameEngine engine = new(room.Game, room.Observer);

            _ = Task.Run(async () =>
            {
                await engine.GameController();
            });
        }
    }
}
