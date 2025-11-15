namespace GrapheneTrace.Core.Models;

public class ConfigurationSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

