using Humanizer;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace INFP_Proj.Data
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string>
    {
        // ^^^
        // IF required to add on stuff to the ASP tables, create a new class in Models
        // that inherits the relevant class. E.g AppUser.cs & AppRole.cs in Models Folder

        //IdentityUser        → AspNetUsers
        //IdentityRole        → AspNetRoles
        //IdentityUserRole    → AspNetUserRoles(join table, user↔role)
        //IdentityUserClaim   → AspNetUserClaims(extra user data/permissions)
        //IdentityUserLogin   → AspNetUserLogins(external logins, e.g.Google)
        //IdentityUserToken   → AspNetUserTokens(auth tokens, refresh tokens)
        //IdentityRoleClaim   → AspNetRoleClaims(permissions attached to roles)

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
        public DbSet<BloodWork> BloodWorks { get; set; }
        public DbSet<DoctorRequest> DoctorRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

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

            modelBuilder.Entity<Records>()
                .HasOne(r => r.MedicationList)
                .WithMany()
                .HasForeignKey(r => r.MedicationListID)
                .OnDelete(DeleteBehavior.Cascade);

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
        }
    }
}
