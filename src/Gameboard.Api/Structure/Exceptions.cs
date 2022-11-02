// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gameboard.Api.Validators;

namespace Gameboard.Api
{
    internal class GameboardException : Exception
    {
        internal GameboardException(string message) : base(message) { }
    }

    internal class AlreadyRegistered : GameboardException
    {
        internal AlreadyRegistered(string userId, string gameId) : base($"Player {userId} is already registered for game {gameId}.") { }
    }

    internal class InvalidInvitationCode : GameboardException
    {
        internal InvalidInvitationCode(string code, string reason) : base(reason) { }
    }

    internal class NotYetRegistered : GameboardException
    {
        internal NotYetRegistered(string playerId, string gameId)
            : base($"User {playerId} hasn't yet registered for game {gameId}.") { }
    }

    internal class RegistrationIsClosed : GameboardException
    {
        internal RegistrationIsClosed(string gameId, string addlMessage = null) :
            base($"Registration for game {gameId} is closed.${(addlMessage.HasValue() ? $" [{addlMessage}]" : string.Empty)}")
        { }
    }

    internal class ResourceNotFound<T> : GameboardException where T : class
    {
        internal ResourceNotFound(string id, string addlMessage = null)
            : base($"Couldn't find resource {id} of type {typeof(T).Name}.{(addlMessage.HasValue() ? $" [{addlMessage}]" : string.Empty)}") { }
    }

    internal class RequiresSameSponsor : GameboardException
    {
        internal RequiresSameSponsor(string gameId, string managerPlayerId, string managerSponsor, string playerId, string playerSponsor)
            : base($"Game {gameId} requires that all players have the same sponsor. The inviting player {managerPlayerId} has sponsor {managerSponsor}, while player {playerId} has sponsor {playerSponsor}.") { }
    }

    internal class SessionNotActive : GameboardException
    {
        internal SessionNotActive(string playerId) : base($"Player {playerId} has an inactive session.") { }
    }

    internal class TeamIsFull : GameboardException
    {
        internal TeamIsFull(string invitingPlayerId, int teamSize, int maxTeamSize)
            : base($"Inviting player {invitingPlayerId} has {teamSize} players on their team, and the max team size for this game is {maxTeamSize}.") { }
    }

    internal class ValidationTypeFailure<TValidator> : GameboardException where TValidator : IModelValidator
    {
        internal ValidationTypeFailure(Type objectType)
            : base($"Validator type {typeof(TValidator)} was unable to validate an object of type {objectType.Name}") { }
    }

    internal class ValidationTypeFailure<TValidator, TObject> : GameboardException where TValidator : IModelValidator
    {
        internal ValidationTypeFailure() :
            base($"Validator type {typeof(TValidator)} was unable to validate an object of type {typeof(TObject).Name}")
        { }
    }


    public class ActionForbidden : Exception { }
    public class EntityNotFound : Exception { }
    public class GameNotActive : Exception { }
    public class InvalidSessionWindow : Exception { }
    public class SessionLimitReached : Exception { }
    public class InvalidTeamSize : Exception { }
    public class InvalidConsoleAction : Exception { }
    public class AlreadyExists : Exception { }
    public class GamespaceLimitReached : Exception { }
    public class ChallengeLocked : Exception { }
    public class ChallengeStartPending : Exception { }
    public class SessionAlreadyStarted : Exception { }
    public class InvalideFeedbackFormat : Exception { }
    public class MissingRequiredField : Exception { }
    public class PlayerIsntOnTeam : Exception { }
}
