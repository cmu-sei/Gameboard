// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;

namespace Gameboard.Api.Data
{

    public class TicketStore: Store<Ticket>, ITicketStore
    {
        public CoreOptions Options { get; }

        public TicketStore(GameboardDbContext dbContext, CoreOptions options)
        :base(dbContext)
        {
            Options = options;
        }

        public async Task<Ticket> Load(string id)
        {
            return await DbSet
                .FirstOrDefaultAsync(c => c.Id == id)
            ;
        }

        public async Task<Ticket> Load(Api.Ticket model)
        {
            return await DbSet
                .FirstOrDefaultAsync(s =>
                    s.Id == model.Id
                );
            ;
        }

        public async Task<Ticket> LoadDetails(string id)
        {
            return await DbSet
                .Include(c => c.Requester)
                .Include(c => c.Assignee)
                .Include(c => c.Creator)
                .Include(c => c.Activity)
                    .ThenInclude(a => a.User)
                .Include(c => c.Activity)
                    .ThenInclude(a => a.Assignee)
                .Include(c => c.Challenge)
                .Include(c => c.Player)
                .Include(c => c.Player.Game)
                // .Include(c => c.Challenge.Player)
                // .Include(c => c.Challenge.Game)
                .FirstOrDefaultAsync(c => c.Id == id)
            ;
        }

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
                    t.Challenge.Tag.ToLower().Contains(term)
                );
            }
            
            return q;
        }

        public async Task<Ticket> ResolveApiKey(string hash)
        {
            return await DbSet.FirstOrDefaultAsync();
        }
    }
}
