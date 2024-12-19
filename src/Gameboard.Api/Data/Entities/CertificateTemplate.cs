using System.Collections.Generic;

namespace Gameboard.Api.Data;

public class CertificateTemplate : IEntity
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }

    // navigations
    public Data.User CreatedByUser { get; set; }
    public string CreatedByUserId { get; set; }

    public ICollection<Data.Game> UseAsTemplateForGames { get; set; } = [];
    public ICollection<Data.Game> UseAsPracticeTemplateForGames { get; set; } = [];
    public PracticeModeSettings UsedAsPracticeModeDefault { get; set; }
}
