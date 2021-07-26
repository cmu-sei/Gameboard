// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;


namespace Gameboard.Api.Services
{
    public class ChallengeEventService
    {
        IChallengeEventStore Store { get; }
        IMapper Mapper { get; }

        public ChallengeEventService (
            IChallengeEventStore store,
            IMapper mapper
        ){
            Store = store;
            Mapper = mapper;
        }

        public async Task<ChallengeEvent> Create(NewChallengeEvent model)
        {
            var entity = Mapper.Map<Data.ChallengeEvent>(model);
            await Store.Create(entity);
            return Mapper.Map<ChallengeEvent>(entity);
        }

        public async Task<ChallengeEvent> Retrieve(string id)
        {
            return Mapper.Map<ChallengeEvent>(await Store.Retrieve(id));
        }

        public async Task Update(ChangedChallengeEvent account)
        {
            var entity = await Store.Retrieve(account.Id);
            Mapper.Map(account, entity);
            await Store.Update(entity);
        }

        public async Task Delete(string id)
        {
            await Store.Delete(id);
        }

        public async Task<ChallengeEvent[]> List(SearchFilter model)
        {
            var q = Store.List(model.Term);

            q = q.OrderBy(p => p.Timestamp);

            q = q.Skip(model.Skip);

            if (model.Take > 0)
                q = q.Take(model.Take);

            return await Mapper.ProjectTo<ChallengeEvent>(q).ToArrayAsync();
        }

    }

}
