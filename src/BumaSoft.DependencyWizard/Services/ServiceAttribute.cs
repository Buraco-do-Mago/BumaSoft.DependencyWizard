namespace BumaSoft.DependencyWizard.Services;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class ServiceAttribute : Attribute
{
    public ServiceScope Scope { get; set; } = ServiceScope.Scoped;

    public ServiceAttribute(ServiceScope scope) => Scope = scope;

    public ServiceAttribute()
    {
    }
}
