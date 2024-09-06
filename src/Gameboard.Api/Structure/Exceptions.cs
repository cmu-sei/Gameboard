// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;

namespace Gameboard.Api
{
    internal class MissingRequiredInput<T> : GameboardValidationException
    {
        internal MissingRequiredInput(string inputName, T input) : base($"""Your input for "{inputName}" was either missing or incorrectly formed (found \"{input}\").""") { }
    }

    internal class AlreadyRegistered : GameboardValidationException
    {
        internal AlreadyRegistered(string userId, string gameId) : base($"Player {userId} is already registered for game {gameId}.") { }
    }

    internal class InvalidInvitationCode : GameboardException
    {
        internal InvalidInvitationCode(string code, string reason) : base($"""Invitation code {code} is invalid: {reason} """) { }
    }

    internal class InvalidParameterValue<T> : GameboardValidationException
    {
        internal InvalidParameterValue(string parameterName, string ruleDescription, T value) : base($"""Parameter "{parameterName}" requires a value which complies with: "{ruleDescription}". Its value was "{value}". """) { }
    }

    internal class MissingRequiredDate(string propertyName) : GameboardValidationException($"The date property {propertyName} is required.") { }

    internal class NotYetRegistered : GameboardException
    {
        internal NotYetRegistered(string playerId, string gameId)
            : base($"User {playerId} hasn't yet registered for game {gameId}.") { }
    }

    internal class RegistrationIsClosed : GameboardException
    {
        internal RegistrationIsClosed(string gameId, string addlMessage = null) :
            base($"Registration for game {gameId} is closed.{(addlMessage.NotEmpty() ? $" [{addlMessage}]" : string.Empty)}")
        { }
    }

    internal class ResourceAlreadyExists<T> : GameboardException where T : class, IEntity
    {
        internal ResourceAlreadyExists(string id, string addlMessage = null) :
            base($"Couldn't create resource '{id}' of type {typeof(T).Name} because it already exists.{(addlMessage.NotEmpty() ? $" {addlMessage}" : string.Empty)}")
        { }
    }

    internal class ResourceNotFound<T> : GameboardValidationException where T : class
    {
        internal ResourceNotFound(string id, string addlMessage = null)
            : base($"Couldn't find resource {id} of type {typeof(T).Name}.{(addlMessage.NotEmpty() ? $" [{addlMessage}]" : string.Empty)}") { }
    }

    internal class RequiresSameSponsor : GameboardException
    {
        internal RequiresSameSponsor(string gameId, string managerPlayerId, string managerSponsor, string playerId, string playerSponsor)
            : base($"Game {gameId} requires that all players have the same sponsor. The inviting player {managerPlayerId} has sponsor {managerSponsor}, while player {playerId} has sponsor {playerSponsor}.") { }
    }

    internal class SimpleValidatorException : GameboardValidationException
    {
        public SimpleValidatorException(string message, Exception ex = null) : base(message, ex) { }
    }

    internal class StartDateOccursAfterEndDate : GameboardValidationException
    {
        public StartDateOccursAfterEndDate(DateTimeOffset start, DateTimeOffset end) : base($"Invalid start/end date values supplied. Start date {start} occurs after End date {end}.") { }
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
    public class SessionNotAdjustable : Exception { }
    public class InvalidConsoleAction : Exception { }
    public class AlreadyExists : Exception { }
    public class ChallengeLocked : Exception { }
    public class ChallengeStartPending : Exception { }
    public class InvalideFeedbackFormat : Exception { }
    public class PlayerIsntInGame : Exception { }
    public class InvalidPlayerMode : Exception { }
    public class MissingRequiredField : Exception { }
}
