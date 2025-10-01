// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;

namespace Gameboard.Api.Features.Extensions;

[DIIgnore]
public interface IExtensionService
{
    Task NotifyScored(ExtensionMessage message);
    Task NotifyTicketCreated(ExtensionMessage message);
}
