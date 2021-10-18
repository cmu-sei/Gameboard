// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api
{
    public class ActionForbidden: Exception {}
    public class EntityNotFound: Exception {}
    public class AlreadyRegistered: Exception {}
    public class NotYetRegistered: Exception {}
    public class InvalidInvitationCode: Exception {}
    public class RequiresSameSponsor: Exception {}
    public class RegistrationIsClosed: Exception {}
    public class TeamIsFull: Exception {}
    public class ResourceNotFound: Exception {}
    public class GameNotActive: Exception {}
    public class SessionNotActive: Exception {}
    public class InvalidSessionWindow: Exception {}
    public class SessionLimitReached: Exception {}
    public class InvalidTeamSize: Exception {}
    public class InvalidConsoleAction: Exception {}
    public class AlreadyExists: Exception {}
    public class GamespaceLimitReached: Exception {}
    public class ChallengeLocked: Exception {}
}
