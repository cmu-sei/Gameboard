namespace Gameboard.Api.Data;

public class SupportSettingsAutoTag : IEntity
{
    public string Id { get; set; }
    public required SupportSettingsAutoTagConditionType ConditionType { get; set; }
    public required string ConditionValue { get; set; }
    public required bool IsEnabled { get; set; }
    public required string Tag { get; set; }

    // navigation/modelfixup
    public string SupportSettingsId { get; set; }
    public SupportSettings SupportSettings { get; set; }
}
