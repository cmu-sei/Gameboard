// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Gameboard.Api
{
    public static class StringExtensions
    {
        public static string ToHash(this string str)
        {
            return BitConverter.ToString(
                SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(str))
            ).Replace("-", "").ToLower();
        }

        public static string[] AsHashTag(this string str)
        {
            var chars = str.ToLower().ToCharArray()
                .Where(c => char.IsLetterOrDigit(c) || c == ' ')
                .ToArray();

            return new string(chars)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        public static string ToSha256(this string input)
        {
            using (SHA256 alg = SHA256.Create())
            {
                return BitConverter.ToString(alg
                    .ComputeHash(Encoding.UTF8.GetBytes(input)))
                    .Replace("-", "")
                    .ToLower();
            }
        }

        public static bool NotEmpty(this DateTimeOffset ts)
        {
            return ts.Year > 1;
        }
        public static bool Empty(this DateTimeOffset ts)
        {
            return ts.Year == 1;
        }

        public static string Tag(this string s)
        {
            if (s.HasValue())
            {
                int x = s.IndexOf("#");
                if (x >= 0)
                    return s.Substring(x+1).Split(' ').First();
            }
            return "";
        }

        //strips hashtag+ from string
        public static string Untagged(this string s)
        {
            if (s.HasValue())
            {
                int x = s.IndexOf("#");
                if (x >= 0)
                    return s.Substring(0, x);
            }
            return s;
        }

        public static bool HasValue(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }
        public static bool IsEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
        public static bool NotEmpty(this string str)
        {
            return str.IsEmpty().Equals(false);
        }

        public static string Sanitize(this string target, char[] exclude)
        {
            string p = "";

            foreach (char c in target.ToCharArray())
                if (!exclude.Contains(c))
                    p += c;

            return p.Replace(" ", "_");
        }

        public static string SanitizeFilename(this string target)
        {
            return target.Sanitize(Path.GetInvalidFileNameChars());
        }

        public static string SanitizePath(this string target)
        {
            return target.Sanitize(Path.GetInvalidPathChars());
        }
    }
}
