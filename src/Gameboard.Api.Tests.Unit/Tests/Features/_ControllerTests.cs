using Gameboard.Api;
using Gameboard.Api.Controllers;
using Gameboard.Api.Validators;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Tests.Unit;

// _Controller is inherited by every api controller in GameboardApi. To access some of its implementation for testing, we subclass it here.

public class ControllerTestable : _Controller
{
    public ControllerTestable(ILogger logger, IDistributedCache cache, params IModelValidator[] validators)
        : base(logger, cache, validators) { }

    public void AuthorizeAllTestable(Func<Boolean>[] requirements)
    {
        base.AuthorizeAll(requirements);
    }

    public void AuthorizeAnyTestable(Func<Boolean>[] requirements)
    {
        base.AuthorizeAny(requirements);
    }

    internal void SetActor(User user)
    {
        base.Actor = user;
    }
}

public class ControllerTests
{
    private ControllerTestable GetControllerTestable(User? withActor = null)
    {
        var controllerTestable = new ControllerTestable(
                    A.Fake<ILogger>(),
                    A.Fake<IDistributedCache>(),
                    Array.Empty<IModelValidator>()
                );

        if (withActor != null)
        {
            controllerTestable.SetActor(withActor);
        }
        else
        {
            controllerTestable.SetActor(A.Fake<User>());
        }

        return controllerTestable;
    }

    [Fact]
    public void AuthorizeAll_WhenReqFalse_ThrowsActionForbidden()
    {
        // arrange
        var sut = GetControllerTestable();
        var authReqs = new[]
        {
            () => false
        };

        // act/assert
        Should.Throw<ActionForbidden>(() => sut.AuthorizeAnyTestable(authReqs));
    }

    [Fact]
    public void AuthorizeAll_WhenReqsTrueAndFalse_ThrowsActionForbidden()
    {
        // arrange
        var sut = GetControllerTestable();
        var authReqs = new[]
        {
            () => false,
            () => true
        };

        // act/assert
        Should.Throw<ActionForbidden>(() => sut.AuthorizeAllTestable(authReqs));
    }

    [Fact]
    public void AuthorizeAll_WhenAdmin_EvaluatesRequirements()
    {
        // arrange
        var fakeAdmin = A.Fake<User>();
        fakeAdmin.Role = UserRole.Admin;

        var sut = GetControllerTestable(fakeAdmin);
        var authRequirements = new Func<Boolean>[]
        {
            () => false
        };

        // act/assert
        Should.Throw<ActionForbidden>(() => sut.AuthorizeAllTestable(authRequirements));
    }

    [Fact]
    public void AuthorizeAny_WhenAdmin_IgnoresOtherRequirements()
    {
        // arrange
        var fakeAdmin = A.Fake<User>();
        fakeAdmin.Role = UserRole.Admin;
        var sut = GetControllerTestable(fakeAdmin);

        var authorizationRequirements = new Func<Boolean>[]
        {
            () => false
        };

        // act/assert
        Should.NotThrow(() => sut.AuthorizeAnyTestable(authorizationRequirements));
    }

    [Fact]
    public void AuthorizeAny_WhenReqFalse_ThrowsActionForbidden()
    {
        // arrange
        var sut = GetControllerTestable();
        var authReqs = new[]
        {
            () => false
        };

        // act/assert
        Should.Throw<ActionForbidden>(() => sut.AuthorizeAnyTestable(authReqs));
    }

    [Fact]
    public void AuthorizeAny_WhenOneReqTrue_Succedes()
    {
        // arrange
        var sut = GetControllerTestable();
        var authReqs = new[]
        {
            () => false,
            () => true
        };

        // act/assert
        Should.NotThrow(() => sut.AuthorizeAnyTestable(authReqs));
    }
}
