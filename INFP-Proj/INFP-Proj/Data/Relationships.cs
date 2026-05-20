namespace INFP_Proj.Data
{
    public class Relationships
    {
        public int PatientID { get; set; }
        public int UserID { get; set; }

        // Navigation
        public Patients? Patient { get; set; }
        public User? User { get; set; }
    }
}