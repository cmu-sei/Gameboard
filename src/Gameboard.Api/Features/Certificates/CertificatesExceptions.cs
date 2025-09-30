// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Data;
using Gameboard.Api.Structure;

namespace Gameboard.Api.Features.Certificates;

public class CertificateIsntPublished : GameboardValidationException
{
    public CertificateIsntPublished(string ownerUserId, PublishedCertificateMode mode, string entityId)
        : base($"""There is no published certificate for user "{ownerUserId}" playing entity "{entityId}" in {mode.ToString()} mode.""") { }
}
