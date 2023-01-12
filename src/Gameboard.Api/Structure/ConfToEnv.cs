using System;
using System.IO;

namespace Gameboard.Api.Structure;

internal static class ConfToEnv
{
    public static void Load(string confFileName)
    {
        if (!File.Exists(confFileName))
            return;

        try
        {
            foreach (string line in File.ReadAllLines(confFileName))
            {
                if (
                    line.Equals(string.Empty)
                    || line.Trim().StartsWith("#")
                    || !line.Contains("=")
                )
                {
                    continue;
                }

                int x = line.IndexOf("=");

                Environment.SetEnvironmentVariable(
                    line.Substring(0, x).Trim(),
                    line.Substring(x + 1).Trim()
                );
            }
        }
        catch { }
    }
}
