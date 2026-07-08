using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.ViewModel
{
    public class ForgotPassword
    {
        [Required]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }
    }
}
