using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Challenges;
using Gameboard.Api.Features.Users;
using Gameboard.Api.Structure.MediatR;
using MediatR;

namespace Gameboard.Api.Features.Reports;

public record GetChallengesReportSubmissionsExport(string ChallengeSpecId, ChallengesReportParameters Parameters) : IRequest<object[]>;

internal sealed class GetChallengesReportSubmissionsHandler
(
    IChallengesReportService challengesReport,
    IValidatorService validatorService
) : IRequestHandler<GetChallengesReportSubmissionsExport, object[]>
{
    private readonly IChallengesReportService _challengesReport = challengesReport;
    private readonly IValidatorService _validator = validatorService;
    public async Task<object[]> Handle(GetChallengesReportSubmissionsExport request, CancellationToken cancellationToken)
    {
        await _validator
            .Auth(config => config.Require(PermissionKey.Reports_View))
            .Validate(cancellationToken);

        var results = await _challengesReport.GetSubmissionsCsv(request.ChallengeSpecId, request.Parameters, cancellationToken);
        var records = new List<dynamic>();

        // we need to know the maximum number of answers in a submission to make a tabular CSV
        var maxAnswerCount = results.Length > 0 ? results.Select(r => r.SubmittedAnswers.Answers.Count()).Max() : 0;

        foreach (var result in results)
        {
            var record = result.ToDynamic() as IDictionary<string, object>;
            var answers = result.SubmittedAnswers.Answers.ToArray();

            record["SectionIndex"] = result.SubmittedAnswers.QuestionSetIndex;
            for (var i = 0; i < maxAnswerCount; i++)
            {
                var answerColumnName = $"AnswerQ{i + 1}";

                if (i < answers.Length)
                {
                    record[answerColumnName] = answers[i];
                }
                else
                {
                    record[answerColumnName] = null;
                }
            }

            records.Add(record);
        }

        return [.. records];
    }
}
