using System;

namespace Gameboard.Api.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class DontBindForDIAttribute : Attribute
{
}
