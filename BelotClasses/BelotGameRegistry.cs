using System.Collections.Concurrent;

namespace BelotWebApp.BelotClasses
{
    public class BelotGameRegistry
    {
        private readonly ConcurrentDictionary<string, BelotGame> _games = new();

        public BelotGame? GetGame(string roomId)
        {
            _games.TryGetValue(roomId, out var game);
            return game;
        }

        public void AddGame(string roomId, BelotGame game)
        {
            _games[roomId] = game;
        }

        public void RemoveGame(string roomId)
        {
            _games.TryRemove(roomId, out _);
        }

        public IEnumerable<BelotGame> GetAllGames() => _games.Values;

        public bool GamesOngoing() => !_games.IsEmpty;
    }
}
