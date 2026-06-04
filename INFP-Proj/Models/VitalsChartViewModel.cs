namespace INFP_Proj.Models
{
    public class VitalsChartViewModel
    {
        public List<string> Labels { get; set; } = new();
        public List<float?> HeartRate { get; set; } = new();
        public List<float?> RespiratoryRate { get; set; } = new();
        public List<float?> BloodPressure { get; set; } = new();
        public List<float?> Temperature { get; set; } = new();
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public List<PatientSelectItem> Patients { get; set; } = new();
        public bool ShowPatientSelector { get; set; }
    }

    public class PatientSelectItem
    {
        public int PatientId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>One patient's vitals series aligned to shared chart labels (admin multi-select).</summary>
    public class PatientVitalsSeries
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public List<float?> HeartRate { get; set; } = new();
        public List<float?> RespiratoryRate { get; set; } = new();
        public List<float?> BloodPressure { get; set; } = new();
        public List<float?> Temperature { get; set; } = new();
    }

    public class AdminVitalsChartViewModel
    {
        public List<PatientSelectItem> Patients { get; set; } = new();
        public List<int> SelectedPatientIds { get; set; } = new();
        public List<string> Labels { get; set; } = new();
        public List<PatientVitalsSeries> Series { get; set; } = new();
    }
}
