namespace GrapheneTrace.Core.DTOs.Admin;

public class SystemConfigurationDto
{
    public List<ConfigurationSettingDto> Settings { get; set; } = new();
    public AlertConfigurationDto AlertConfig { get; set; } = new();
    public List<DatabaseStatusDto> DatabaseStatuses { get; set; } = new();
    public List<ContentTemplateDto> ContentTemplates { get; set; } = new();
}

public class ConfigurationSettingDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double Value { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
    public string Suffix { get; set; } = string.Empty;
}

public class AlertConfigurationDto
{
    public bool AlertingEnabled { get; set; } = true;
    public double AlertSensitivity { get; set; } = 68;
    public string EscalationWindow { get; set; } = "15 minutes";
    public string NotificationChannel { get; set; } = "Email";
}

public class DatabaseStatusDto
{
    public string System { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ContentTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class UpdateConfigurationDto
{
    public List<ConfigurationSettingDto>? Settings { get; set; }
    public AlertConfigurationDto? AlertConfig { get; set; }
    public List<ContentTemplateDto>? ContentTemplates { get; set; }
}


