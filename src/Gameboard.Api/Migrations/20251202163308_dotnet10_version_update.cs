using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;

#nullable disable

namespace Gameboard.Api.Migrations
{
    /// <inheritdoc />
    public partial class dotnet10_version_update : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchivedChallenges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GameName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PlayerId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PlayerName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastScoreTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSyncTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HasGamespaceDeployed = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerMode = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<string>(type: "text", nullable: true),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Duration = table.Column<long>(type: "bigint", nullable: false),
                    Result = table.Column<int>(type: "integer", nullable: false),
                    Events = table.Column<string>(type: "text", nullable: true),
                    Submissions = table.Column<string>(type: "text", nullable: true),
                    TeamMembers = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedChallenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Extensions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    HostUrl = table.Column<string>(type: "text", nullable: false),
                    Token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Extensions", x => x.Id);
                    table.UniqueConstraint("AK_Extensions_Type", x => x.Type);
                });

            migrationBuilder.CreateTable(
                name: "ExternalGameHosts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ClientUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DestroyResourcesOnDeployFailure = table.Column<bool>(type: "boolean", nullable: false),
                    GamespaceDeployBatchSize = table.Column<int>(type: "integer", nullable: true),
                    HttpTimeoutInSeconds = table.Column<int>(type: "integer", nullable: true),
                    HostApiKey = table.Column<string>(type: "character varying(70)", maxLength: 70, nullable: true),
                    HostUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PingEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StartupEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TeamExtendedEndpoint = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalGameHosts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sponsors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Logo = table.Column<string>(type: "text", nullable: true),
                    Approved = table.Column<bool>(type: "boolean", nullable: false),
                    ParentSponsorId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sponsors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sponsors_Sponsors_ParentSponsorId",
                        column: x => x.ParentSponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Email = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NameStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ApprovedName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastIdpAssignedRole = table.Column<int>(type: "integer", nullable: true),
                    LoginCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    HasDefaultSponsor = table.Column<bool>(type: "boolean", nullable: false),
                    PlayAudioOnBrowserNotification = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SponsorId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    GeneratedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    ExpiresOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "NULL"),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    OwnerId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiKeys_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CertificateTemplate",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateTemplate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateTemplate_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    HelpText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackTemplates_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameExportBatches",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ExportedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExportedByUserId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameExportBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameExportBatches_Users_ExportedByUserId",
                        column: x => x.ExportedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PracticeChallengeGroups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TextSearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Id", "Description" }),
                    ParentGroupId = table.Column<string>(type: "text", nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeChallengeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroups_PracticeChallengeGroups_ParentGroup~",
                        column: x => x.ParentGroupId,
                        principalTable: "PracticeChallengeGroups",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroups_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroups_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupportSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    SupportPageGreeting = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedByUserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotifications",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MarkdownContent = table.Column<string>(type: "text", nullable: false),
                    StartsOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndsOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NotificationType = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    IsDismissible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedByUserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemNotifications_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeModeSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AttemptLimit = table.Column<int>(type: "integer", nullable: true),
                    DefaultPracticeSessionLengthMinutes = table.Column<int>(type: "integer", nullable: false),
                    IntroTextMarkdown = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    MaxConcurrentPracticeSessions = table.Column<int>(type: "integer", nullable: true),
                    MaxPracticeSessionLengthMinutes = table.Column<int>(type: "integer", nullable: true),
                    SuggestedSearches = table.Column<string>(type: "text", nullable: true),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CertificateTemplateId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeModeSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PracticeModeSettings_CertificateTemplate_CertificateTemplat~",
                        column: x => x.CertificateTemplateId,
                        principalTable: "CertificateTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PracticeModeSettings_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Competition = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Season = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Track = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Division = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Logo = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Sponsor = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Background = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    GameStart = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GameEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    GameMarkdown = table.Column<string>(type: "text", nullable: true),
                    FeedbackConfig = table.Column<string>(type: "text", nullable: true),
                    RegistrationMarkdown = table.Column<string>(type: "text", nullable: true),
                    RegistrationOpen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationClose = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RegistrationType = table.Column<int>(type: "integer", nullable: false),
                    RegistrationConstraint = table.Column<string>(type: "text", nullable: true),
                    MinTeamSize = table.Column<int>(type: "integer", nullable: false),
                    MaxTeamSize = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    RequireSponsoredTeam = table.Column<bool>(type: "boolean", nullable: false),
                    SessionMinutes = table.Column<int>(type: "integer", nullable: false),
                    SessionLimit = table.Column<int>(type: "integer", nullable: false),
                    SessionAvailabilityWarningThreshold = table.Column<int>(type: "integer", nullable: true),
                    GamespaceLimitPerSession = table.Column<int>(type: "integer", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    AllowLateStart = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPreview = table.Column<bool>(type: "boolean", nullable: false),
                    AllowPublicScoreboardAccess = table.Column<bool>(type: "boolean", nullable: false),
                    AllowReset = table.Column<bool>(type: "boolean", nullable: false),
                    CardText1 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CardText2 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    CardText3 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    ExternalHostId = table.Column<string>(type: "character varying(40)", nullable: true),
                    Mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PlayerMode = table.Column<int>(type: "integer", nullable: false),
                    RequireSynchronizedStart = table.Column<bool>(type: "boolean", nullable: false),
                    ShowOnHomePageInPracticeMode = table.Column<bool>(type: "boolean", nullable: false),
                    TextSearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Competition", "Id", "Track", "Season", "Division" }),
                    ChallengesFeedbackTemplateId = table.Column<string>(type: "text", nullable: true),
                    FeedbackTemplateId = table.Column<string>(type: "text", nullable: true),
                    CertificateTemplateId = table.Column<string>(type: "text", nullable: true),
                    PracticeCertificateTemplateId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_CertificateTemplate_CertificateTemplateId",
                        column: x => x.CertificateTemplateId,
                        principalTable: "CertificateTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Games_CertificateTemplate_PracticeCertificateTemplateId",
                        column: x => x.PracticeCertificateTemplateId,
                        principalTable: "CertificateTemplate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Games_ExternalGameHosts_ExternalHostId",
                        column: x => x.ExternalHostId,
                        principalTable: "ExternalGameHosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Games_FeedbackTemplates_ChallengesFeedbackTemplateId",
                        column: x => x.ChallengesFeedbackTemplateId,
                        principalTable: "FeedbackTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Games_FeedbackTemplates_FeedbackTemplateId",
                        column: x => x.FeedbackTemplateId,
                        principalTable: "FeedbackTemplates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "SupportSettingsAutoTags",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ConditionType = table.Column<int>(type: "integer", nullable: false),
                    ConditionValue = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SupportSettingsId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SupportSettingsAutoTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupportSettingsAutoTags_SupportSettings_SupportSettingsId",
                        column: x => x.SupportSettingsId,
                        principalTable: "SupportSettings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemNotificationInteractions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SawCalloutOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SawFullNotificationOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DismissedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SystemNotificationId = table.Column<string>(type: "character varying(40)", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemNotificationInteractions", x => x.Id);
                    table.UniqueConstraint("AK_SystemNotificationInteractions_SystemNotificationId_UserId", x => new { x.SystemNotificationId, x.UserId });
                    table.ForeignKey(
                        name: "FK_SystemNotificationInteractions_SystemNotifications_SystemNo~",
                        column: x => x.SystemNotificationId,
                        principalTable: "SystemNotifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SystemNotificationInteractions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeGates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TargetId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RequiredId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RequiredScore = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeGates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeGates_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeSpecs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Disabled = table.Column<bool>(type: "boolean", nullable: false),
                    AverageDeploySeconds = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    X = table.Column<float>(type: "real", nullable: false),
                    Y = table.Column<float>(type: "real", nullable: false),
                    R = table.Column<float>(type: "real", nullable: false),
                    GameEngineType = table.Column<int>(type: "integer", nullable: false),
                    SolutionGuideUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ShowSolutionGuideInCompetitiveMode = table.Column<bool>(type: "boolean", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    TextSearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: true)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "Name", "Id", "Description", "GameId", "Tag", "Tags", "Text" }),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeSpecs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeSpecs_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DenormalizedTeamScores",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamName = table.Column<string>(type: "text", nullable: true),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    ScoreOverall = table.Column<double>(type: "double precision", nullable: false),
                    ScoreAutoBonus = table.Column<double>(type: "double precision", nullable: false),
                    ScoreManualBonus = table.Column<double>(type: "double precision", nullable: false),
                    ScoreChallenge = table.Column<double>(type: "double precision", nullable: false),
                    ScoreAdvanced = table.Column<double>(type: "double precision", nullable: true),
                    SolveCountNone = table.Column<int>(type: "integer", nullable: false),
                    SolveCountPartial = table.Column<int>(type: "integer", nullable: false),
                    SolveCountComplete = table.Column<int>(type: "integer", nullable: false),
                    CumulativeTimeMs = table.Column<double>(type: "double precision", nullable: false),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DenormalizedTeamScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DenormalizedTeamScores_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExternalGameTeams",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExternalGameUrl = table.Column<string>(type: "text", nullable: true),
                    DeployStatus = table.Column<int>(type: "integer", nullable: false),
                    GameId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalGameTeams", x => x.Id);
                    table.UniqueConstraint("AK_ExternalGameTeams_TeamId_GameId", x => new { x.TeamId, x.GameId });
                    table.ForeignKey(
                        name: "FK_ExternalGameTeams_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GameGameExportBatch",
                columns: table => new
                {
                    ExportedInBatchesId = table.Column<string>(type: "text", nullable: false),
                    IncludedGamesId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameGameExportBatch", x => new { x.ExportedInBatchesId, x.IncludedGamesId });
                    table.ForeignKey(
                        name: "FK_GameGameExportBatch_GameExportBatches_ExportedInBatchesId",
                        column: x => x.ExportedInBatchesId,
                        principalTable: "GameExportBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GameGameExportBatch_Games_IncludedGamesId",
                        column: x => x.IncludedGamesId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Players",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ApprovedName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    NameStatus = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    InviteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IsReady = table.Column<bool>(type: "boolean", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    SessionBegin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SessionEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SessionMinutes = table.Column<double>(type: "double precision", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Time = table.Column<long>(type: "bigint", nullable: false),
                    CorrectCount = table.Column<int>(type: "integer", nullable: false),
                    PartialCount = table.Column<int>(type: "integer", nullable: false),
                    Advanced = table.Column<bool>(type: "boolean", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    WhenCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsLateStart = table.Column<bool>(type: "boolean", nullable: false),
                    AdvancedFromGameId = table.Column<string>(type: "character varying(40)", nullable: true),
                    AdvancedFromPlayerId = table.Column<string>(type: "character varying(40)", nullable: true),
                    AdvancedFromTeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AdvancedWithScore = table.Column<double>(type: "double precision", nullable: true),
                    SponsorId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Players", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Players_Games_AdvancedFromGameId",
                        column: x => x.AdvancedFromGameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Players_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Players_Players_AdvancedFromPlayerId",
                        column: x => x.AdvancedFromPlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Players_Sponsors_SponsorId",
                        column: x => x.SponsorId,
                        principalTable: "Sponsors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Players_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeBonuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PointValue = table.Column<double>(type: "double precision", nullable: false),
                    ChallengeBonusType = table.Column<int>(type: "integer", nullable: false),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", nullable: true),
                    SolveRank = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeBonuses_ChallengeSpecs_ChallengeSpecId",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AttachedEntityType = table.Column<int>(type: "integer", nullable: false),
                    WhenEdited = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WhenFinalized = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    WhenCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FeedbackTemplateId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", nullable: false),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissions_ChallengeSpecs_ChallengeSpecId",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissions_FeedbackTemplates_FeedbackTemplateId",
                        column: x => x.FeedbackTemplateId,
                        principalTable: "FeedbackTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissions_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PracticeChallengeGroupChallengeSpec",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PracticeChallengeGroupId = table.Column<string>(type: "text", nullable: false),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticeChallengeGroupChallengeSpec", x => x.Id);
                    table.UniqueConstraint("AK_PracticeChallengeGroupChallengeSpec_ChallengeSpecId_Practic~", x => new { x.ChallengeSpecId, x.PracticeChallengeGroupId });
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroupChallengeSpec_ChallengeSpecs_Challeng~",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PracticeChallengeGroupChallengeSpec_PracticeChallengeGroups~",
                        column: x => x.PracticeChallengeGroupId,
                        principalTable: "PracticeChallengeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PublishedCertificate",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    PublishedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Mode = table.Column<int>(type: "integer", nullable: false),
                    OwnerUserId = table.Column<string>(type: "character varying(40)", nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PublishedCertificate", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OwnerUserId_Users_Id",
                        column: x => x.OwnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PublishedCertificate_ChallengeSpecs_ChallengeSpecId",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PublishedCertificate_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Challenges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    GraderKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Points = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    PlayerMode = table.Column<int>(type: "integer", nullable: false),
                    LastScoreTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSyncTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WhenCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HasDeployedGamespace = table.Column<bool>(type: "boolean", nullable: false),
                    GameEngineType = table.Column<int>(type: "integer", nullable: false),
                    PendingSubmission = table.Column<string>(type: "text", nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PlayerId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    SpecId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Challenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Challenges_ChallengeSpecs_SpecId",
                        column: x => x.SpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Challenges_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Challenges_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeedbackSubmissionResponses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FeedbackSubmissionId = table.Column<string>(type: "text", nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: true),
                    Prompt = table.Column<string>(type: "text", nullable: true),
                    ShortName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeedbackSubmissionResponses", x => new { x.FeedbackSubmissionId, x.Id });
                    table.ForeignKey(
                        name: "FK_FeedbackSubmissionResponses_FeedbackSubmissions_FeedbackSub~",
                        column: x => x.FeedbackSubmissionId,
                        principalTable: "FeedbackSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AwardedChallengeBonuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    EnteredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    InternalSummary = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ChallengeBonusId = table.Column<string>(type: "character varying(40)", nullable: true),
                    ChallengeId = table.Column<string>(type: "character varying(40)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AwardedChallengeBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AwardedChallengeBonuses_ChallengeBonuses_ChallengeBonusId",
                        column: x => x.ChallengeBonusId,
                        principalTable: "ChallengeBonuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AwardedChallengeBonuses_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChallengeId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Text = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeEvents_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChallengeSubmissions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SubmittedOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.0),
                    Answers = table.Column<string>(type: "text", nullable: false),
                    ChallengeId = table.Column<string>(type: "character varying(40)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChallengeSubmissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChallengeSubmissions_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Feedback",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PlayerId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    GameId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChallengeId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChallengeSpecId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Answers = table.Column<string>(type: "text", nullable: true),
                    Submitted = table.Column<bool>(type: "boolean", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Feedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Feedback_ChallengeSpecs_ChallengeSpecId",
                        column: x => x.ChallengeSpecId,
                        principalTable: "ChallengeSpecs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedback_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedback_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedback_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Feedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ManualBonuses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    EnteredOn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    PointValue = table.Column<double>(type: "double precision", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    EnteredByUserId = table.Column<string>(type: "character varying(40)", nullable: true),
                    ChallengeId = table.Column<string>(type: "character varying(40)", nullable: true),
                    TeamId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ManualBonuses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ManualBonuses_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ManualBonuses_Users_EnteredByUserId",
                        column: x => x.EnteredByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Key = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    RequesterId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AssigneeId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreatorId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    ChallengeId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    PlayerId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    TeamId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Summary = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Label = table.Column<string>(type: "text", nullable: true),
                    StaffCreated = table.Column<bool>(type: "boolean", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Attachments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Challenges_ChallengeId",
                        column: x => x.ChallengeId,
                        principalTable: "Challenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Tickets_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tickets_Users_CreatorId",
                        column: x => x.CreatorId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Tickets_Users_RequesterId",
                        column: x => x.RequesterId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TicketActivity",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TicketId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    UserId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    AssigneeId = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    Message = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Attachments = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketActivity", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketActivity_Tickets_TicketId",
                        column: x => x.TicketId,
                        principalTable: "Tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TicketActivity_Users_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_TicketActivity_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OwnerId",
                table: "ApiKeys",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedChallenges_GameId",
                table: "ArchivedChallenges",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedChallenges_PlayerId",
                table: "ArchivedChallenges",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedChallenges_TeamId",
                table: "ArchivedChallenges",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchivedChallenges_UserId",
                table: "ArchivedChallenges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardedChallengeBonuses_ChallengeBonusId",
                table: "AwardedChallengeBonuses",
                column: "ChallengeBonusId");

            migrationBuilder.CreateIndex(
                name: "IX_AwardedChallengeBonuses_ChallengeId",
                table: "AwardedChallengeBonuses",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateTemplate_CreatedByUserId",
                table: "CertificateTemplate",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeBonuses_ChallengeSpecId",
                table: "ChallengeBonuses",
                column: "ChallengeSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeEvents_ChallengeId",
                table: "ChallengeEvents",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeGates_GameId",
                table: "ChallengeGates",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_GameId",
                table: "Challenges",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_PlayerId",
                table: "Challenges",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_SpecId",
                table: "Challenges",
                column: "SpecId");

            migrationBuilder.CreateIndex(
                name: "IX_Challenges_TeamId",
                table: "Challenges",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSpecs_GameId",
                table: "ChallengeSpecs",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSpecs_TextSearchVector",
                table: "ChallengeSpecs",
                column: "TextSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_ChallengeSubmissions_ChallengeId",
                table: "ChallengeSubmissions",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_DenormalizedTeamScores_GameId",
                table: "DenormalizedTeamScores",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalGameTeams_GameId",
                table: "ExternalGameTeams",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_ChallengeId",
                table: "Feedback",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_ChallengeSpecId",
                table: "Feedback",
                column: "ChallengeSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_GameId",
                table: "Feedback",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_PlayerId",
                table: "Feedback",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Feedback_UserId",
                table: "Feedback",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_ChallengeSpecId",
                table: "FeedbackSubmissions",
                column: "ChallengeSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_FeedbackTemplateId",
                table: "FeedbackSubmissions",
                column: "FeedbackTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_GameId",
                table: "FeedbackSubmissions",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackSubmissions_UserId",
                table: "FeedbackSubmissions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FeedbackTemplates_CreatedByUserId",
                table: "FeedbackTemplates",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameExportBatches_ExportedByUserId",
                table: "GameExportBatches",
                column: "ExportedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_GameGameExportBatch_IncludedGamesId",
                table: "GameGameExportBatch",
                column: "IncludedGamesId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_CertificateTemplateId",
                table: "Games",
                column: "CertificateTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_ChallengesFeedbackTemplateId",
                table: "Games",
                column: "ChallengesFeedbackTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_ExternalHostId",
                table: "Games",
                column: "ExternalHostId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_FeedbackTemplateId",
                table: "Games",
                column: "FeedbackTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_PracticeCertificateTemplateId",
                table: "Games",
                column: "PracticeCertificateTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_TextSearchVector",
                table: "Games",
                column: "TextSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_ManualBonuses_ChallengeId",
                table: "ManualBonuses",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_ManualBonuses_EnteredByUserId",
                table: "ManualBonuses",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_AdvancedFromGameId",
                table: "Players",
                column: "AdvancedFromGameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_AdvancedFromPlayerId",
                table: "Players",
                column: "AdvancedFromPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_GameId",
                table: "Players",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_Id_TeamId",
                table: "Players",
                columns: new[] { "Id", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_Players_SponsorId",
                table: "Players",
                column: "SponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_TeamId",
                table: "Players",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserId",
                table: "Players",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Players_UserId_TeamId",
                table: "Players",
                columns: new[] { "UserId", "TeamId" });

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroupChallengeSpec_PracticeChallengeGroupId",
                table: "PracticeChallengeGroupChallengeSpec",
                column: "PracticeChallengeGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroups_CreatedByUserId",
                table: "PracticeChallengeGroups",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroups_ParentGroupId",
                table: "PracticeChallengeGroups",
                column: "ParentGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroups_TextSearchVector",
                table: "PracticeChallengeGroups",
                column: "TextSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeChallengeGroups_UpdatedByUserId",
                table: "PracticeChallengeGroups",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PracticeModeSettings_CertificateTemplateId",
                table: "PracticeModeSettings",
                column: "CertificateTemplateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PracticeModeSettings_UpdatedByUserId",
                table: "PracticeModeSettings",
                column: "UpdatedByUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublishedCertificate_ChallengeSpecId",
                table: "PublishedCertificate",
                column: "ChallengeSpecId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedCertificate_GameId",
                table: "PublishedCertificate",
                column: "GameId");

            migrationBuilder.CreateIndex(
                name: "IX_PublishedCertificate_OwnerUserId",
                table: "PublishedCertificate",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sponsors_ParentSponsorId",
                table: "Sponsors",
                column: "ParentSponsorId");

            migrationBuilder.CreateIndex(
                name: "IX_SupportSettings_UpdatedByUserId",
                table: "SupportSettings",
                column: "UpdatedByUserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupportSettingsAutoTags_SupportSettingsId",
                table: "SupportSettingsAutoTags",
                column: "SupportSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotificationInteractions_UserId",
                table: "SystemNotificationInteractions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemNotifications_CreatedByUserId",
                table: "SystemNotifications",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketActivity_AssigneeId",
                table: "TicketActivity",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketActivity_TicketId",
                table: "TicketActivity",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketActivity_UserId",
                table: "TicketActivity",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_AssigneeId",
                table: "Tickets",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ChallengeId",
                table: "Tickets",
                column: "ChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreatorId",
                table: "Tickets",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Key",
                table: "Tickets",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_PlayerId",
                table: "Tickets",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_RequesterId",
                table: "Tickets",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_SponsorId",
                table: "Users",
                column: "SponsorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "ArchivedChallenges");

            migrationBuilder.DropTable(
                name: "AwardedChallengeBonuses");

            migrationBuilder.DropTable(
                name: "ChallengeEvents");

            migrationBuilder.DropTable(
                name: "ChallengeGates");

            migrationBuilder.DropTable(
                name: "ChallengeSubmissions");

            migrationBuilder.DropTable(
                name: "DenormalizedTeamScores");

            migrationBuilder.DropTable(
                name: "Extensions");

            migrationBuilder.DropTable(
                name: "ExternalGameTeams");

            migrationBuilder.DropTable(
                name: "Feedback");

            migrationBuilder.DropTable(
                name: "FeedbackSubmissionResponses");

            migrationBuilder.DropTable(
                name: "GameGameExportBatch");

            migrationBuilder.DropTable(
                name: "ManualBonuses");

            migrationBuilder.DropTable(
                name: "PracticeChallengeGroupChallengeSpec");

            migrationBuilder.DropTable(
                name: "PracticeModeSettings");

            migrationBuilder.DropTable(
                name: "PublishedCertificate");

            migrationBuilder.DropTable(
                name: "SupportSettingsAutoTags");

            migrationBuilder.DropTable(
                name: "SystemNotificationInteractions");

            migrationBuilder.DropTable(
                name: "TicketActivity");

            migrationBuilder.DropTable(
                name: "ChallengeBonuses");

            migrationBuilder.DropTable(
                name: "FeedbackSubmissions");

            migrationBuilder.DropTable(
                name: "GameExportBatches");

            migrationBuilder.DropTable(
                name: "PracticeChallengeGroups");

            migrationBuilder.DropTable(
                name: "SupportSettings");

            migrationBuilder.DropTable(
                name: "SystemNotifications");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Challenges");

            migrationBuilder.DropTable(
                name: "ChallengeSpecs");

            migrationBuilder.DropTable(
                name: "Players");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "CertificateTemplate");

            migrationBuilder.DropTable(
                name: "ExternalGameHosts");

            migrationBuilder.DropTable(
                name: "FeedbackTemplates");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Sponsors");
        }
    }
}
