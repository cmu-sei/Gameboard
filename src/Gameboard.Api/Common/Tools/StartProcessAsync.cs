// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Gameboard.Api.Common;

public static class StartProcessAsync
{
    public static Task<int> StartAsync(string command, string[] args = null)
    {
        var tcs = new TaskCompletionSource<int>();
        var startInfo = new ProcessStartInfo { FileName = command };

        if (args is not null)
            foreach (var arg in args) { startInfo.ArgumentList.Add(arg); }

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.Exited += (sender, args) =>
        {
            tcs.SetResult(process.ExitCode);
            process.Dispose();
        };

        process.Start();
        return tcs.Task;
    }
}
