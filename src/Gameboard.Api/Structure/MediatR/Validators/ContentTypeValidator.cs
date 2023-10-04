using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gameboard.Api.Common;
using Microsoft.AspNetCore.Http;

namespace Gameboard.Api.Structure.MediatR.Validators;

public class ContentTypeValidator<TModel> : IGameboardValidator<TModel>
    where TModel : class
{
    private IEnumerable<string> _allowedMimeTypes;
    private Func<TModel, IFormFile> _fileProperty;

    public Func<TModel, RequestValidationContext, Task> GetValidationTask()
    {
        return (model, context) =>
        {
            var file = _fileProperty.Invoke(model);
            if (_allowedMimeTypes != null && !_allowedMimeTypes.Any(t => t == file.ContentType))
                context.AddValidationException(new ProhibitedMimeTypeUploaded(file.Name, file.ContentType, _allowedMimeTypes));

            return Task.CompletedTask;
        };
    }

    public ContentTypeValidator<TModel> HasPermittedTypes(IEnumerable<string> types)
    {
        _allowedMimeTypes = types;
        return this;
    }

    public ContentTypeValidator<TModel> UseProperty(Func<TModel, IFormFile> fileProperty)
    {
        _fileProperty = fileProperty;
        return this;
    }
}
