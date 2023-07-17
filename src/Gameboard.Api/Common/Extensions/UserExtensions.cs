
namespace Gameboard.Api.Common;

public static class UserExtensions
{
    public static SimpleEntity ToSimpleEntity(this User user)
        => new() { Id = user.Id, Name = user.ApprovedName };
}
