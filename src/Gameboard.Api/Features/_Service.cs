// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Services
{
    public abstract class _Service
    {
        public _Service(
            ILogger logger,
            IMapper mapper,
            CoreOptions options
        )
        {
            Logger = logger;
            Options = options;
            Mapper = mapper;

            JsonOptions = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
            };
            JsonOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            );
        }

        protected IMapper Mapper { get; }
        protected ILogger Logger { get; }
        protected CoreOptions Options { get; }
        protected JsonSerializerOptions JsonOptions { get; }
    }
}
