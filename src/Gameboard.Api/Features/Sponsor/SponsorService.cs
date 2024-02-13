// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Sponsors;
using Gameboard.Api.Data;
using Gameboard.Api.Structure;
using Gameboard.Api.Common;

namespace Gameboard.Api.Services;

public interface ISponsorService
{
    Task AddOrUpdate(UpdateSponsorRequest model);
    void DeleteLogoFileByName(string fileName);
    IEnumerable<string> GetAllowedLogoMimeTypes();
    Task<Data.Sponsor> GetDefaultSponsor();
    Task<Sponsor[]> List(SponsorSearch model);
    string ResolveSponsorAvatarUri(string avatarFileName);
    Task<Sponsor> Retrieve(string id);
}

public class SponsorService : ISponsorService
{
    private readonly CoreOptions _coreOptions;
    private readonly Defaults _defaults;
    private readonly IMapper _mapper;
    private readonly IStore _store;
    private readonly IStore<Data.Sponsor> _sponsorStore;

    public SponsorService
    (
        CoreOptions coreOptions,
        Defaults defaults,
        IMapper mapper,
        IStore store,
        IStore<Data.Sponsor> sponsorStore
    )
    {
        _coreOptions = coreOptions;
        _defaults = defaults;
        _mapper = mapper;
        _sponsorStore = sponsorStore;
        _store = store;
    }

    public async Task AddOrUpdate(UpdateSponsorRequest model)
    {
        var entity = await _sponsorStore.Retrieve(model.Id);

        if (entity is not null)
        {
            _mapper.Map(model, entity);
            await _sponsorStore.Update(entity);
            return;
        }

        entity = _mapper.Map<Data.Sponsor>(model);
        await _sponsorStore.Create(entity);
    }

    public void DeleteLogoFileByName(string fileName)
    {
        string oldLogoPath = Path.Combine(_coreOptions.ImageFolder, fileName);
        if (File.Exists(oldLogoPath))
            File.Delete(oldLogoPath);
    }

    public IEnumerable<string> GetAllowedLogoMimeTypes()
    {
        return new string[]
        {
            MediaTypeNames.Image.Gif,
            MediaTypeNames.Image.Jpeg,
            MimeTypes.ImagePng,
            MimeTypes.ImageSvg,
            MimeTypes.ImageWebp
        };
    }

    public async Task<Data.Sponsor> GetDefaultSponsor()
    {
        if (_defaults.DefaultSponsor.IsNotEmpty())
        {
            var defaultSponsor = await _sponsorStore
                .List()
                .FirstOrDefaultAsync(s => s.Id == _defaults.DefaultSponsor);

            if (defaultSponsor is not null)
                return defaultSponsor;
        }

        var firstSponsor = await _sponsorStore
                .List()
                .FirstOrDefaultAsync();

        if (firstSponsor is not null)
            return firstSponsor;

        throw new CouldntResolveDefaultSponsor();
    }

    public async Task<Sponsor[]> List(SponsorSearch model)
    {
        var query = _store.WithNoTracking<Data.Sponsor>();

        if (model.HasParent.HasValue)
        {
            var hasParentBool = model.HasParent.Value;
            query = query.Where(s => (s.ParentSponsorId != null) == hasParentBool);
        }

        if (model.ExcludeSponsorId.IsNotEmpty())
        {
            query = query.Where(s => s.Id != model.ExcludeSponsorId);
        }

        query = query.OrderBy(s => s.Name);

        return await _mapper.ProjectTo<Sponsor>(query).ToArrayAsync();
    }

    public string ResolveSponsorAvatarUri(string avatarFileName)
        => Path.Combine(_coreOptions.ImageFolder, avatarFileName);

    public async Task<Sponsor> Retrieve(string id)
    {
        return _mapper.Map<Sponsor>(await _sponsorStore.Retrieve(id));
    }
}
