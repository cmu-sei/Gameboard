using System;
using System.Collections.Generic;
using System.Linq;

namespace Gameboard.Api.Structure;

public class GameboardAuthorizationRules
{
    private readonly User _actor;
    public readonly IList<Func<bool>> _rules;

    internal GameboardAuthorizationRules(User actor, IList<Func<bool>> rules)
    {
        _actor = actor;
        _rules = rules;
    }

    public void AddAllowedRoles(params UserRole[] roles)
    {
        _rules.Append(() => roles.Contains(_actor.Role));
    }

    public bool Evaluate()
    {
        if (_actor.IsAdmin)
            return true;

        foreach (var rule in _rules)
        {
            if (!rule.Invoke())
                return false;
        }

        return true;
    }
}
