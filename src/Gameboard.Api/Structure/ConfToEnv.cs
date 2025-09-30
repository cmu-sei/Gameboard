// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;

namespace Gameboard.Api.Structure;

internal static class ConfToEnv
{
    public static void Load(string confFileName)
    {
        var finalPath = Path.Combine(Directory.GetCurrentDirectory(), confFileName);

        if (!File.Exists(finalPath))
            return;

        try
        {
            foreach (string line in File.ReadAllLines(finalPath))
            {
                if (
                    line.Equals(string.Empty)
                    || line.Trim().StartsWith("#")
                    || !line.Contains('=')
                )
                {
                    continue;
                }

                int x = line.IndexOf('=');

                Environment.SetEnvironmentVariable(
                    line[..x].Trim(),
                    line[(x + 1)..].Trim()
                );
            }
        }
        catch { }
    }
}
