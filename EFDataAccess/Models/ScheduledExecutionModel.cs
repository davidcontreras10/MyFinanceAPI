using Microsoft.EntityFrameworkCore;

namespace EFDataAccess.Models
{
    public static class ScheduledExecutionModel
    {
        private const int MaximumTimeZoneIdLength = 128;
        private const int MaximumAttemptFailureStageLength = 64;
        private const int MaximumAttemptFailureTypeLength = 200;
        private const int MaximumAttemptFailureMessageLength = 1000;

        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AutomaticTask>().Property(x => x.RetiredAtUtc).HasColumnType("datetime2");
            modelBuilder.Entity<AppUser>().Property(x => x.SchedulingTimeZoneId)
                .HasMaxLength(MaximumTimeZoneIdLength).IsRequired().HasDefaultValue(MyFinanceModel.ScheduledUserPreferences.DefaultTimeZone);
            modelBuilder.Entity<ScheduledTaskSchedule>(entity =>
            {
                entity.ToTable("ScheduledTaskSchedule", table => table.HasCheckConstraint(
                    "CK_ScheduledTaskSchedule_Cursor", "[NextScanUtc] >= [ActivatedAtUtc]"));
                entity.HasKey(x => x.TaskId);
                entity.Property(x => x.TaskId).ValueGeneratedNever();
                entity.HasIndex(x => x.NextScanUtc);
                entity.HasOne<AutomaticTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<ScheduledTaskRevision>(entity =>
            {
                entity.ToTable("ScheduledTaskRevision", table => table.HasCheckConstraint(
                    "CK_ScheduledTaskRevision_Window", "[EffectiveUntilUtc] IS NULL OR [EffectiveUntilUtc] > [EffectiveFromUtc]"));
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TaskId, x.EffectiveFromUtc }).IsUnique();
                entity.HasIndex(x => x.TaskId).IsUnique().HasFilter("[EffectiveUntilUtc] IS NULL");
                entity.Property(x => x.TimeZoneId).HasMaxLength(MaximumTimeZoneIdLength).IsRequired();
                entity.Property(x => x.DefinitionJson).IsRequired();
                entity.HasOne<ScheduledTaskSchedule>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<ScheduledTaskOccurrence>(entity =>
            {
                entity.ToTable("ScheduledTaskOccurrence", table => table.HasCheckConstraint(
                    "CK_ScheduledTaskOccurrence_Date", "CONVERT(date, [PostingDate]) = [ScheduledLocalDate]"));
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.TaskId, x.ScheduledLocalDate }).IsUnique();
                entity.HasIndex(x => x.DueAtUtc);
                entity.Property(x => x.ScheduledLocalDate).HasColumnType("date");
                entity.Property(x => x.TimeZoneId).HasMaxLength(MaximumTimeZoneIdLength).IsRequired();
                entity.Property(x => x.DefinitionJson).IsRequired();
                entity.HasOne<AutomaticTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<ScheduledTaskCompletion>(entity =>
            {
                entity.ToTable("ScheduledTaskCompletion", table => table.HasCheckConstraint(
                    "CK_ScheduledTaskCompletion_Identity",
                    "([OccurrenceId] IS NOT NULL AND [ManualRequestId] IS NULL) OR ([OccurrenceId] IS NULL AND [ManualRequestId] IS NOT NULL)"));
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.OccurrenceId).IsUnique().HasFilter("[OccurrenceId] IS NOT NULL");
                entity.HasIndex(x => new { x.ActorId, x.ManualRequestId }).IsUnique().HasFilter("[ManualRequestId] IS NOT NULL");
                entity.HasOne<ScheduledTaskOccurrence>().WithMany().HasForeignKey(x => x.OccurrenceId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<AutomaticTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.NoAction);
            });
            modelBuilder.Entity<ScheduledTaskExecutionAttempt>(entity =>
            {
                entity.ToTable("ScheduledTaskExecutionAttempt", table =>
                {
                    table.HasCheckConstraint("CK_ScheduledTaskExecutionAttempt_Identity",
                        "([OccurrenceId] IS NOT NULL AND [ManualRequestId] IS NULL) OR ([OccurrenceId] IS NULL AND [ManualRequestId] IS NOT NULL)");
                    table.HasCheckConstraint("CK_ScheduledTaskExecutionAttempt_Number", "[AttemptNumber] > 0");
                    table.HasCheckConstraint("CK_ScheduledTaskExecutionAttempt_Status",
                        "([Status] = 1 AND [EndedAtUtc] IS NULL) OR ([Status] IN (2, 3, 4) AND [EndedAtUtc] IS NOT NULL)");
                });
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.OccurrenceId, x.AttemptNumber }).IsUnique()
                    .HasFilter("[OccurrenceId] IS NOT NULL");
                entity.HasIndex(x => new { x.ActorId, x.ManualRequestId, x.AttemptNumber }).IsUnique()
                    .HasFilter("[ManualRequestId] IS NOT NULL");
                entity.HasIndex(x => new { x.TaskId, x.StartedAtUtc });
                entity.Property(x => x.PostingDate).HasColumnType("date");
                entity.Property(x => x.StartedAtUtc).HasColumnType("datetime2");
                entity.Property(x => x.EndedAtUtc).HasColumnType("datetime2");
                entity.Property(x => x.FailureStage).HasMaxLength(MaximumAttemptFailureStageLength);
                entity.Property(x => x.FailureType).HasMaxLength(MaximumAttemptFailureTypeLength);
                entity.Property(x => x.FailureMessage).HasMaxLength(MaximumAttemptFailureMessageLength);
                entity.HasOne<ScheduledTaskOccurrence>().WithMany().HasForeignKey(x => x.OccurrenceId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<AutomaticTask>().WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.OwnerId).OnDelete(DeleteBehavior.NoAction);
                entity.HasOne<AppUser>().WithMany().HasForeignKey(x => x.ActorId).OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
