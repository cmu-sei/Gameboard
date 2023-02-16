// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api
{

    [Flags]
    public enum UserRole
    {
        Member =    0,
        Observer =  0b1,
        Tester =    0b10,
        Designer =  0b100,
        Registrar = 0b1000,
        Director =  0b10000,
        Admin =     0b100000,
        Support =   0b1000000
    }

    public enum PlayerRole
    {
        Member,
        Manager
    }

    public enum PlayerMode
    {
        Competition,
        Practice
    }

    public enum GameRegistrationType
    {
        None,
        Open,
        Domain
    }

    public enum ChallengeEventType
    {
        Started,
        GamespaceOn,
        GamespaceOff,
        Submission
    }

    public enum ChallengeResult
    {
        None,
        Partial,
        Success
    }

    public enum ActivityType
    {
        Comment,
        StatusChange,
        AssigneeChange
    }

}
