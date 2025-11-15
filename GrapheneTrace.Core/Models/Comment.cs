namespace GrapheneTrace.Core.Models;

public class Comment : BaseEntity
{
    public Guid PatientId { get; set; }
    public User Patient { get; set; } = default!;
    public Guid AuthorId { get; set; }
    public User Author { get; set; } = default!;
    public string Text { get; set; } = string.Empty;
    public Guid? PatientDataId { get; set; }
    public PatientData? PatientData { get; set; }
}

