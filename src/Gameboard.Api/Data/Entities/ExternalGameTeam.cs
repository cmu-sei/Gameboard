namespace Gameboard.Api.Data;

public enum ExternalGameDeployStatus
{
    NotStarted = 1,
    Deploying = 2,
    Deployed = 3,
}

/// <summary>
/// Holds metadata about each team which participates in a game
/// with Engine Mode set to "External". 
/// 
/// The two primary pieces of useful info are the game deploy status
/// (which tells clients where to send users depending on the deploy
/// status of the)
/// </summary>
public class ExternalGameTeam : IEntity
{
    public string Id { get; set; }
    public string TeamId { get; set; }
    public string ExternalGameUrl { get; set; }
    public ExternalGameDeployStatus DeployStatus { get; set; }

    // nav properties
    public string GameId { get; set; }
    public Game Game { get; set; }
}
