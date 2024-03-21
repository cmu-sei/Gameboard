using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Common.Services;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Authorizers;
using MediatR;

namespace Gameboard.Api.Features.Admin;

public record SendAnnouncementCommand(string Title, string ContentMarkdown, string TeamId = null) : IRequest;

internal class SendAnnouncementHandler : IRequestHandler<SendAnnouncementCommand>
{
    private readonly User _actingUser;
    private readonly IUserHubBus _userHubBus;
    private readonly UserRoleAuthorizer _userRoleAuthorizer;
    private readonly IValidatorService<SendAnnouncementCommand> _validator;

    public SendAnnouncementHandler
    (
        IActingUserService actingUserService,
        IUserHubBus userHubBus,
        UserRoleAuthorizer userRoleAuthorizer,
        IValidatorService<SendAnnouncementCommand> validator
    )
    {
        _actingUser = actingUserService.Get();
        _userHubBus = userHubBus;
        _userRoleAuthorizer = userRoleAuthorizer;
        _validator = validator;
    }

    public async Task Handle(SendAnnouncementCommand request, CancellationToken cancellationToken)
    {
        // auth/validate
        _userRoleAuthorizer
            .AllowRoles(UserRole.Admin, UserRole.Director)
            .Authorize();

        _validator.AddValidator((req, ctx) =>
        {
            if (req.ContentMarkdown.IsEmpty())
                ctx.AddValidationException(new MissingRequiredInput<SendAnnouncementCommand>(nameof(req.ContentMarkdown), req));
        });


        await _userHubBus.SendAnnouncement(new UserHubAnnouncementEvent
        {
            ContentMarkdown = request.ContentMarkdown,
            Title = request.Title,
            SentByUser = new SimpleEntity { Id = _actingUser.Id, Name = _actingUser.ApprovedName },
            TeamId = request.TeamId
        });
    }
}
