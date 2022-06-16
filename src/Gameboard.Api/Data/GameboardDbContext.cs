// Copyright 2021 Carnegie Mellon University. All Rights Reserved.
// Released under a MIT (SEI)-style license. See LICENSE.md in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace Gameboard.Api.Data
{
    public class GameboardDbContext : DbContext
    {
        public GameboardDbContext(DbContextOptions options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(b => {
                b.Property(u => u.Id).HasMaxLength(40);
                b.Property(u => u.Username).HasMaxLength(64);
                b.Property(u => u.ApprovedName).HasMaxLength(64);
                b.Property(u => u.Name).HasMaxLength(64);
                b.Property(u => u.NameStatus).HasMaxLength(40);
                b.Property(u => u.Email).HasMaxLength(64);
                b.Property(u => u.Sponsor).HasMaxLength(40);
            });

            builder.Entity<Player>(b => {
                b.HasOne(p => p.User).WithMany(u => u.Enrollments).OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(p => p.TeamId);
                b.Property(p => p.Id).HasMaxLength(40);
                b.Property(p => p.TeamId).HasMaxLength(40);
                b.Property(p => p.UserId).HasMaxLength(40);
                b.Property(p => p.GameId).HasMaxLength(40);
                b.Property(p => p.ApprovedName).HasMaxLength(64);
                b.Property(p => p.Name).HasMaxLength(64);
                b.Property(p => p.NameStatus).HasMaxLength(40);
                b.Property(p => p.Sponsor).HasMaxLength(40);
                b.Property(p => p.InviteCode).HasMaxLength(40);
            });

            builder.Entity<Game>(b => {
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
                b.Property(p => p.Key).HasMaxLength(64);
                b.Property(p => p.CardText1).HasMaxLength(64);
                b.Property(p => p.CardText2).HasMaxLength(64);
                b.Property(p => p.CardText3).HasMaxLength(64);
                b.Property(p => p.Mode).HasMaxLength(40);
            });


            builder.Entity<Challenge>(b => {
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

            builder.Entity<ChallengeEvent>(b => {
                b.HasOne(p => p.Challenge).WithMany(u => u.Events).OnDelete(DeleteBehavior.Cascade);
                b.Property(u => u.Id).HasMaxLength(40);
                b.Property(u => u.ChallengeId).HasMaxLength(40);
                b.Property(u => u.UserId).HasMaxLength(40);
                b.Property(u => u.TeamId).HasMaxLength(40);
                b.Property(u => u.Text).HasMaxLength(1024);
            });

            builder.Entity<ChallengeSpec>(b => {
                b.HasOne(p => p.Game).WithMany(u => u.Specs).OnDelete(DeleteBehavior.Cascade);
                b.Property(u => u.Id).HasMaxLength(40);
                b.Property(u => u.GameId).HasMaxLength(40);
                b.Property(u => u.ExternalId).HasMaxLength(40);
            });

            builder.Entity<ChallengeGate>(b => {
                b.HasOne(p => p.Game).WithMany(u => u.Prerequisites).OnDelete(DeleteBehavior.Cascade);
                b.Property(g => g.Id).HasMaxLength(40);
                b.Property(g => g.TargetId).HasMaxLength(40);
                b.Property(g => g.RequiredId).HasMaxLength(40);
                b.Property(g => g.GameId).HasMaxLength(40);
            });

            builder.Entity<Sponsor>(b => {
                b.Property(u => u.Id).HasMaxLength(40);
                b.Property(u => u.Name).HasMaxLength(128);
            });

            builder.Entity<Feedback>(b => {
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

            builder.Entity<ArchivedChallenge>(b => {
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

            builder.Entity<Ticket>(b => {
                // todo - limits and FK constraints
                b.Property(u => u.AssigneeId).IsRequired(false);
                // b.Property(u => u.Key).ValueGeneratedOnAdd();
                b.Property(u => u.Key).UseSerialColumn();
                b.Property(u => u.Summary).HasMaxLength(128).IsRequired();
            });

            builder.Entity<TicketActivity>(b => {
                b.HasOne(p => p.Ticket).WithMany(u => u.Activity).OnDelete(DeleteBehavior.Cascade);
            });


        }

        public DbSet<User> Users { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Challenge> Challenges { get; set; }
        public DbSet<ChallengeEvent> ChallengeEvents { get; set; }
        public DbSet<ChallengeSpec> ChallengeSpecs { get; set; }
        public DbSet<ChallengeGate> ChallengeGates { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }
        public DbSet<Feedback> Feedback { get; set; }
        public DbSet<ArchivedChallenge> ArchivedChallenges { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketActivity> TicketActivity { get; set; }
    }
}
