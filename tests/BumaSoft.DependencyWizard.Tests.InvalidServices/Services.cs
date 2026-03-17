using BumaSoft.DependencyWizard.Services;

namespace BumaSoft.DependencyWizard.Tests.InvalidServices;

public class BaseService
{
    private Guid Something { get; set; } = Guid.NewGuid();

    public Guid ReturnSomething() => Something;
}

[Service(ServiceScope.Scoped, injectionMode: InjectionMode.Abstraction)]
public class InvalidAbstractionService : BaseService;

[Service(ServiceScope.Scoped, injectionType: typeof(BaseService), injectionMode: InjectionMode.Concrete)]
public class InvalidConcreteService : BaseService;
