using System;

namespace Gameboard.Api.Data;

public class SupportSettings : IEntity
{
    public string Id { get; set; }
    public string AutoTagPracticeTicketsWith { get; set; }
    public string SupportPageGreeting { get; set; }
    public DateTimeOffset UpdatedOn { get; set; }

    public string UpdatedByUserId;
    public Data.User UpdatedByUser;
}
