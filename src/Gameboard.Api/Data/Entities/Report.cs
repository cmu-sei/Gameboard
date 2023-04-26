using Gameboard.Api.Data;

public class Report : IEntity
{
    public required string Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string ExampleFields { get; set; }
    public required string ExampleParameters { get; set; }
}
