using System;

namespace Gameboard.Api.Features.Player;

public class PlayerUpdatedViewModel
{
    public string Id { get; set; }
    public string ApprovedName { get; set; }
    public string PreUpdateName { get; set; }
    public string Name { get; set; }
    public string NameStatus { get; set; }
}