namespace Gameboard.Api.Structure;

public class LogEventId
{
    // let's do 1xxx for info, 2xxx for warning, 3xxx for error, and 4xxx for critical
    public const int Hub_Connection_Connected = 1001;
    public const int Hub_Connection_Disconnected = 1002;
    public const int GameHub_Group_JoinStart = 1003;
    public const int GameHub_Group_JoinEnd = 1004;
    public const int SupportHub_Staff_JoinStart = 1005;
    public const int SupportHub_Staff_JoinEnd = 1006;

    public const int GameStart_Failed = 4001;
}
