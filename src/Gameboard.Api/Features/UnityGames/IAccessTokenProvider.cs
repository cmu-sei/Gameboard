using System.Threading.Tasks;

namespace Gameboard.Api.Features.UnityGames;

internal interface IAccessTokenProvider
{
    Task<string> GetToken();
}
