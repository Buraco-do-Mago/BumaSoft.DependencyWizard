namespace BumaSoft.DependencyWizard.Services;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class ServiceAttribute(ServiceScope scope = ServiceScope.Scoped, Type? injectionType = null, InjectionMode injectionMode = InjectionMode.Concrete) : Attribute
{
    public ServiceScope Scope { get; set; } = scope;
    public Type? InjectionType { get; set; } = injectionType;
    public InjectionMode InjectionMode { get; set; } = injectionMode;
}
