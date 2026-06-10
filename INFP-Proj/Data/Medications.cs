using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Medications
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int MedicationID { get; set; }
        public required string MedicationName { get; set; }
        public bool Approval { get; set; } = false;
        public required TimeOnly ConsumptionTime { get; set; }
    }
}
