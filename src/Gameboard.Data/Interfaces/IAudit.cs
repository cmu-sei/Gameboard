// Copyright 2020 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;

namespace Gameboard.Data
{
    /// <summary>
    /// interface for wiring created and updated dates automatically by the dbcontext
    /// </summary>
    interface IAudit
    {
        DateTime Created { get; set; }

        DateTime? Updated { get; set; }
    }
}

