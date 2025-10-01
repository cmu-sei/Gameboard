// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Structure.Logging;

public class LogEventId
{
    // let's do 1xxx for info, 2xxx for warning, 3xxx for error, and 4xxx for critical
    public const int Challenge_SyncWithGameEngine = 1001;
    public const int Hub_Connection_Connected = 1002;
    public const int Hub_Connection_Disconnected = 1003;
    public const int GameHub_Group_JoinStart = 1004;
    public const int GameHub_Group_JoinEnd = 1005;
    public const int SupportHub_Staff_JoinStart = 1006;
    public const int SupportHub_Staff_JoinEnd = 1007;

    public const int Db_LongRunningQuery = 2001;

    public const int GameStart_Failed = 4001;
}
