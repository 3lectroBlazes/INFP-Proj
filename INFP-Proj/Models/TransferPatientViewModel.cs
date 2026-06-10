using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Models
{
    public class TransferPatientViewModel
    {
        [Required(ErrorMessage = "Please select a patient to transfer.")]
        [Display(Name = "Select Patient")]
        public int PatientID { get; set; }

        [Required(ErrorMessage = "Please select a new bed.")]
        [Display(Name = "Select New Bed")]
        public int NewBedID { get; set; }
    }
}