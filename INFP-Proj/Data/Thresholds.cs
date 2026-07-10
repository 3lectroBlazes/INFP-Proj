using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Thresholds
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ThresholdID { get; set; }
        public float? SystolicBloodPressureThreshold { get; set; }
        public float? DiastolicBloodPressureThreshold { get; set; }
        public float? HeartRatePercentageThreshold { get; set; }
        public float? RespiratoryRatePercentageThreshold { get; set; }
        public float? TemperatureThreshold { get; set; }
    }
}
