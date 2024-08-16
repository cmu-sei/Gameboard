using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Admin;

public record SendAnnouncementCommand(string Title, string ContentMarkdown, string TeamId = null) : IRequest;

internal class SendAnnouncementHandler(
    IActingUserService actingUserService,
    IUserHubBus userHubBus,
    IValidatorService<SendAnnouncementCommand> validator
    ) : IRequestHandler<SendAnnouncementCommand>
{
    private readonly User _actingUser = actingUserService.Get();
    private readonly IUserHubBus _userHubBus = userHubBus;
    private readonly IValidatorService<SendAnnouncementCommand> _validator = validator;

    public async Task Handle(SendAnnouncementCommand request, CancellationToken cancellationToken)
    {
        // auth/validate
        await _validator
            .ConfigureAuthorization(config => config.RequirePermissions(UserRolePermissionKey.Admin_SendAnnouncements))
            .AddValidator((req, ctx) =>
            {
                if (req.ContentMarkdown.IsEmpty())
                    ctx.AddValidationException(new MissingRequiredInput<SendAnnouncementCommand>(nameof(req.ContentMarkdown), req));
            })
            .Validate(request, cancellationToken);

        await _userHubBus.SendAnnouncement(new UserHubAnnouncementEvent
        {
            ContentMarkdown = request.ContentMarkdown,
            Title = request.Title,
            SentByUser = new SimpleEntity { Id = _actingUser.Id, Name = _actingUser.ApprovedName },
            TeamId = request.TeamId
        });
    }
}
