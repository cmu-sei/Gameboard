using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Practice;

public sealed class CantAccessPracticeChallengeGroupsUserDataException(string userId) : GameboardValidationException($"You don't have access to challenge group data for user {userId}.") { }
