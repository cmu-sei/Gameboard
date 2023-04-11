using System;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class StartEndDateValidator<TModel> : IGameboardValidator<TModel>
{
    public DateTimeOffset EndDate { get; private set; }
    public bool EndDateRequired { get; private set; } = false;
    public DateTimeOffset StartDate { get; private set; }
    public bool StartDateRequired { get; private set; } = false;

    public Func<TModel, DateTimeOffset> StartDateProperty { get; set; }
    public Func<TModel, DateTimeOffset> EndDateProperty { get; set; }

    private StartEndDateValidator() { }

    public static StartEndDateValidator<TModel> Configure(Action<StartEndDateValidator<TModel>> configAction)
    {
        var validator = new StartEndDateValidator<TModel>();
        configAction(validator);

        if (validator.StartDateProperty == null)
            throw new InvalidOperationException($"{nameof(StartEndDateValidator<TModel>)} can't validate without a {nameof(StartEndDateValidator<TModel>.StartDateProperty)} specified.");

        if (validator.EndDateProperty == null)
            throw new InvalidOperationException($"{nameof(StartEndDateValidator<TModel>)} can't validate without a {nameof(StartEndDateValidator<TModel>.EndDateProperty)} specified.");

        return validator;
    }

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return (model, context) =>
        {
            var startDateValue = StartDateProperty(model);
            var endDateValue = EndDateProperty(model);

            if (StartDateRequired && startDateValue == DateTimeOffset.MinValue)
                return Task.FromResult<GameboardValidationException>(new MissingRequiredDate("StartDate"));

            if (EndDateRequired && endDateValue == DateTimeOffset.MinValue)
                return Task.FromResult<GameboardValidationException>(new MissingRequiredDate("EndDate"));

            if (startDateValue > DateTimeOffset.MinValue && endDateValue > DateTimeOffset.MinValue && startDateValue > endDateValue)
                return Task.FromResult<GameboardValidationException>(new StartDateOccursAfterEndDate(startDateValue, endDateValue));

            return Task.FromResult<GameboardValidationException>(null);
        };
    }
}
