using System;
using System.Text;
using Gameboard.Api.Structure;

namespace Gameboard.Api;

public static class ExceptionExtensions
{
    public static string ToTypeName(string exceptionCode)
        => Encoding.UTF8.GetString(Convert.FromBase64String(exceptionCode));

    public static string ToExceptionCode<T>(this T exception) where T : GameboardValidationException
        => ToExceptionCode(typeof(T));

    public static string ToExceptionCode(this Type t)
        => ToExceptionCode(t.Name);

    private static string ToExceptionCode(string typeName)
        => Convert.ToBase64String(Encoding.UTF8.GetBytes(typeName));
}
