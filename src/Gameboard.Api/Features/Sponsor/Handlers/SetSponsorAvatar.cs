// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Data;
using Gameboard.Api.Services;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Sponsors;

public record SetSponsorAvatarCommand(string SponsorId, IFormFile AvatarFile, User ActingUser) : IRequest<string>;

internal class SetSponsorAvatarHandler(
    CoreOptions coreOptions,
    ContentTypeValidator<SetSponsorAvatarCommand> legalAvatarType,
    EntityExistsValidator<SetSponsorAvatarCommand, Data.Sponsor> sponsorExists,
    SponsorService sponsorService,
    IStore store,
    IValidatorService<SetSponsorAvatarCommand> validatorService
    ) : IRequestHandler<SetSponsorAvatarCommand, string>
{
    private readonly CoreOptions _coreOptions = coreOptions;
    private readonly ContentTypeValidator<SetSponsorAvatarCommand> _legalAvatarType = legalAvatarType;
    private readonly EntityExistsValidator<SetSponsorAvatarCommand, Data.Sponsor> _sponsorExists = sponsorExists;
    private readonly SponsorService _sponsorService = sponsorService;
    private readonly IStore _store = store;
    private readonly IValidatorService<SetSponsorAvatarCommand> _validatorService = validatorService;

    public async Task<string> Handle(SetSponsorAvatarCommand request, CancellationToken cancellationToken)
    {
        _validatorService.Auth(c => c.Require(Users.PermissionKey.Sponsors_CreateEdit));
        _validatorService.AddValidator(_sponsorExists.UseProperty(r => r.SponsorId));
        _validatorService.AddValidator
        (
            _legalAvatarType
                .HasPermittedTypes(_sponsorService.GetAllowedLogoMimeTypes())
                .UseProperty(r => r.AvatarFile)
        );
        await _validatorService.Validate(request, cancellationToken);

        // load the sponsor - we'll need to update its logo file name when we're done
        var sponsor = await _store
            .WithNoTracking<Data.Sponsor>()
            .SingleAsync(s => s.Id == request.SponsorId, cancellationToken);

        // record the sponsor's previous logo, if any, so we can delete it if this succeeds
        var previousAvatarFileName = sponsor.Logo;
        var avatarFileName = string.Empty;
        var uploadedAvatarFullPath = string.Empty;

        if (request.AvatarFile is not null)
        {
            // upload the new file
            avatarFileName = request.AvatarFile.FileName.ToLower();
            uploadedAvatarFullPath = Path.Combine(_coreOptions.ImageFolder, avatarFileName);

            using var stream = new FileStream(uploadedAvatarFullPath, FileMode.Create);
            await request.AvatarFile.CopyToAsync(stream, cancellationToken);

            // update the sponsor
            await _store
                .WithNoTracking<Data.Sponsor>()
                .Where(s => s.Id == request.SponsorId)
                .ExecuteUpdateAsync(s => s.SetProperty(s => s.Logo, avatarFileName), cancellationToken);
        }

        // delete the old file if it exists (this happens even if there's no avatar being uploaded, since
        // this operation also allows the end user to clear the avatar)
        if (previousAvatarFileName.NotEmpty() && previousAvatarFileName != avatarFileName)
        {
            var previousAvatarFullPath = _sponsorService.ResolveSponsorAvatarUri(previousAvatarFileName);
            if (File.Exists(previousAvatarFullPath))
                File.Delete(previousAvatarFullPath);
        }

        return avatarFileName;
    }
}
