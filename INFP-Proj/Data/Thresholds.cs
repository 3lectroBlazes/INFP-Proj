using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Thresholds
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ThresholdID { get; set; }
        public float? SBPUpperThreshold { get; set; }
        public float? SBPLowerThreshold { get; set; }
        public float? DBPUpperThreshold { get; set; }
        public float? DBPLowerThreshold { get; set; }
        public float? HeartRateUpperPercentageThreshold { get; set; }
        public float? HeartRateLowerPercentageThreshold { get; set; }
        public float? RespiratoryRateUpperPercentageThreshold { get; set; }
        public float? RespiratoryRateLowerThreshold { get; set; }
    }
}
