// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Structure.MediatR;
using Gameboard.Api.Structure.MediatR.Validators;

namespace Gameboard.Api.Features.Reports;

internal class EnrollmentReportValidator : IGameboardRequestValidator<EnrollmentReportParameters>
{
    private readonly IValidatorService<EnrollmentReportParameters> _validatorService;

    public EnrollmentReportValidator(IValidatorService<EnrollmentReportParameters> validatorService)
    {
        _validatorService = validatorService;
    }

    public async Task Validate(EnrollmentReportParameters request, CancellationToken cancellationToken)
    {
        var startEndDateValidator = StartEndDateValidator<EnrollmentReportParameters>.Configure(opt =>
        {
            opt.StartDateProperty = p => p.EnrollDateStart;
            opt.EndDateProperty = p => p.EnrollDateEnd;
        });

        _validatorService.AddValidator(startEndDateValidator);

        await _validatorService.Validate(request, cancellationToken);
    }
}
