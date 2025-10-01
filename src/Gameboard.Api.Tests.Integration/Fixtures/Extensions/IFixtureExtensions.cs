// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api.Tests.Integration.Fixtures;

internal static class IFixtureExtensions
{
    internal static string CreateStringWithLength(this IFixture fixture, int requestedLength)
    {
        string output = "";

        while (output.Length < requestedLength)
        {
            output += fixture.Create<string>();
        }

        return output.Substring(0, requestedLength);
    }
}
