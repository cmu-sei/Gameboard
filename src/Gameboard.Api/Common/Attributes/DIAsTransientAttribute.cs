using System;

namespace Gameboard.Api.Common;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public sealed class DIAsTransientAttribute : Attribute { }
