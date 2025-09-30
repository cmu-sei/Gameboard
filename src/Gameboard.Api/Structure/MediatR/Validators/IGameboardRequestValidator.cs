// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR;

public interface IGameboardRequestValidator<T>
{
    Task Validate(T request, CancellationToken cancellationToken);
}
