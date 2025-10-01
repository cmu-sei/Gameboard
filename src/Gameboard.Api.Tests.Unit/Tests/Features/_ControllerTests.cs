// Copyright 2025 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Gameboard.Api.Controllers;
using Gameboard.Api.Data;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Gameboard.Api.Tests.Unit;

// _Controller is inherited by every (legacy) api controller in GameboardApi. To access some of its implementation for testing, we subclass it here.

// public class ControllerTests
// {
//     private ControllerTestable GetControllerTestable(User? withActor = null)
//     {
//         var controllerTestable = new ControllerTestable(
//             A.Fake<ILogger>(),
//             A.Fake<IDistributedCache>(),
//             withActor ?? new User { RolePermissions = [] },
//             []
//         );

//         return controllerTestable;
//     }

//     [Fact]
//     public void AuthorizeAll_WhenReqFalse_ThrowsActionForbidden()
//     {
//         // arrange
//         var sut = GetControllerTestable();
//         var authReqs = new[]
//         {
//             () => false
//         };

//         // act/assert
//         Should.Throw<ActionForbidden>(() => sut.AuthorizeAnyTestable(authReqs));
//     }

//     [Fact]
//     public void AuthorizeAll_WhenReqsTrueAndFalse_ThrowsActionForbidden()
//     {
//         // arrange
//         var sut = GetControllerTestable();
//         var authReqs = new[]
//         {
//             () => false,
//             () => true
//         };

//         // act/assert
//         Should.Throw<ActionForbidden>(() => sut.AuthorizeAllTestable(authReqs));
//     }

//     [Fact]
//     public void AuthorizeAll_WhenAdmin_EvaluatesRequirements()
//     {
//         // arrange
//         var fakeAdmin = A.Fake<User>();
//         fakeAdmin.Role = UserRole.Admin;

//         var sut = GetControllerTestable(fakeAdmin);
//         var authRequirements = new Func<Boolean>[]
//         {
//             () => false
//         };

//         // act/assert
//         Should.Throw<ActionForbidden>(() => sut.AuthorizeAllTestable(authRequirements));
//     }

//     [Fact]
//     public void AuthorizeAny_WhenAdmin_IgnoresOtherRequirements()
//     {
//         // arrange
//         var fakeAdmin = A.Fake<User>();
//         fakeAdmin.Role = UserRole.Admin;
//         var sut = GetControllerTestable(fakeAdmin);

//         var authorizationRequirements = new Func<Boolean>[]
//         {
//             () => false
//         };

//         // act/assert
//         Should.NotThrow(() => sut.AuthorizeAnyTestable(authorizationRequirements));
//     }

//     [Fact]
//     public void AuthorizeAny_WhenReqFalse_ThrowsActionForbidden()
//     {
//         // arrange
//         var sut = GetControllerTestable();
//         var authReqs = new[]
//         {
//             () => false
//         };

//         // act/assert
//         Should.Throw<ActionForbidden>(() => sut.AuthorizeAnyTestable(authReqs));
//     }

//     [Fact]
//     public void AuthorizeAny_WhenOneReqTrue_Succedes()
//     {
//         // arrange
//         var sut = GetControllerTestable();
//         var authReqs = new[]
//         {
//             () => false,
//             () => true
//         };

//         // act/assert
//         Should.NotThrow(() => sut.AuthorizeAnyTestable(authReqs));
//     }
// }
