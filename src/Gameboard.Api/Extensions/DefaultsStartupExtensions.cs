// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.IO;
using Gameboard.Api;

namespace Microsoft.Extensions.DependencyInjection
{
    // Startup extension for setting up defaults. May be expanded in future to involve the database.
    public static class DefaultsStartupExtensions
    {
        public static IServiceCollection AddDefaults(
            this IServiceCollection services,
            Defaults defaults,
            string contentRootPath
        ) {
            services.AddSingleton<Defaults>(_ => {
                // if no filename specified, check for presence of 'feedback-template.yaml'
                var filename = defaults.FeedbackTemplateFile.NotEmpty() ? defaults.FeedbackTemplateFile : "feedback-template.yaml";
                var file = Path.Combine(contentRootPath, filename);
                string template = null;
                if (File.Exists(file))
                    template = File.ReadAllText(file);
                return new Defaults {
                    FeedbackTemplate = template,
                    FeedbackTemplateFile = defaults.FeedbackTemplateFile
                };
            });
            
            return services;
        }
    }
}
