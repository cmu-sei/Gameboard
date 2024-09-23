using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Support;

/// <summary>
/// Lists the users who are "attached" (e.g. created, are assigned to, are on the same team as, etc.)
/// to a ticket. Separate from ITicketService because of referential loops and because those services
/// are too big anyway.
/// </summary>
public interface ITicketAttachedUsersProvider
{
    Task<IEnumerable<TicketAttachedUser>> GetAttachedUsers(string ticketId);
}

internal class TicketAttachedUsersProvider(IUserRolePermissionsService permissionsService, IStore store) : ITicketAttachedUsersProvider
{
    private readonly IUserRolePermissionsService _permissionsService = permissionsService;
    private readonly IStore _store = store;

    public async Task<IEnumerable<TicketAttachedUser>> GetAttachedUsers(string ticketId)
    {
        // first we collect all the on-ticket fields
        // to figure out which users care about this ticket
        var ticketInfo = await _store
            .WithNoTracking<Data.Ticket>()
            .Select(t => new
            {
                t.Id,
                AssignedUserId = t.AssigneeId,
                CreatorUserId = t.CreatorId,
                RequesterUserId = t.RequesterId,
                t.TeamId
            })
            .SingleAsync(t => t.Id == ticketId);

        var distinctUserIds = new string[]
        {
            ticketInfo.AssignedUserId,
            ticketInfo.CreatorUserId,
            ticketInfo.RequesterUserId
        }
        .Where(id => id.IsNotEmpty())
        .Distinct()
        .ToArray();

        // find roles which are eligible to receive support notifications
        var supportRoles = await _permissionsService.GetRolesWithPermission(PermissionKey.Support_ManageTickets);

        // then we pull those users plus any player's user who is on the ticket team
        var users = await _store
            .WithNoTracking<Data.User>()
            .Where
            (
                u =>
                    (distinctUserIds.Length > 0 && distinctUserIds.Contains(u.Id)) ||
                    (ticketInfo.TeamId != null && ticketInfo.TeamId != "" && u.Enrollments.Any(p => p.TeamId == ticketInfo.TeamId)) ||
                    supportRoles.Contains(u.Role)
            )
            .Select(u => new
            {
                u.Id,
                u.ApprovedName,
                IsSupportPersonnel = supportRoles.Contains(u.Role),
                TeamIds = u.Enrollments.Select(p => p.TeamId)
            })
            .ToArrayAsync();

        return users.Select(u => new TicketAttachedUser
        {
            TicketId = ticketId,
            Id = u.Id,
            Name = u.ApprovedName,
            IsAssignedTo = u.Id == ticketInfo.AssignedUserId,
            IsCreatedBy = u.Id == ticketInfo.CreatorUserId,
            IsRequestedBy = u.Id == ticketInfo.RequesterUserId,
            IsSupportPersonnel = u.IsSupportPersonnel,
            IsTeammate = u.TeamIds.Any(tId => tId == ticketInfo.TeamId)
        });
    }
}
