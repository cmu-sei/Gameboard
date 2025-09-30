// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class DIAsTransientAttribute : Attribute { }
