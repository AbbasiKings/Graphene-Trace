namespace GrapheneTrace.Core.DTOs.Patient;

public class DataUploadDto
{
    public string RawDataString { get; set; } = string.Empty;
    public DateTime? TimestampUtc { get; set; }
}

