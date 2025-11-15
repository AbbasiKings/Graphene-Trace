namespace GrapheneTrace.Core.DTOs.Patient;

public class UploadFrameDto
{
    public Guid PatientId { get; set; }
    public string CsvData { get; set; } = string.Empty;
}

