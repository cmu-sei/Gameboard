using System;
using System.Collections.Generic;

namespace Gameboard.Api.Data;

public class ChallengeSubmission : IEntity
{
    public string Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public ChallengeQuestionSetAnswerData Answers { get; set; }

    public string ChallengeId { get; set; }
    public Data.Challenge Challenge { get; set; }
}

public class ChallengeQuestionSetAnswers
{
    public int QuestionSetIndex { get; set; }
    public IEnumerable<string> Answers { get; set; }
}

// this basic type is currently used in the schema in two places. It's
// serialized as the "PendingSubmission" column of the "Challenges" table
// (to retain one set of unsubmitted answers per question set). It's also
// the primary column of interest in ChallengeSubmissions, where we track
// each set of answers submitted for a challenge.
//
// We have to record this as a separate entity, even though it only holds,
// functionally, an array, because the postgres EF provider can't serialize
// pure arrays as entities in 7.0.x. Things changed wildly in 8.0.x, but we're
// not there yet.
public class ChallengeQuestionSetAnswerData
{
    public IEnumerable<ChallengeQuestionSetAnswers> Data { get; set; }
}
