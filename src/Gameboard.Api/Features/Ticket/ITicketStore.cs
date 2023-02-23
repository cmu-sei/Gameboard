// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;

namespace Gameboard.Api.Data.Abstractions
{

    public interface ITicketStore : IStore<Ticket>
    {
        Task<Data.Ticket> Load(string id);
        Task<Data.Ticket> Load(int id);
        Task<Data.Ticket> Load(Api.Ticket model);
        Task<Data.Ticket> LoadDetails(string id);
        Task<Data.Ticket> LoadDetails(int id);
    }
}
