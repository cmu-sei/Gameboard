// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Sponsors;
using Gameboard.Api.Data;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Gameboard.Api.Services;

public class SponsorService : _Service
{
    private readonly Defaults _defaults;
    private readonly IStore _store;
    private readonly IStore<Data.Sponsor> _sponsorStore;

    public SponsorService
    (
        ILogger<SponsorService> logger,
        IMapper mapper,
        CoreOptions options,
        Defaults defaults,
        IStore store,
        IStore<Data.Sponsor> sponsorStore
    ) : base(logger, mapper, options)
    {
        _defaults = defaults;
        _sponsorStore = sponsorStore;
        _store = store;
    }

    public async Task<Sponsor> Retrieve(string id)
    {
        return Mapper.Map<Sponsor>(await _sponsorStore.Retrieve(id));
    }

    public async Task AddOrUpdate(ChangedSponsor model)
    {
        var entity = await _sponsorStore.Retrieve(model.Id);

        if (entity is not null)
        {
            Mapper.Map(model, entity);
            await _sponsorStore.Update(entity);
            return;
        }

        entity = Mapper.Map<Data.Sponsor>(model);
        await _sponsorStore.Create(entity);
    }

    public async Task<Data.Sponsor> GetDefaultSponsor()
    {
        var defaultSponsor = await _sponsorStore
            .List()
            .FirstOrDefaultAsync(s => s.Logo == _defaults.DefaultSponsor);

        if (_defaults.DefaultSponsor.IsEmpty() || defaultSponsor is null)
        {
            var firstSponsor = await _sponsorStore
                .List()
                .FirstOrDefaultAsync();

            if (firstSponsor is not null)
                return firstSponsor;
        }

        throw new CouldntResolveDefaultSponsor();
    }

    public async Task Delete(string id)
    {
        // get the sponsor (including its sponsored users and players)
        // because we need to update their sponsor to the default
        var entity = await _store
            .WithNoTracking<Data.Sponsor>()
            .Include(s => s.SponsoredPlayers)
            .Include(s => s.SponsoredUsers)
            .SingleAsync(s => s.Id == id);

        // if this sponsor sponsors any players or users, we need to update them
        if (entity.SponsoredPlayers.Any() || entity.SponsoredUsers.Any())
        {
            var defaultSponsor = await GetDefaultSponsor();

            if (entity.SponsoredPlayers.Any())
            {
                await _store
                    .WithNoTracking<Data.Player>()
                    .Where(p => p.SponsorId == id)
                    .ExecuteUpdateAsync(p => p.SetProperty(p => p.SponsorId, defaultSponsor.Id));
            }

            if (entity.SponsoredUsers.Any())
            {
                await _store
                    .WithNoTracking<Data.User>()
                    .Where(u => u.SponsorId == id)
                    .ExecuteUpdateAsync(u => u.SetProperty(u => u.SponsorId, defaultSponsor.Id));
            }
        }

        // now delete the entity
        await _sponsorStore.Delete(id);
        if (entity.Logo.IsEmpty())
            return;

        // check for their logo file and delete that too
        string path = Path.Combine(Options.ImageFolder, entity.Logo);
        if (File.Exists(path))
            File.Delete(path);
    }

    public async Task<Sponsor[]> List(SearchFilter model)
    {
        var q = _sponsorStore.List(model.Term);

        q = q.OrderBy(p => p.Name);
        q = q.Skip(model.Skip);

        if (model.Take > 0)
            q = q.Take(model.Take);

        return await Mapper.ProjectTo<Sponsor>(q).ToArrayAsync();
    }

    public async Task<Sponsor> AddOrUpdate(string id, string filename)
    {
        var entity = await _sponsorStore.Retrieve(id);

        if (entity is null)
            entity = await _sponsorStore.Create(new Data.Sponsor { Id = id });

        entity.Logo = filename;

        await _sponsorStore.Update(entity);
        return Mapper.Map<Sponsor>(entity);
    }

    public async Task<string> SetLogo(string sponsorId, IFormFile file, CancellationToken cancellationToken)
    {
        // upload the new file
        string logoFileName = file.FileName.ToLower();
        string logoPath = Path.Combine(Options.ImageFolder, logoFileName);

        using (var stream = new FileStream(logoPath, FileMode.Create))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        // record the sponsor's previous logo, if any, so we can delete it
        // if this succeeds
        var sponsor = await _store
            .WithNoTracking<Data.Sponsor>()
            .SingleAsync(s => s.Id == sponsorId, cancellationToken);
        var previousLogo = sponsor.Logo;

        // update the sponsor
        await _store
            .WithNoTracking<Data.Sponsor>()
            .Where(s => s.Id == sponsorId)
            .ExecuteUpdateAsync(s => s.SetProperty(s => s.Logo, logoFileName));

        // delete the old file if it exists
        if (previousLogo.NotEmpty())
        {
            string oldLogoPath = Path.Combine(Options.ImageFolder, previousLogo);
            if (File.Exists(oldLogoPath))
                File.Delete(oldLogoPath);
        }

        return logoFileName;
    }
}
