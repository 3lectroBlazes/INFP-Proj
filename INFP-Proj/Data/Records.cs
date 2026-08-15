using INFP_Proj.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class Records
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int RecordID { get; set; }
    public int PatientID { get; set; }
    public int BedID { get; set; }
    public int WardID { get; set; }
    public int HospitalID { get; set; }
    public int DiagnosisID { get; set; }
    public string? Description { get; set; }
    public required DateTime AdmissionDateTime { get; set; } = DateTime.UtcNow;
    public DateTime? DischargeDateTime { get; set; }
    public string? DischargeReason { get; set; }

    [ForeignKey("PatientID")]
    public Patients? Patients { get; set; }
    [ForeignKey("BedID")]
    public Beds? Beds { get; set; }
    [ForeignKey("WardID")]
    public Wards? Wards { get; set; }
    [ForeignKey("HospitalID")]
    public Hospitals? Hospitals { get; set; }
    [ForeignKey("DiagnosisID")]
    public Diagnoses? Diagnoses { get; set; }
}