using CampusPulse.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CampusPulse.Infrastructure.Data
{
    public class CampusPulseDbContext : DbContext
    {
        public CampusPulseDbContext(DbContextOptions<CampusPulseDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Student> Students { get; set; }

        public DbSet<Lecturer> Lecturers { get; set; }

        public DbSet<Institution> Institutions { get; set; }

        public DbSet<Programme> Programmes { get; set; }

        public DbSet<Module> Modules { get; set; }

        public DbSet<StudentModule> StudentModules { get; set; }

        public DbSet<Venue> Venues { get; set; }

        public DbSet<Timetable> Timetables { get; set; }

        public DbSet<AttendanceSession> AttendanceSessions { get; set; }

        public DbSet<AttendanceRecord> AttendanceRecords { get; set; }

        public DbSet<Announcement> Announcements { get; set; }

        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Institution>().HasData(
                new Institution
                {
                    InstitutionId = 1,
                    InstitutionName = "Durban University of Technology",
                    StudentEmailDomain = "dut4life.ac.za"
                }
            );

            // User → Student
            modelBuilder.Entity<Student>()
                .HasOne<User>()
                .WithOne()
                .HasForeignKey<Student>(s => s.UserId);

            modelBuilder.Entity<Student>()
                .HasOne<Institution>()
                .WithMany()
                .HasForeignKey(s => s.InstitutionId);

            // User → Lecturer
            modelBuilder.Entity<Lecturer>()
                .HasOne<User>()
                .WithOne()
                .HasForeignKey<Lecturer>(l => l.UserId);

            // Programme → Module
            modelBuilder.Entity<Module>()
                .HasOne<Programme>()
                .WithMany()
                .HasForeignKey(m => m.ProgrammeId);

            // Student → StudentModule
            modelBuilder.Entity<StudentModule>()
                .HasOne<Student>()
                .WithMany()
                .HasForeignKey(sm => sm.StudentId);

            // Module → StudentModule
            modelBuilder.Entity<StudentModule>()
                .HasOne<Module>()
                .WithMany()
                .HasForeignKey(sm => sm.ModuleId);

            // Module → Timetable
            modelBuilder.Entity<Timetable>()
                .HasOne<Module>()
                .WithMany()
                .HasForeignKey(t => t.ModuleId);

            // Lecturer → Timetable
            modelBuilder.Entity<Timetable>()
                .HasOne<Lecturer>()
                .WithMany()
                .HasForeignKey(t => t.LecturerId);

            // Venue → Timetable
            modelBuilder.Entity<Timetable>()
                .HasOne<Venue>()
                .WithMany()
                .HasForeignKey(t => t.VenueId);

            // Timetable → AttendanceSession
            modelBuilder.Entity<AttendanceSession>()
                .HasOne<Timetable>()
                .WithMany()
                .HasForeignKey(a => a.TimetableId);

            modelBuilder.Entity<AttendanceRecord>()
                .HasOne<AttendanceSession>()
                .WithMany()
                .HasForeignKey(a => a.AttendanceSessionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AttendanceRecord>()
                .HasOne<Student>()
                .WithMany()
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Notification
            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId);
        }
    }
}