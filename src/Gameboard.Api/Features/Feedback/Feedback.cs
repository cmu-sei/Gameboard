// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace Gameboard.Api
{
     public class FeedbackSubmission
    {
        public string ChallengeId { get; set; }
        public string ChallengeSpecId { get; set; }
        public string GameId { get; set; }
        public bool Submit { get; set; }
        public FeedbackQuestion[] Questions { get; set; }

    }

    public class FeedbackQuestion : QuestionTemplate
    {
        public string Answer { get; set; }
    }

    public class QuestionTemplate
    {
        public string Id {get; set; }
        public string Type {get; set; }
        public string[] Options {get; set; }
        public string Prompt { get; set; }
        public string ShortName { get; set; }
    }

    public class BoardFeedbackTemplate
    {
        public QuestionTemplate[] Board {get; set; }
        public QuestionTemplate[] Challenge {get; set; }
    }
    

    public class Feedback
    {
        public string Id { get; set; } 
        public string UserId { get; set; } 
        public string PlayerId { get; set; } 
        public string ChallengeId { get; set; } 
        public string ChallengeSpecId { get; set; } 
        public string GameId { get; set; }
        public FeedbackQuestion[] Questions { get; set; } 
        public bool Submitted { get; set; } 
        public DateTimeOffset Timestamp { get; set; }


    }

    public class FeedbackReportDetails : Feedback
    {
        public string ApprovedName { get; set; }
        public string ChallengeTag { get; set; }
    }

     public class FeedbackSearchParams: SearchFilter
    {
        public string GameId { get; set; } 
        public string ChallengeSpecId { get; set; } 
        public string ChallengeId { get; set; } 
        public string Type { get; set; } 
    }

}
