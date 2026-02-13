namespace BumaSoft.DependencyWizard.Configurations;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class ConfigurationAttribute : Attribute
{
    public string? Section { get; set; }

    public ConfigurationAttribute(string section) => Section = section;

    public ConfigurationAttribute()
    {
    }
}
