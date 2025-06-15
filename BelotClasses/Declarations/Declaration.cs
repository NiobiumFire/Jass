namespace BelotWebApp.BelotClasses.Declarations
{
    public abstract class Declaration
    {
        protected Declaration(BelotGame? game, int player)
        {
            this.game = game;
            Player = player;
        }

        protected readonly BelotGame? game;

        public DeclarationType Type { get; protected set; }

        public int Player { get; protected set; }

        public bool Declared { get; set; }

        public abstract bool IsDeclarable { get; }
    }
}
