using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Diagnoses
    {
        [Key]
        public int DiagnosisID { get; set; }
        public required string DiagnosisName { get; set; }
    }
}
