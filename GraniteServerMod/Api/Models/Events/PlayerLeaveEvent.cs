namespace GraniteServer.Api.Models.Events;

public class PlayerLeaveEvent : EventDto<PlayerEventData>
{
    public PlayerLeaveEvent()
    {
        EventType = "player.leave";
    }
}
