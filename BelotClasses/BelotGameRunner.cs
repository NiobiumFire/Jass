namespace BelotWebApp.BelotClasses
{
    public static class BelotGameRunner
    {
        public static void ContinueFromDeal(BelotRoom room)
        {
            room.Game.WaitDeal = false;
            Continue(room);
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
