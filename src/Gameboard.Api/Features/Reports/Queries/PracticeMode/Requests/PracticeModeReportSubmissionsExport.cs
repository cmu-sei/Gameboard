using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gameboard.Api.Features.Reports;
using MediatR;

namespace Gameboard.Api.Reports;

// this returns an object array because, regrettably, submission entries have a dynamic number of "answer" fields, so we pull back a strongly-typed
// object from the app logic, but then we convert to untyped here to accommodate for arbitray numbers of fields.
public record PracticeModeReportSubmissionsExportQuery(string ChallengeSpecId, PracticeModeReportParameters Parameters) : IRequest<object[]>;

internal sealed class PracticeModeReportSubmissionsExportHandler(IPracticeModeReportService practiceMode) : IRequestHandler<PracticeModeReportSubmissionsExportQuery, object[]>
{
    private readonly IPracticeModeReportService _practiceMode = practiceMode;

    public async Task<object[]> Handle(PracticeModeReportSubmissionsExportQuery request, CancellationToken cancellationToken)
    {
        var results = await _practiceMode.GetSubmissionsCsv(request.ChallengeSpecId, request.Parameters, cancellationToken);
        var records = new List<dynamic>();

        // we need to know the maximum number of answers in a submission to make a tabular CSV
        var maxAnswerCount = results.Select(r => r.SubmittedAnswers.Answers.Count()).Max();

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
