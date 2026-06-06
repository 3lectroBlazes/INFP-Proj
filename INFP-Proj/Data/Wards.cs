using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Wards
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WardID { get; set; }
        public required string WardName { get; set; }
        public required int MaxCapacity { get; set; }
    }
}
