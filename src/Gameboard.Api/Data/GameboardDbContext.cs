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

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Challenge> Challenges { get; set; }
        public DbSet<ChallengeEvent> ChallengeEvents { get; set; }
        public DbSet<ChallengeSpec> ChallengeSpecs { get; set; }
        public DbSet<ChallengeGate> ChallengeGates { get; set; }
        public DbSet<Sponsor> Sponsors { get; set; }
    }
}
