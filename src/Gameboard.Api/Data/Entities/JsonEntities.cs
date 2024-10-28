using System.Collections.Generic;

namespace Gameboard.Api.Data;

// This basic type is currently used in the schema in two places. One instance per
// challenge is serialized as the "PendingSubmission" column of the "Challenges" table
// (to retain one set of unsubmitted answers per question set). Also, each record
// in the ChallengeSubmissions entity has an array of these in the Answers column, 
// where we track each set of answers submitted for a challenge.
// 
// WHY THIS ISN'T HANDLED WITH A JSON COLUMN?
// The ideal way to store JSON in a database, of course, is to use the provider's
// implementation of JSON columns. We couldn't do this at the time of implementation
// because the API surfaces for MSSQL and Postgres were not equivalent (pre .net 8.0).
// Even doing this per-provider wasn't easily manageable (you can easily mark a JSON
// column "required" in Postgres' EF provider, but not so easily in MSSQL).
//
// Postgres has since standardized their implementation to be seamless with the standard
// EF API surface for JSON columns, but we needed features that relied on the JSON
// data before we were able to upgrade to .NET Core 8, so we just used strings
// and hope to reconcile the schema later.
public class ChallengeSubmissionAnswers
{
    public int QuestionSetIndex { get; set; }
    public IEnumerable<string> Answers { get; set; }
}
