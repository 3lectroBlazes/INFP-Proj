using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Thresholds
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ThresholdID { get; set; }
        public int SBPUpperThreshold { get; set; }
        public int SBPLowerThreshold { get; set; }
        public int DBPUpperThreshold { get; set; }
        public int DBPLowerThreshold { get; set; }
        public int HeartRateUpperPercentageThreshold { get; set; }
        public int HeartRateLowerPercentageThreshold { get; set; }
        public int RespiratoryRateUpperPercentageThreshold { get; set; }
        public int RespiratoryRateLowerThreshold { get; set; }
    }
}
