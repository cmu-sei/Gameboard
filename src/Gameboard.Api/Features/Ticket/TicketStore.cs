// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public interface ITicketStore : IStore<Ticket>
{
    Task<Data.Ticket> Load(string id);
    Task<Data.Ticket> Load(int id);
    Task<Data.Ticket> Load(Api.Ticket model);
    Task<Data.Ticket> LoadDetails(string id);
    Task<Data.Ticket> LoadDetails(int id);
}

public class TicketStore(IGuidService guids, IDbContextFactory<GameboardDbContext> dbContextFactory, CoreOptions options) : Store<Ticket>(dbContextFactory, guids), ITicketStore
{
    public CoreOptions Options { get; } = options;

    public Task<Ticket> Load(string id)
        => DbSet.FirstOrDefaultAsync(c => c.Id == id);

    public Task<Ticket> Load(int id)
        => DbSet.FirstOrDefaultAsync(c => c.Key == id);

    public async Task<Ticket> Load(Api.Ticket model)
        => await DbSet.FirstOrDefaultAsync(s => s.Id == model.Id);

    public async Task<Ticket> LoadDetails(string id)
        => await DbSet
            .Include(c => c.Requester)
            .Include(c => c.Assignee)
            .Include(c => c.Creator)
            .Include(c => c.Activity)
                .ThenInclude(a => a.User)
            .Include(c => c.Activity)
                .ThenInclude(a => a.Assignee)
            .Include(c => c.Challenge)
            .Include(c => c.Player)
                .ThenInclude(p => p.Game)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<Ticket> LoadDetails(int id)
        => await DbSet
            .Include(c => c.Requester)
            .Include(c => c.Assignee)
            .Include(c => c.Creator)
            .Include(c => c.Activity)
                .ThenInclude(a => a.User)
            .Include(c => c.Activity)
                .ThenInclude(a => a.Assignee)
            .Include(c => c.Challenge)
            .Include(c => c.Player)
                .ThenInclude(p => p.Game)
            .FirstOrDefaultAsync(c => c.Key == id);

    public override IQueryable<Ticket> List(string term)
    {
        var q = base.List();
        q.Include(c => c.Assignee)
            .Include(c => c.Requester)
            .Include(c => c.Challenge);

        if (term.NotEmpty())
        {
            term = term.ToLower();
            var prefix = Options.KeyPrefix.ToLower() + "-";
            q = q.Where(t => t.Summary.ToLower().Contains(term) ||
                t.Label.Contains(term, System.StringComparison.CurrentCultureIgnoreCase) ||
                (prefix + t.Key.ToString()).Contains(term) ||
                t.Requester.ApprovedName.Contains(term, System.StringComparison.CurrentCultureIgnoreCase) ||
                t.Assignee.ApprovedName.Contains(term, System.StringComparison.CurrentCultureIgnoreCase) ||
                t.Challenge.Name.Contains(term, System.StringComparison.CurrentCultureIgnoreCase) ||
                t.Challenge.Tag.Contains(term, System.StringComparison.CurrentCultureIgnoreCase) ||
                t.Challenge.Id.Equals(term, System.StringComparison.CurrentCultureIgnoreCase) ||
                t.TeamId.Contains(term, System.StringComparison.CurrentCultureIgnoreCase) ||
                t.PlayerId.Contains(term, System.StringComparison.CurrentCultureIgnoreCase) ||
                t.RequesterId.ToLower().Contains(term)

            );
        }

        return q;
    }
}
