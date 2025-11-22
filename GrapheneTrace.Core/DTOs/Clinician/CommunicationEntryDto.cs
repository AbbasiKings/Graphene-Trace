using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DTOs.Clinician;

public class CommunicationEntryDto
{
    public Guid CommentId { get; set; }
    public Guid? PatientDataId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Text { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public UserRole AuthorRole { get; set; }
}






