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
        )
        {
            services.AddSingleton<Defaults>(_ =>
            {
                // if no filename specified, check for presence of 'feedback-template.yaml'
                var feedbackFilename = defaults.FeedbackTemplateFile.NotEmpty() ? defaults.FeedbackTemplateFile : "feedback-template.yaml";
                var feedbackFile = Path.Combine(contentRootPath, feedbackFilename);
                string feedbackTemplate = null;
                if (File.Exists(feedbackFile))
                    feedbackTemplate = File.ReadAllText(feedbackFile);

                var certificateFilename = defaults.CertificateTemplateFile.NotEmpty() ? defaults.CertificateTemplateFile : "certificate-template.html";
                var certificateFile = Path.Combine(contentRootPath, certificateFilename);
                string certificateTemplate = null;
                if (File.Exists(certificateFile))
                    certificateTemplate = File.ReadAllText(certificateFile);

                var shiftTimezone = defaults.ShiftTimezone.NotEmpty() ? defaults.ShiftTimezone : Defaults.ShiftTimezoneFallback;
                var shiftStrings = defaults.ShiftStrings != null ? defaults.ShiftStrings : Defaults.ShiftStringsFallback;
                var shifts = defaults.ShiftStrings != null ? new System.DateTimeOffset[shiftStrings.Length][] : Defaults.ShiftsFallback;
                if (defaults.ShiftStrings != null)
                {
                    for (int i = 0; i < shiftStrings.Length; i++)
                    {
                        shifts[i] = new System.DateTimeOffset[] { Defaults.ConvertTime(shiftStrings[i][0], shiftTimezone), Defaults.ConvertTime(shiftStrings[i][1], shiftTimezone) };
                    }
                }

                return new Defaults
                {
                    DefaultSponsor = defaults.DefaultSponsor,
                    FeedbackTemplate = feedbackTemplate,
                    FeedbackTemplateFile = defaults.FeedbackTemplateFile,
                    CertificateTemplate = certificateTemplate,
                    CertificateTemplateFile = defaults.CertificateTemplateFile,
                    ShiftStrings = shiftStrings,
                    Shifts = shifts,
                    ShiftTimezone = shiftTimezone
                };
            });

            return services;
        }
    }
}
