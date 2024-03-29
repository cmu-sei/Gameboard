using System.Net;
using System.Net.Http;
using Gameboard.Api.Common;

namespace Gameboard.Api.Features.Games.External;

internal class GamebrainException : GameboardException
{
    public GamebrainException(HttpMethod method, string endpoint, HttpStatusCode statusCode, string error) : base($"Gamebrain threw a {statusCode} in response to a {method} request to {endpoint}. Error detail: '{error}'") { }
}

internal class GamebrainEmptyResponseException : GameboardException
{
    public GamebrainEmptyResponseException(HttpMethod method, string url) : base($"Gamebrain didn't respond to a {method} request to {url}.") { }
}
