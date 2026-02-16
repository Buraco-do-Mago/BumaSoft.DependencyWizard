using BumaSoft.DependencyWizard.Services;

namespace BumaSoft.DependencyWizard.SemanticKernel.Plugins;

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class PluginAttribute : Attribute
{
    public ServiceScope Scope { get; set; } = ServiceScope.Scoped;

    public PluginAttribute(ServiceScope scope) => Scope = scope;

    public PluginAttribute()
    {
    }
}
