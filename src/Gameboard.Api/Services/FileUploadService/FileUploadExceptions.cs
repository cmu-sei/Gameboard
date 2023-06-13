using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Common;

internal class ProhibitedMimeTypeUploaded : GameboardValidationException
{
    public ProhibitedMimeTypeUploaded(string fileName, string mimeType, IEnumerable<string> allowedMimeTypes)
        : base($"""File "{fileName}" has an illegal content type ("{mimeType}"). Permitted content types are: {string.Join(",", allowedMimeTypes)} """) { }
}
