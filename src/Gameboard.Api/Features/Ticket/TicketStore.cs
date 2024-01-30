// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public class TicketStore : Store<Ticket>, ITicketStore
{
    public CoreOptions Options { get; }

    public TicketStore(IGuidService guids, GameboardDbContext dbContext, CoreOptions options)
        : base(dbContext, guids)
    {
        Options = options;
    }

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
                t.Label.ToLower().Contains(term) ||
                (prefix + t.Key.ToString()).Contains(term) ||
                t.Requester.ApprovedName.ToLower().Contains(term) ||
                t.Assignee.ApprovedName.ToLower().Contains(term) ||
                t.Challenge.Name.ToLower().Contains(term) ||
                t.Challenge.Tag.ToLower().Contains(term) ||
                t.TeamId.ToLower().Contains(term) ||
                t.PlayerId.ToLower().Contains(term) ||
                t.RequesterId.ToLower().Contains(term)
            );
        }

        return q;
    }
}
