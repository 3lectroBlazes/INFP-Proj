using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Wards
    {
        [Key]
        public int WardID { get; set; }
        public required string WardName { get; set; }
        public required int MaxCapacity { get; set; }
    }
}
