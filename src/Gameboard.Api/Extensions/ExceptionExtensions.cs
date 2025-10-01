// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Text;
using Gameboard.Api.Structure;

namespace Gameboard.Api;

public static class ExceptionExtensions
{
    public static string ToExceptionCode<T>(this T exception) where T : GameboardValidationException
        => ToExceptionCode(typeof(T));

    public static string ToExceptionCode(this Type t)
        => ToExceptionCode(t.Name);

    private static string ToExceptionCode(string typeName)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(typeName));
}
