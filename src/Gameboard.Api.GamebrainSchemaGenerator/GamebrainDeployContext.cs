using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Gameboard.Api.GamebrainSchemaGenerator;

public interface IGamebrainDeployContextSimpleEntity
{
    [Required] string Id { get; set; }
    [Required] string Name { get; set; }
}

public interface IGamebrainDeployContextSession
{
    [Required] DateTimeOffset Start { get; set; }
    [Required] DateTimeOffset End { get; set; }
}

public interface IGamebrainDeployContextGamespace
{
    [Required] string Id { get; set; }
    [Required] IEnumerable<string> VmUris { get; set; }
}

public interface IGamebrainDeployContextTeam
{
    [Required] string Id { get; set; }
    [Required] string Name { get; set; }
    [Required] IGamebrainDeployContextSession Session { get; set; }
    [Required] IEnumerable<IGamebrainDeployContextGamespace> Gamespaces { get; set; }
}

public interface IGamebrainDeployContext
{
    [Required] IGamebrainDeployContextSimpleEntity Game { get; set; }
    [Required] IEnumerable<IGamebrainDeployContextTeam> Teams { get; set; }
}
