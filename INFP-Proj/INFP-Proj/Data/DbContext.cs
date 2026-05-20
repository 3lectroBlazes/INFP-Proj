using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
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
        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Composite PK for Relationships junction table
            modelBuilder.Entity<Relationships>()
                .HasKey(r => new { r.PatientID, r.UserID });
        }
    }
}