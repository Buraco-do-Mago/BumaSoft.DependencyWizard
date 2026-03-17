using BumaSoft.DependencyWizard.Services;

namespace BumaSoft.DependencyWizard.Tests.ValidServices;

public class BaseService
{
    private Guid Something { get; set; } = Guid.NewGuid();

    public Guid ReturnSomething() => Something;
}

[Service(ServiceScope.Transient)]
public class TransientService : BaseService;

[Service(ServiceScope.Scoped)]
public class ScopedService : BaseService;

[Service(ServiceScope.Singleton)]
public class SingletonService : BaseService;

public interface IAbstractionService;

[Service(ServiceScope.Scoped, injectionType: typeof(IAbstractionService), injectionMode: InjectionMode.Abstraction)]
public class AbstractionService : BaseService, IAbstractionService;

public interface IBothService;

[Service(ServiceScope.Scoped, injectionType: typeof(IBothService), injectionMode: InjectionMode.Both)]
public class BothService : BaseService, IBothService;
