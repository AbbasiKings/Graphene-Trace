namespace GrapheneTrace.Client.Models
{
    public record AdminMetric(string Title, string Value, string Subtitle, string Icon);

    public record UsageTrendPoint(string Label, int Patients, int Clinicians);

    public record DataIngestionStatus(string UserId, DateTime Timestamp, string Status, string Message, string Severity);

    public record AuditLogEntry(DateTime OccurredAt, string Actor, string Action, string Target, string Severity);

    public record AdminUser(
        string Id,
        string FullName,
        string Email,
        string Role,
        string Status,
        DateTime LastLogin,
        string AssignedClinician,
        int TotalFrames,
        DateTime LastDataReceived);

    public class AdminUserFormModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Patient";

        public string Status { get; set; } = "Active";

        public string AssignedClinician { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public bool IsNew => string.IsNullOrWhiteSpace(Id);
    }

    public class ConfigurationSetting
    {
        public ConfigurationSetting(string name, string description, double value, double min, double max, string suffix)
        {
            Name = name;
            Description = description;
            Value = value;
            Min = min;
            Max = max;
            Suffix = suffix;
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public double Value { get; set; }

        public double Min { get; set; }

        public double Max { get; set; }

        public string Suffix { get; set; }
    }

    public record AuditLogDetail(DateTime OccurredAt, string Actor, string IpAddress, string Action, string Outcome, string Notes);

    public class DatabaseStatus
    {
        public DatabaseStatus(string system, string status, DateTime lastUpdated, string description)
        {
            System = system;
            Status = status;
            LastUpdated = lastUpdated;
            Description = description;
        }

        public string System { get; set; }

        public string Status { get; set; }

        public DateTime LastUpdated { get; set; }

        public string Description { get; set; }
    }

    public class ContentTemplate
    {
        public ContentTemplate(string key, string description, string subject, string body)
        {
            Key = key;
            Description = description;
            Subject = subject;
            Body = body;
        }

        public string Key { get; set; }

        public string Description { get; set; }

        public string Subject { get; set; }

        public string Body { get; set; }
    }
}

