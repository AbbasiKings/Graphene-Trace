namespace GrapheneTrace.Core.DTOs.Patient;

public class FileUploadResultDto
{
    public string FileName { get; set; } = string.Empty;
    public int FramesProcessed { get; set; }
    public int AlertsRaised { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}

