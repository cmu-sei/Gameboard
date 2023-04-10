using System;

namespace Gameboard.Api.Structure.Validators;

internal class MissingRequiredDate : GameboardValidationException
{
    public MissingRequiredDate(string propertyName) : base($"The date property {propertyName} is required.") { }
}

internal class StartDateOccursAfterEndDate : GameboardValidationException
{
    public StartDateOccursAfterEndDate(DateTimeOffset start, DateTimeOffset end) : base($"Invalid start/end date values supplied. Start date {start} occurs after End date {end}.") { }
}
