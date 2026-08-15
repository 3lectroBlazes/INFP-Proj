using Humanizer;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static INFP_Proj.Pages.User.CareUpdatesModel;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace INFP_Proj.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }
        public DbSet<Patients> Patients { get; set; }
        public DbSet<Bracelet> Bracelets { get; set; }
        public DbSet<Vitals> Vitals { get; set; }
        public DbSet<AllergyList> AllergyLists { get; set; }
        public DbSet<Allergies> Allergies { get; set; }
        public DbSet<MedicationList> MedicationLists { get; set; }
        public DbSet<Medications> Medications { get; set; }
        public DbSet<Records> Records { get; set; }
        public DbSet<Beds> Beds { get; set; }
        public DbSet<Wards> Wards { get; set; }
        public DbSet<Hospitals> Hospitals { get; set; }
        public DbSet<Diagnoses> Diagnoses { get; set; }
        public DbSet<Relationships> Relationships { get; set; }
        public DbSet<BraceletRelation> BraceletRelations { get; set; }
        public DbSet<Log> Logs { get; set; }
        public DbSet<LogAcknowledgement> LogAcknowledgements { get; set; }
        public DbSet<DoctorRequest> DoctorRequests { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentChangeRequest> AppointmentChangeRequests { get; set; }
        public DbSet<Thresholds> Thresholds { get; set; }
        public DbSet<DeathCerts> DeathCerts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LogAcknowledgement>(entity =>
            {
                entity.ToTable("LogAcknowledgements");

                entity.HasKey(acknowledgement =>
                    acknowledgement.LogAcknowledgementID);

                entity.Property(acknowledgement =>
                        acknowledgement.UserID)
                    .HasMaxLength(450)
                    .IsRequired();

                entity.HasIndex(acknowledgement => new
                {
                    acknowledgement.LogID,
                    acknowledgement.UserID
                })
                    .IsUnique()
                    .HasDatabaseName(
                        "UX_LogAcknowledgements_LogID_UserID");

                entity.HasOne<Log>()
                    .WithMany()
                    .HasForeignKey(acknowledgement =>
                        acknowledgement.LogID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne<AppUser>()
                    .WithMany()
                    .HasForeignKey(acknowledgement =>
                        acknowledgement.UserID)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<AppointmentChangeRequest>(entity =>
            {
                entity.ToTable("AppointmentChangeRequests");

                entity.HasKey(request =>
                    request.AppointmentChangeRequestID);

                entity.Property(request =>
                        request.Status)
                    .HasMaxLength(30)
                    .IsRequired();

                entity.Property(request =>
                        request.Reason)
                    .HasMaxLength(500);

                entity.Property(request =>
                        request.ReviewMessage)
                    .HasMaxLength(500);

                entity.Property(request =>
                        request.ReviewedByUserID)
                    .HasMaxLength(450);

                entity.HasOne(request =>
                        request.Appointment)
                    .WithMany()
                    .HasForeignKey(request =>
                        request.AppointmentRequestID)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(request =>
                        request.Patient)
                    .WithMany()
                    .HasForeignKey(request =>
                        request.PatientID)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasIndex(request =>
                        request.AppointmentRequestID)
                    .HasDatabaseName(
                        "UX_AppointmentChangeRequests_OnePending")
                    .IsUnique()
                    .HasFilter("[Status] = 'Pending'");

                entity.HasIndex(request =>
                        request.PatientID)
                    .HasDatabaseName(
                        "IX_AppointmentChangeRequests_PatientID");
            });

            modelBuilder.Entity<Records>()
                .HasOne(r => r.Patients)
                .WithMany()
                .HasForeignKey(r => r.PatientID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Records>()
                .HasOne(r => r.Beds)
                .WithMany()
                .HasForeignKey(r => r.BedID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Records>()
                .HasOne(r => r.Wards)
                .WithMany()
                .HasForeignKey(r => r.WardID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Records>()
                .HasOne(r => r.Hospitals)
                .WithMany()
                .HasForeignKey(r => r.HospitalID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Records>()
                .HasOne(r => r.Diagnoses)
                .WithMany()
                .HasForeignKey(r => r.DiagnosisID)
                .OnDelete(DeleteBehavior.Restrict);

            // CHANGED: MedicationList now belongs to a Records (many-to-one),
            // instead of Records pointing at a single MedicationList.
            modelBuilder.Entity<MedicationList>()
                .HasOne(m => m.Records)
                .WithMany()
                .HasForeignKey(m => m.RecordID)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Relationships>()
                .HasKey(r => new { r.PatientID, r.UserID });

            modelBuilder.Entity<Relationships>()
                .HasOne(r => r.Patient)
                .WithMany(p => p.Relationships)
                .HasForeignKey(r => r.PatientID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Relationships>()
                .HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(r => r.UserID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BraceletRelation>()
                .HasKey(br => new { br.PatientID, br.BraceletID });

            modelBuilder.Entity<BraceletRelation>()
                .HasOne(br => br.Patient)
                .WithMany(p => p.BraceletRelations)
                .HasForeignKey(br => br.PatientID)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BraceletRelation>()
                .HasOne(br => br.Bracelet)
                .WithMany(b => b.BraceletRelations)
                .HasForeignKey(br => br.BraceletID)
                .OnDelete(DeleteBehavior.NoAction);
            modelBuilder.Entity<Appointment>()
            .Property(a => a.DoctorID)
            .HasMaxLength(450);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.User)
                .WithMany()
                .HasForeignKey(a => a.DoctorID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}