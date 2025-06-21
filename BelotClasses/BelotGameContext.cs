using BelotWebApp.BelotClasses.Observers;

namespace BelotWebApp.BelotClasses
{
    public class BelotGameContext
    {
        public BelotGameContext(BelotGame game, IBelotObserver? observer)
        {
            Game = game;
            Observer = observer;
        }

        public BelotGame Game { get; set; }
        public IBelotObserver? Observer { get; set; }
    }

}
