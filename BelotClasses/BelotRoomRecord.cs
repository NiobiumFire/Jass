namespace BelotWebApp.BelotClasses
{
    public record BelotRoomRecord(string RoomId, string[] Players, bool Started, int ScoreTarget, bool AllowChat);
}
