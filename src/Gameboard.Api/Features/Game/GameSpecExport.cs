// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

namespace Gameboard.Api
{
    public class GameSpecImport
    {
        public string Data { get; set; }
    }

    public class GameSpecExport
    {
        public string Id { get; set; }
        public ExportFormat Format { get; set; }
        public int GenerateSpecCount { get; set; }
    }

    public enum ExportFormat
    {
        Yaml,
        Json
    }
}
