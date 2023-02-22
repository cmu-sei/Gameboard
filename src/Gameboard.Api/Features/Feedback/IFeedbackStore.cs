// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System.Threading.Tasks;

namespace Gameboard.Api.Data.Abstractions
{

    public interface IFeedbackStore : IStore<Feedback>
    {
        Task<Data.Feedback> Load(Feedback model);
    }
}
