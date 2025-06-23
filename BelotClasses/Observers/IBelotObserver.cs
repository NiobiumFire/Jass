using BelotWebApp.BelotClasses.Cards;

namespace BelotWebApp.BelotClasses.Observers
{
    public interface IBelotObserver
    {
        Task OnTurnChanged();
        Task OnNewGame();
        Task OnNewRound();
        Task OnDeal();
        Task OnPendingSuitNomination(int[] validCalls);
        Task OnSuitNomination();
        Task OnNoCallDecided();
        Task OnCallDecided();
        Task OnPendingCardPlay(int[] validCards);
        Card OnBotSelectCard(BelotGame game, int[] validCards);
        Task OnDeclaration(List<string> messages, List<string> emotes);
        Task OnCardPlayEnd();
        Task OnHumanLastCard();
        Task OnTrickWinnerDetermined(int winner);
        Task OnResetTable();
        Task OnRoundComplete(string message);
        Task OnGameComplete();
    }
}
