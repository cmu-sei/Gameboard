using Gameboard.Api.Data;

public class Report : IEntity
{
    public string Id { get; set; }
    public required string Key { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }

    // tried doing these as a json doc to acknowledge that they're descriptive/not true entities, but 
    // making that happen across MSSQL/PGSQL through EF got ugly quickly, so we're just pipe delimiting.
    public required string ExampleFields { get; set; }
    public required string ExampleParameters { get; set; }
}
