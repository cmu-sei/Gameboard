// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;
using Gameboard.Api.Data;

namespace Gameboard.Api.Validators;

public class TicketValidator : IModelValidator
{
    private readonly ITicketStore _store;

    public TicketValidator(ITicketStore store)
    {
        _store = store;
    }

    public Task Validate(object model)
    {
        if (model is Entity)
            return _validate(model as Entity);

        if (model is Ticket)
            return _validate(model as Ticket);

        if (model is ChangedTicket)
            return _validate(model as ChangedTicket);

        if (model is NewTicket)
            return _validate(model as NewTicket);

        if (model is NewTicketComment)
            return _validate(model as NewTicketComment);


        throw new System.NotImplementedException();
    }

    private async Task _validate(Entity model)
    {
        if ((await _store.Exists(model.Id)).Equals(false))
            throw new ResourceNotFound<Ticket>(model.Id);

        await Task.CompletedTask;
    }

    private async Task _validate(NewTicket model)
    {
        // TODO validate that references exist and belong to the requester
        // see feedback validator for examples
        // for example, challenge exists and it is part of a player session that the user belongs to

        await Task.CompletedTask;
    }

    private async Task _validate(Ticket model)
    {
        if ((await _store.Exists(model.Id)).Equals(false))
            throw new ResourceNotFound<Ticket>(model.Id);

        await Task.CompletedTask;
    }

    private async Task _validate(ChangedTicket model)
    {
        if ((await _store.Exists(model.Id)).Equals(false))
            throw new ResourceNotFound<Ticket>(model.Id);

        // TODO validate that references exist and belong to the requester

        await Task.CompletedTask;
    }

    private async Task _validate(NewTicketComment model)
    {
        if ((await _store.Exists(model.TicketId)).Equals(false))
            throw new ResourceNotFound<Ticket>(model.TicketId);

        await Task.CompletedTask;
    }
}
