using System;
using System.Threading.Tasks;

namespace Gameboard.Api.Structure.MediatR.Validators;

internal class StartEndDateValidator<TModel> : IGameboardValidator<TModel>
{
    public DateTimeOffset? EndDate { get; private set; }
    public bool EndDateRequired { get; private set; } = false;
    public DateTimeOffset? StartDate { get; private set; }
    public bool StartDateRequired { get; private set; } = false;

    public Func<TModel, DateTimeOffset?> StartDateProperty { get; set; }
    public Func<TModel, DateTimeOffset?> EndDateProperty { get; set; }

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

            if (StartDateRequired && (startDateValue == null || startDateValue.Value.DoesntHaveValue()))
                return Task.FromResult<GameboardValidationException>(new MissingRequiredDate(nameof(StartDate)));

            if (EndDateRequired && (endDateValue == null || endDateValue.Value.DoesntHaveValue()))
                return Task.FromResult<GameboardValidationException>(new MissingRequiredDate(nameof(EndDate)));

            if (startDateValue != null && startDateValue.Value.HasValue() && endDateValue != null && endDateValue.Value.HasValue() && startDateValue > endDateValue)
                return Task.FromResult<GameboardValidationException>(new StartDateOccursAfterEndDate(startDateValue.Value, endDateValue.Value));

            return Task.FromResult<GameboardValidationException>(null);
        };
    }
}
