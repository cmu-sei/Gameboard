// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Tests.Shared;

public abstract class GbAutomatedTestException : Exception
{
    public GbAutomatedTestException(string message) : base(message) { }
}

public class GbAutomatedTestSetupException : GbAutomatedTestException
{
    public GbAutomatedTestSetupException(string message) : base(message) { }
}
