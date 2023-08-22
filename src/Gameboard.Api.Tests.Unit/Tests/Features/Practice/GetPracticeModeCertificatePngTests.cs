using Gameboard.Api.Data.Abstractions;
using Gameboard.Api.Features.Practice;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;
using Microsoft.EntityFrameworkCore.Query;

namespace Gameboard.Api.Tests.Unit;

public class GetPracticeModeCertificateHtmlTests
{
    private readonly GetPracticeModeCertificatePngHandler _sut;

    public GetPracticeModeCertificateHtmlTests()
    {
        _sut = new GetPracticeModeCertificatePngHandler
        (
            A.Fake<EntityExistsValidator<GetPracticeModeCertificatePngQuery, Data.User>>(options =>
            {
                options.WithArgumentsForConstructor(() => new EntityExistsValidator<GetPracticeModeCertificatePngQuery, Data.User>(A.Fake<IStore<Data.User>>()));
            }),
            A.Fake<EntityExistsValidator<GetPracticeModeCertificatePngQuery, Data.User>>(options =>
            {
                options.WithArgumentsForConstructor(() => new EntityExistsValidator<GetPracticeModeCertificatePngQuery, Data.User>(A.Fake<IStore<Data.User>>()));
            }),
            A.Fake<CoreOptions>(),
            A.Fake<IPracticeService>(),
            A.Fake<IValidatorService<GetPracticeModeCertificatePngQuery>>()
        );
    }

    [Fact]
    public void GetDurationDescription_WithHoursAndMinutes_ResolvesExpected()
    {
        // given
        var duration = TimeSpan.FromMinutes(62);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("1 hour and 2 minutes");
    }

    [Fact]
    public void GetDurationDescription_WithHours_HidesMinutes()
    {
        // given
        var duration = TimeSpan.FromHours(2);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("2 hours");
    }

    [Fact]
    public void GetDurationDescription_WithMinutes_HidesHours()
    {
        // given
        var duration = TimeSpan.FromMinutes(39);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("39 minutes");
    }

    [Fact]
    public void GetDurationDescription_WithHms_HidesSeconds()
    {
        // given
        // 1 hour, 3 minutes, 4 seconds
        var duration = TimeSpan.FromSeconds(3787);

        // when
        var result = _sut.GetDurationDescription(duration);

        // then
        result.ToLower().ShouldBe("1 hour and 3 minutes");
    }
}
