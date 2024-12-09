// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data;

public class GameboardDbContext(DbContextOptions options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApiKey>(k =>
        {
            k.Property(k => k.Id).HasMaxLength(40);
            k.Property(k => k.Name).HasMaxLength(50);
            k.Property(k => k.Key).HasMaxLength(100);

            k.Property(k => k.ExpiresOn)
                .HasDefaultValueSql("NULL")
                .ValueGeneratedOnAdd();

            k.Property(k => k.GeneratedOn)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            k.HasOne(k => k.Owner)
                .WithMany(u => u.ApiKeys)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Challenge>(b =>
        {
            b.HasOne(p => p.Player).WithMany(u => u.Challenges).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(p => p.Game).WithMany(u => u.Challenges).OnDelete(DeleteBehavior.SetNull);
            b.HasIndex(p => p.TeamId);
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(u => u.SpecId).HasMaxLength(40);
            b.Property(u => u.ExternalId).HasMaxLength(40);
            b.Property(u => u.PlayerId).HasMaxLength(40);
            b.Property(u => u.TeamId).HasMaxLength(40);
            b.Property(u => u.GameId).HasMaxLength(40);
            b.Property(u => u.GraderKey).HasMaxLength(64);
        });

        builder.Entity<ChallengeBonus>(b =>
        {
            b
                .HasDiscriminator(b => b.ChallengeBonusType)
                .HasValue<ChallengeBonusCompleteSolveRank>(ChallengeBonusType.CompleteSolveRank);

            b.Property(b => b.Id).HasStandardGuidLength();
            b.Property(b => b.Description).HasStandardNameLength();
            b.HasOne(b => b.ChallengeSpec).WithMany(c => c.Bonuses).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChallengeBonusCompleteSolveRank>();

        builder.Entity<AwardedChallengeBonus>(b =>
        {
            b.Property(a => a.Id).HasStandardGuidLength();
            b.Property(a => a.InternalSummary).HasMaxLength(200);
            b.Property(a => a.EnteredOn)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();

            b.HasOne(a => a.ChallengeBonus).WithMany(c => c.AwardedTo).OnDelete(DeleteBehavior.Restrict);
            b.HasOne(a => a.Challenge).WithMany(c => c.AwardedBonuses).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ManualBonus>(b =>
        {
            b
                .HasDiscriminator(b => b.Type)
                .HasValue<ManualChallengeBonus>(ManualBonusType.Challenge)
                .HasValue<ManualTeamBonus>(ManualBonusType.Manual);

            b.Property(b => b.Id).HasStandardGuidLength();
            b.Property(b => b.Description).HasMaxLength(200);
            b.Property(b => b.EnteredOn)
                .HasDefaultValueSql("NOW()")
                .ValueGeneratedOnAdd();
            b.HasOne(m => m.EnteredByUser).WithMany(u => u.EnteredManualBonuses).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ManualChallengeBonus>(b =>
        {
            b
                .HasOne(m => m.Challenge)
                .WithMany(c => c.AwardedManualBonuses).OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

        });

        builder.Entity<ManualTeamBonus>(b =>
        {
            b
                .Property(m => m.TeamId)
                .IsRequired();
        });

        builder.Entity<ChallengeEvent>(b =>
        {
            b.HasOne(p => p.Challenge).WithMany(u => u.Events).OnDelete(DeleteBehavior.Cascade);
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(u => u.ChallengeId).HasMaxLength(40);
            b.Property(u => u.UserId).HasMaxLength(40);
            b.Property(u => u.TeamId).HasMaxLength(40);
            b.Property(u => u.Text).HasMaxLength(1024);
        });

        builder.Entity<ChallengeGate>(b =>
        {
            b.HasOne(p => p.Game).WithMany(u => u.Prerequisites).OnDelete(DeleteBehavior.Cascade);
            b.Property(g => g.Id).HasMaxLength(40);
            b.Property(g => g.TargetId).HasMaxLength(40);
            b.Property(g => g.RequiredId).HasMaxLength(40);
            b.Property(g => g.GameId).HasMaxLength(40);
        });

        builder.Entity<ChallengeSpec>(b =>
        {
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(u => u.GameId).HasMaxLength(40);
            b.Property(u => u.ExternalId).HasMaxLength(40);
            b.Property(u => u.SolutionGuideUrl).HasMaxLength(1000);

            b.HasOne(p => p.Game).WithMany(u => u.Specs).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ChallengeSubmission>(b =>
        {
            b.Property(s => s.SubmittedOn).IsRequired();
            b.Property(s => s.Answers).IsRequired();
            b.Property(s => s.Score).IsRequired().HasDefaultValue(0);

            b.HasOne(s => s.Challenge)
                .WithMany(c => c.Submissions)
                .IsRequired();
        });

        builder.Entity<DenormalizedTeamScore>(b =>
        {
            b.Property(d => d.Id).HasStandardGuidLength();
            b.Property(d => d.GameId)
                .HasStandardGuidLength()
                .IsRequired();
            b.Property(d => d.TeamId)
                .HasStandardGuidLength()
                .IsRequired();

            b.HasOne(d => d.Game)
                .WithMany(g => g.DenormalizedTeamScores)
                .IsRequired();
        });

        builder.Entity<Extension>(b =>
        {
            b.HasAlternateKey(e => e.Type);
            b.Property(e => e.Id).HasStandardGuidLength();
            b.Property(e => e.Name).HasStandardNameLength().IsRequired();
            b.Property(e => e.Token).HasMaxLength(256).IsRequired();
            b.Property(e => e.HostUrl).IsRequired();
        });

        builder.Entity<ExternalGameHost>(b =>
        {
            b.Property(c => c.Id).HasStandardGuidLength();
            b.Property(c => c.Name).HasStandardNameLength().IsRequired();
            b.Property(c => c.ClientUrl).HasStandardUrlLength();
            b.Property(c => c.HostApiKey).HasMaxLength(70);
            b.Property(c => c.HostUrl).HasStandardUrlLength().IsRequired();
            b.Property(c => c.PingEndpoint).HasStandardUrlLength();
            b.Property(c => c.StartupEndpoint).HasStandardUrlLength().IsRequired();
            b.Property(c => c.TeamExtendedEndpoint).HasStandardUrlLength();

            b.HasMany(c => c.UsedByGames)
                .WithOne(g => g.ExternalHost)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<ExternalGameTeam>(b =>
        {
            b.Property(s => s.Id).HasStandardGuidLength();
            b.Property(s => s.TeamId)
                .HasStandardGuidLength()
                .IsRequired();

            b.HasAlternateKey(s => new { s.TeamId, s.GameId });
            b.HasOne(b => b.Game)
                .WithMany(g => g.ExternalGameTeams)
                .IsRequired();
        });

        builder.Entity<Feedback>(b =>
        {
            b.HasOne(p => p.User).WithMany(u => u.Feedback).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(p => p.Player).WithMany(u => u.Feedback).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(p => p.Game).WithMany(u => u.Feedback).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(p => p.Challenge).WithMany(u => u.Feedback).OnDelete(DeleteBehavior.Cascade);
            b.HasOne(p => p.ChallengeSpec).WithMany(u => u.Feedback).OnDelete(DeleteBehavior.Cascade);
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(u => u.UserId).HasMaxLength(40);
            b.Property(u => u.PlayerId).HasMaxLength(40);
            b.Property(u => u.ChallengeId).HasMaxLength(40);
            b.Property(u => u.ChallengeSpecId).HasMaxLength(40);
            b.Property(u => u.GameId).HasMaxLength(40);
        });

        builder.Entity<FeedbackSubmission>(b =>
        {
            b
                .HasDiscriminator(b => b.AttachedEntityType)
                    .HasValue<FeedbackSubmissionChallengeSpec>(FeedbackResponseAttachedEntityType.ChallengeSpec)
                    .HasValue<FeedbackSubmissionGame>(FeedbackResponseAttachedEntityType.Game);
            b
                .HasOne(f => f.User)
                .WithMany(u => u.FeedbackResponses);

            b
                .HasOne(f => f.FeedbackTemplate)
                .WithMany(t => t.Submissions);

            // configures the question data as proper database JSON
            b.OwnsMany(f => f.Responses, ownedJsonEntityBuilder =>
            {
                ownedJsonEntityBuilder.ToTable("FeedbackResponsesData");
            });
        });

        builder.Entity<FeedbackSubmissionGame>(b =>
        {
            b
                .HasOne<Data.Game>()
                .WithMany(g => g.FeedbackSubmissions);
        });

        builder.Entity<FeedbackTemplate>(b =>
        {
            b.Property(b => b.Name).HasStandardNameLength().IsRequired();
            b.Property(b => b.Content).IsRequired();
            b.Property(b => b.HelpText).HasMaxLength(200);
            b.HasOne(b => b.CreatedByUser).WithMany(u => u.CreatedFeedbackTemplates).IsRequired();
            b.HasMany(t => t.UseAsFeedbackTemplateForGameChallenges).WithOne(g => g.FeedbackTemplate);
            b.HasMany(t => t.UseAsFeedbackTemplateForGames).WithOne(g => g.ChallengesFeedbackTemplate);
        });

        builder.Entity<Game>(b =>
        {
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(p => p.Sponsor).HasMaxLength(40);
            b.Property(p => p.TestCode).HasMaxLength(40);
            b.Property(p => p.Name).HasMaxLength(64);
            b.Property(p => p.Competition).HasMaxLength(64);
            b.Property(p => p.Season).HasMaxLength(64);
            b.Property(p => p.Division).HasMaxLength(64);
            b.Property(p => p.Track).HasMaxLength(64);
            b.Property(p => p.Logo).HasMaxLength(64);
            b.Property(p => p.Background).HasMaxLength(64);
            b.Property(p => p.TestCode).HasMaxLength(64);
            b.Property(p => p.CardText1).HasMaxLength(64);
            b.Property(p => p.CardText2).HasMaxLength(64);
            b.Property(p => p.CardText3).HasMaxLength(64);
            b.Property(p => p.Mode).HasMaxLength(40);
        });

        builder.Entity<ArchivedChallenge>(b =>
        {
            // Archive is snapshot with no foreign keys; explicitly index Id fields for searching
            b.HasIndex(p => p.TeamId);
            b.HasIndex(p => p.GameId);
            b.HasIndex(p => p.PlayerId);
            b.HasIndex(p => p.UserId);
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(u => u.TeamId).HasMaxLength(40);
            b.Property(p => p.GameId).HasMaxLength(40);
            b.Property(p => p.GameName).HasMaxLength(64);
            b.Property(u => u.PlayerId).HasMaxLength(40);
            b.Property(p => p.PlayerName).HasMaxLength(64);
            b.Property(u => u.UserId).HasMaxLength(40);
        });

        builder.Entity<Player>(b =>
        {
            b.HasKey(p => p.Id);
            b.HasIndex(p => p.TeamId);
            b.Property(p => p.Id).HasMaxLength(40);
            b.Property(p => p.TeamId).HasMaxLength(40);
            b.Property(p => p.UserId).HasMaxLength(40);
            b.Property(p => p.GameId).HasMaxLength(40);
            b.Property(p => p.ApprovedName).HasMaxLength(64);
            b.Property(p => p.Name).HasMaxLength(64);
            b.Property(p => p.NameStatus).HasMaxLength(40);
            b.Property(p => p.InviteCode).HasMaxLength(40);
            b.Property(p => p.AdvancedFromTeamId).HasStandardGuidLength();

            // performance-oriented indices
            b.HasIndex(p => p.UserId);
            b.HasIndex(p => new { p.UserId, p.TeamId });
            b.HasIndex(p => new { p.Id, p.TeamId });

            // nav properties
            b.HasOne(p => p.User).WithMany(u => u.Enrollments).OnDelete(DeleteBehavior.Cascade);
            b
                .HasOne(p => p.Sponsor).WithMany(s => s.SponsoredPlayers)
                .IsRequired();

            b
                .HasOne(p => p.AdvancedFromGame)
                .WithMany(g => g.AdvancedPlayers)
                .OnDelete(DeleteBehavior.SetNull);

            b
                .HasOne(p => p.AdvancedFromPlayer)
                .WithMany(p => p.AdvancedToPlayers)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PublishedCertificate>(b =>
        {
            b.HasKey(c => c.Id);
            b.HasDiscriminator(c => c.Mode)
                .HasValue<PublishedCompetitiveCertificate>(PublishedCertificateMode.Competitive)
                .HasValue<PublishedPracticeCertificate>(PublishedCertificateMode.Practice);
        });

        builder.Entity<PublishedCompetitiveCertificate>(b =>
        {
            b.Property(c => c.GameId).HasStandardGuidLength();
            b.HasOne(c => c.Game).WithMany(g => g.PublishedCompetitiveCertificates);

            b.HasOne(c => c.OwnerUser)
                .WithMany(u => u.PublishedCompetitiveCertificates)
                .HasConstraintName("FK_OwnerUserId_Users_Id");
        });

        builder.Entity<PublishedPracticeCertificate>(b =>
        {
            b.Property(c => c.ChallengeSpecId).HasStandardGuidLength();
            b.HasOne(c => c.ChallengeSpec).WithMany(s => s.PublishedPracticeCertificates);

            b.HasOne(c => c.OwnerUser)
                .WithMany(u => u.PublishedPracticeCertificates)
                .HasConstraintName("FK_OwnerUserId_Users_Id");
        });

        builder.Entity<PracticeModeSettings>(b =>
        {
            b.HasKey(m => m.Id);
            b.Property(m => m.Id).HasStandardGuidLength();
            b.Property(m => m.IntroTextMarkdown).HasMaxLength(4000);
            b
                .HasOne(m => m.UpdatedByUser)
                .WithOne(u => u.UpdatedPracticeModeSettings)
                .IsRequired(false);
        });

        builder.Entity<Sponsor>(b =>
        {
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(u => u.Name).HasMaxLength(128);
            b.HasOne(s => s.ParentSponsor)
                .WithMany(p => p.ChildSponsors);
        });

        builder.Entity<SupportSettings>(b =>
        {
            b.HasKey(b => b.Id);
            b.Property(b => b.Id).HasStandardGuidLength();
            b
                .Property(b => b.UpdatedOn)
                .IsRequired();

            b
                .HasOne(b => b.UpdatedByUser)
                .WithOne(u => u.UpdatedSupportSettings)
                .HasForeignKey<SupportSettings>(s => s.UpdatedByUserId)
                    .OnDelete(DeleteBehavior.SetNull)
                .IsRequired();
        });

        builder.Entity<SupportSettingsAutoTag>(b =>
        {
            b.HasKey(b => b.Id);
            b
                .Property(b => b.Tag)
                .IsRequired()
                .HasStandardNameLength();

            b
                .Property(b => b.ConditionValue)
                .IsRequired()
                .HasStandardGuidLength();

            b
                .HasOne(b => b.SupportSettings)
                .WithMany(s => s.AutoTags)
                .HasForeignKey(s => s.SupportSettingsId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        builder.Entity<SystemNotification>(b =>
        {
            b.HasKey(n => n.Id);
            b.Property(n => n.Id).HasStandardGuidLength();
            b.Property(n => n.Title)
                .HasStandardNameLength()
                .IsRequired();
            b.Property(n => n.IsDismissible).HasDefaultValue(true);
            b.Property(n => n.MarkdownContent).IsRequired();

            // nav properties
            b
                .HasOne(n => n.CreatedByUser)
                .WithMany(u => u.CreatedSystemNotifications)
                .IsRequired();

            b
                .HasMany(n => n.Interactions)
                .WithOne(i => i.SystemNotification)
                .IsRequired();
        });

        builder.Entity<SystemNotificationInteraction>(b =>
        {
            b.HasKey(i => i.Id);
            b.HasAlternateKey(i => new { i.SystemNotificationId, i.UserId });

            b
                .HasOne(i => i.User)
                .WithMany(u => u.SystemNotificationInteractions)
                .IsRequired();
        });

        builder.Entity<Ticket>(b =>
        {
            b.HasOne(p => p.Challenge).WithMany(u => u.Tickets).OnDelete(DeleteBehavior.SetNull);
            b.HasOne(p => p.Player).WithMany(u => u.Tickets).OnDelete(DeleteBehavior.SetNull);
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(u => u.CreatorId).HasMaxLength(40);
            b.Property(u => u.RequesterId).HasMaxLength(40);
            b.Property(u => u.AssigneeId).HasMaxLength(40);
            b.Property(u => u.ChallengeId).HasMaxLength(40);
            b.Property(u => u.PlayerId).HasMaxLength(40);
            b.Property(u => u.TeamId).HasMaxLength(40);
            b.Property(u => u.Status).HasMaxLength(64);
            b.Property(u => u.Key).UseSerialColumn(); // Serial increment by 1
            b.Property(u => u.Summary).HasMaxLength(128).IsRequired();
            b.HasIndex(u => u.Key).IsUnique();
        });

        builder.Entity<TicketActivity>(b =>
        {
            b.HasOne(p => p.Ticket).WithMany(u => u.Activity).OnDelete(DeleteBehavior.Cascade);
            b.Property(u => u.TicketId).HasMaxLength(40);
            b.Property(u => u.UserId).HasMaxLength(40);
            b.Property(u => u.AssigneeId).HasMaxLength(40);
            b.Property(u => u.Status).HasMaxLength(64);
        });

        builder.Entity<User>(b =>
        {
            b.Property(u => u.Id).HasMaxLength(40);
            b.Property(u => u.Username).HasMaxLength(64);
            b.Property(u => u.ApprovedName).HasMaxLength(64);
            b.Property(u => u.Name).HasMaxLength(64);
            b.Property(u => u.NameStatus).HasMaxLength(40);
            b.Property(u => u.Email).HasMaxLength(64);
            b.Property(u => u.LoginCount).HasDefaultValueSql("0");
            b.Property(u => u.PlayAudioOnBrowserNotification).HasDefaultValue(false);

            // nav properties
            b.HasOne(u => u.Sponsor).WithMany(s => s.SponsoredUsers)
                .IsRequired();
        });
    }

    public DbSet<ApiKey> ApiKeys { get; set; }
    public DbSet<ArchivedChallenge> ArchivedChallenges { get; set; }
    public DbSet<AwardedChallengeBonus> AwardedChallengeBonuses { get; set; }
    public DbSet<Challenge> Challenges { get; set; }
    public DbSet<ChallengeBonus> ChallengeBonuses { get; set; }
    public DbSet<ChallengeBonusCompleteSolveRank> ChallengeBonusesCompleteSolveRank { get; set; }
    public DbSet<ChallengeEvent> ChallengeEvents { get; set; }
    public DbSet<ChallengeGate> ChallengeGates { get; set; }
    public DbSet<ChallengeSpec> ChallengeSpecs { get; set; }
    public DbSet<ChallengeSubmission> ChallengeSubmissions { get; set; }
    public DbSet<DenormalizedTeamScore> DenormalizedTeamScores { get; set; }
    public DbSet<Extension> Extensions { get; set; }
    public DbSet<ExternalGameHost> ExternalGameHosts { get; set; }
    public DbSet<ExternalGameTeam> ExternalGameTeams { get; set; }
    public DbSet<Feedback> Feedback { get; set; }
    public DbSet<FeedbackSubmission> FeedbackSubmissions { get; set; }
    public DbSet<FeedbackTemplate> FeedbackTemplates { get; set; }
    public DbSet<Game> Games { get; set; }
    public DbSet<ManualBonus> ManualBonuses { get; set; }
    public DbSet<Player> Players { get; set; }
    public DbSet<PublishedCertificate> PublishedCertificate { get; set; }
    public DbSet<Sponsor> Sponsors { get; set; }
    public DbSet<SupportSettingsAutoTag> SupportSettingsAutoTags { get; set; }
    public DbSet<SystemNotification> SystemNotifications { get; set; }
    public DbSet<SystemNotificationInteraction> SystemNotificationInteractions { get; set; }
    public DbSet<Ticket> Tickets { get; set; }
    public DbSet<TicketActivity> TicketActivity { get; set; }
    public DbSet<User> Users { get; set; }
}
