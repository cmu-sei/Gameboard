// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using System;

namespace Gameboard.Api
{
    public static class AppConstants
    {
        public const string SubjectClaimName = "sub";
        public const string NameClaimName = "name";
        public const string ApprovedNameClaimName = "verified_name";
        public const string SponsorClaimName = "sponsor";
        public const string RoleClaimName = "role";
        public const string RoleListClaimName = "rolelist";
        public const string RegistrarPolicy = "RegistrarPolicy";
        public const string DesignerPolicy = "DesignerPolicy";
        public const string AdminPolicy = "AdminPolicy";
        public const string ConsolePolicy = "ConsolePolicy";
        public const string HubPolicy = "HubPolicy";
        public const string TicketOnlyPolicy = "TicketOnlyPolicy";
        public const string GraderPolicy = "GraderPolicy";
        public const string DataProtectionPurpose = "_dp:Gameboard";
        public const string MksCookie = "gameboard.mks";
        public const string ImageMapType = "map";
        public const string ImageCardType = "card";
        public const string NameStatusPending = "pending";
        public const string NameStatusNotUnique = "not_unique";
        public const string InternalSupportChannel = "internal_support";
        public const string ApiKeyAuthPolicy = "ApiKeyAuthPolicy";

        public static DateTimeOffset NULL_DATE = DateTimeOffset.MinValue;

        public const UserRole AllRoles =
            UserRole.Admin |
            UserRole.Director |
            UserRole.Registrar |
            UserRole.Designer |
            UserRole.Tester |
            UserRole.Observer |
            UserRole.Support
        ;
    }
}
