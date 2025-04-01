using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Data;
using Gameboard.Api.Structure.MediatR;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Features.Games.Requests;

public record CloneGameCommand(string GameId, string Name) : IRequest<Game>;

internal sealed class CloneGameHandler
(
    IGuidService guids,
    IMapper mapper,
    IStore store,
    IValidatorService validatorService
) : IRequestHandler<CloneGameCommand, Game>
{
    public async Task<Game> Handle(CloneGameCommand request, CancellationToken cancellationToken)
    {
        await validatorService
            .Auth(c => c.Require(Users.PermissionKey.Games_CreateEditDelete))
            .AddEntityExistsValidator<Data.Game>(request.GameId)
            .Validate(cancellationToken);

        var game = await store
            .WithNoTracking<Data.Game>()
            .Include(g => g.Prerequisites)
            .Include(g => g.Specs)
            .SingleAsync(g => g.Id == request.GameId, cancellationToken);

        await store.DoTransaction(async db =>
        {
            // assign new id and name
            game.Id = guids.Generate();
            game.Name = request.Name;

            // attach the related entities (so we don't signal to EF that we want to create them)
            db.AttachRange(game.Prerequisites);
            db.AttachRange(game.Specs);

            db.Add(game);
            await db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return mapper.Map<Game>(game);
    }
}
