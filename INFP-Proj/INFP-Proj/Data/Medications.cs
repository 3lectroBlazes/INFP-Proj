using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Medications
    {
        [Key]
        public int MedicationID { get; set; }
        public required string MedicationName { get; set; }
        public required TimeOnly ConsumptionTime { get; set; }
    }
}
