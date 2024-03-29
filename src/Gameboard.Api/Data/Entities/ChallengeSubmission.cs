using System;

namespace Gameboard.Api.Data;

public class ChallengeSubmission : IEntity
{
    public string Id { get; set; }
    public DateTimeOffset SubmittedOn { get; set; }
    public double Score { get; set; }

    /// <summary>
    /// A JSON string (represnting the ChallengeSubmissionAnswers model) of
    /// submitted answers. See JSONEntities.cs for an explanation.
    /// </summary>
    public string Answers { get; set; }

    public string ChallengeId { get; set; }
    public Data.Challenge Challenge { get; set; }
}

