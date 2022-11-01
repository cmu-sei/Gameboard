// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Threading.Tasks;
using Gameboard.Api.Validators;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Controllers
{
    public class _Controller : ControllerBase, IActionFilter
    {
        public _Controller(
            ILogger logger,
            IDistributedCache cache,
            params IModelValidator[] validators
        )
        {
            Logger = logger;
            Cache = cache;
            _validators = validators;
        }

        protected User Actor { get; set; }
        protected ILogger Logger { get; private set; }
        protected IDistributedCache Cache { get; private set; }
        private IModelValidator[] _validators;

        public void OnActionExecuting(ActionExecutingContext context)
        {
            Actor = User.ToActor();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        /// <summary>
        /// Validate a model against all validators registered
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        protected async Task Validate(object model)
        {
            foreach (var v in _validators)
                await v.Validate(model);
        }

        /// <summary>
        /// Authorize if all requirements are met
        /// </summary>
        /// <param name="requirements"></param>
        protected void AuthorizeAll(params Func<Boolean>[] requirements)
        {
            bool valid = true;

            foreach (var requirement in requirements)
                valid &= requirement.Invoke();

            if (valid.Equals(false))
                throw new ActionForbidden();
        }

        /// <summary>
        /// Authorized if any requirement is met
        /// </summary>
        /// <param name="requirements"></param>
        protected void AuthorizeAny(params Func<Boolean>[] requirements)
        {
            if (Actor.IsAdmin)
                return;

            bool valid = false;

            foreach (var requirement in requirements)
            {
                valid |= requirement.Invoke();
                if (valid) break;
            }

            if (valid.Equals(false))
                throw new ActionForbidden();
        }

    }

    public enum AuthRequirement
    {
        None,
        Self,
        RegistrarOrSelf,
        DirectorOrSelf
    }
}
