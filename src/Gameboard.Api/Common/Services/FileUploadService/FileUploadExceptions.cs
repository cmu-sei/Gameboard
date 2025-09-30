// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Collections.Generic;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Common;

internal class ProhibitedMimeTypeUploaded : GameboardValidationException
{
    public ProhibitedMimeTypeUploaded(string fileName, string mimeType, IEnumerable<string> allowedMimeTypes)
        : base($"""File "{fileName}" has an illegal content type ("{mimeType}"). Permitted content types are: {string.Join(",", allowedMimeTypes)} """) { }
}
