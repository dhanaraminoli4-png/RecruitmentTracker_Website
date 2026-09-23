using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Models;

namespace RecruitmentTracker.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<JobVacancy> JobVacancies { get; set; }

        public DbSet<JobApplication> JobApplications { get; set; }

        // Interview types managed by HR
        public DbSet<InterviewType> InterviewTypes { get; set; }

        public DbSet<InterviewRound> InterviewRounds { get; set; }

        public DbSet<Interview> Interviews { get; set; }

        public DbSet<OfferTemplate> OfferTemplates { get; set; }


        public DbSet<InterviewerSchedule> InterviewerSchedules { get; set; }

        public DbSet<CandidateProfile> CandidateProfiles { get; set; }

        // =====================================================
        // DATABASE RELATIONSHIPS
        // =====================================================
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // VERY IMPORTANT for ASP.NET Identity
            base.OnModelCreating(builder);

            // Interview -> Job Application
            builder.Entity<Interview>()
                .HasOne(i => i.JobApplication)
                .WithMany()
                .HasForeignKey(i => i.JobApplicationId)
                .OnDelete(DeleteBehavior.NoAction);

            // Interview -> Interview Round
            builder.Entity<Interview>()
                .HasOne(i => i.InterviewRound)
                .WithMany()
                .HasForeignKey(i => i.InterviewRoundId)
                .OnDelete(DeleteBehavior.NoAction);

            // Interview -> Interview Type
            builder.Entity<Interview>()
                .HasOne(i => i.InterviewType)
                .WithMany()
                .HasForeignKey(i => i.InterviewTypeId)
                .OnDelete(DeleteBehavior.NoAction);

            // Removes the Salary decimal precision warning
            builder.Entity<JobVacancy>()
                .Property(j => j.Salary)
                .HasPrecision(18, 2);
        }
    }
}