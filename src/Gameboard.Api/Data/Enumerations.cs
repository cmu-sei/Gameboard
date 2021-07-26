// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api
{

    [Flags]
    public enum UserRole
    {
        Member =    0,
        Observer =  0b00000001,
        Tester =    0b00000010,
        Designer =  0b00000100,
        Registrar = 0b00001000,
        Director =  0b00010000,
        Admin =     0b00100000
    }

    public enum PlayerRole
    {
        Member,
        Manager
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
}
