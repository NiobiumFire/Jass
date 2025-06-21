using BelotWebApp.BelotClasses.Observers;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace BelotWebApp.BelotClasses
{
    public class BelotGameRegistry
    {
        private readonly ConcurrentDictionary<string, BelotGameContext> _contexts = new();

        public BelotGameContext? GetContext(string roomId)
        {
            _contexts.TryGetValue(roomId, out var context);
            return context;
        }

        public void AddContext(string roomId, BelotGameContext context)
        {
            _contexts[roomId] = context;
        }

        public void RemoveContext(string roomId)
        {
            _contexts.TryRemove(roomId, out _);
        }

        public void RefreshObserver(string roomId, IHubCallerClients newClients)
        {
            if (_contexts.TryGetValue(roomId, out var context) && context.Observer is LiveBelotObserver liveObserver)
            {
                liveObserver.UpdateClients(newClients);
            }
        }

        public IEnumerable<BelotGame> GetAllGames() => _contexts.Values.Select(g => g.Game);

        public bool GamesOngoing() => !_contexts.IsEmpty;
    }
}
