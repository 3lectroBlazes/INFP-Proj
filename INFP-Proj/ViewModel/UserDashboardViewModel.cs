namespace INFP_Proj.ViewModel
{
    public class UserDashboardViewModel
    {
        public bool HasPatientRecord { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; } = "Unknown patient";
        public string PatientStatus { get; set; } = "Unknown";
        public string PatientNotes { get; set; } = "No notes recorded.";

        public string HospitalName { get; set; } = "Not assigned";
        public string HospitalAddress { get; set; } = "Not assigned";

        public string WardName { get; set; } = "Not assigned";
        public string Room { get; set; } = "Not assigned";
        public string Floor { get; set; } = "Not assigned";
        public string Sector { get; set; } = "Not assigned";
        public string BedLocation { get; set; } = "Not assigned";
        public float? BedTemperature { get; set; }
        public float? Weight { get; set; }

        public float? HeartRate { get; set; }
        public float? SystolicBloodPressure { get; set; }
        public float? DiastolicBloodPressure { get; set; }
        public float? RespiratoryRate { get; set; }
        public float? Temperature { get; set; }
        public DateTime? LatestVitalsRecordedAt { get; set; }

        public float? BraceletBattery { get; set; }
        public float? Movement { get; set; }
        public string BraceletLocation { get; set; } = "Unknown";

        public string Diagnosis { get; set; } = "No diagnosis recorded";
        public string MedicationName { get; set; } = "No medication recorded";
        public string Dosage { get; set; } = "N/A";
        public TimeOnly? MedicationTime { get; set; }

        public class UserMedicationItem
        {
            public string MedicationName { get; set; } = "Unknown medication";
            public string Dosage { get; set; } = "N/A";
            public TimeOnly? ConsumptionTime { get; set; }
        }

        public List<UserMedicationItem> CurrentMedications { get; set; } = new();

        public string RecordDescription { get; set; } = "No record description";
        public DateTime? AdmissionDateTime { get; set; }
        public DateTime? DischargeDateTime { get; set; }

        public bool HasUnresolvedEmergency { get; set; }
        public string AlertMessage { get; set; } = "No urgent alerts.";

        public bool HasDoctorRequest { get; set; }
        public int? LatestDoctorRequestId { get; set; }
        public string LatestDoctorRequestMessage { get; set; } = "No doctor request submitted.";
        public string LatestDoctorReply { get; set; } = "No reply yet.";
        public bool LatestDoctorRequestCompleted { get; set; }
        public DateTime? LatestDoctorRequestDate { get; set; }
    }
}