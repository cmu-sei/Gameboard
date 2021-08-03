// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api
{
    public interface INameService
    {
        string GetRandomName();
        void PopulateList(List<string> list);
        void AppendList(List<string> list);
    }

    public class NameService: INameService
    {
        private readonly ILogger<NameService> _logger;
        private readonly CoreOptions _options;
        private Random _random;
        private List<string> _list;

        public NameService(
            ILogger<NameService> logger,
            CoreOptions options
        )
        {
            _logger = logger;
            _options = options;
            _random = new Random();
            _list = new List<string>();
        }

        public string GetRandomName()
        {
            if (_list.Count == 0)
                InitList();

            if (_list.Count == 0)
                return $"player_{_random.Next().ToString("x")}";

            int i = _random.Next(0, _list.Count);

            return _list[i];
        }

        public void PopulateList(List<string> list)
        {
            _list = list;
        }

        public void AppendList(List<string> list)
        {
            _list.AddRange(list);
        }

        private void InitList()
        {
            if (File.Exists(_options.SafeNamesFile))
            {
                try
                {
                    var tmp = JsonSerializer.Deserialize<string[]>(
                        File.ReadAllText(_options.SafeNamesFile)
                    );
                    _list = tmp.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load SafeNamesFile");
                }
            }
        }
    }
}
