namespace GraniteServer.Api.Models.Events;

public class PlayerJoinEvent : EventDto<PlayerEventData>
{
    public PlayerJoinEvent()
    {
        EventType = "player.join";
    }
}
