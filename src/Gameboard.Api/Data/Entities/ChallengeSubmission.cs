using System;
using System.Collections.Generic;

namespace Gameboard.Api.Data;

public class ChallengeSubmission : IEntity
{
    public string Id { get; set; }
    public DateTimeOffset CreatedOn { get; set; }
    public IEnumerable<ChallengeSubmissionAnswerData> AnswerData { get; set; }

    public string PendingSubmissionForChallengeId { get; set; }
    public Data.Challenge PendingSubmissionForChallenge { get; set; }

    public string SubmittedForChallengeId { get; set; }
    public Data.Challenge SubmittedForChallenge { get; set; }
}

public class ChallengeSubmissionAnswerData
{
    public int QuestionSetIndex { get; set; }
    public IEnumerable<string> Answers { get; set; }
}
