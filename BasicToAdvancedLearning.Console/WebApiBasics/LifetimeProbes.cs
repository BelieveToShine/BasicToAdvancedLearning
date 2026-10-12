using Microsoft.AspNetCore.Mvc;

namespace BasicToAdvancedLearning.WebApiBasics;

// Three tiny services that differ ONLY in how they are registered. Every new object takes the next number
// from one shared counter, so the numbers in the output reveal when the container created each object.
public abstract class Probe
{
    private static int _created;

    protected Probe()
    {
        Number = ++_created;
    }

    public int Number { get; }

    // Back to zero so every run of the lesson starts the same.
    public static void ResetCounter() => _created = 0;
}

public class SingletonProbe : Probe { }   // registered with AddSingleton
public class ScopedProbe : Probe { }      // registered with AddScoped
public class TransientProbe : Probe { }   // registered with AddTransient

// A second service that needs the same three probes, so one request has TWO places that receive them.
// It is registered as Transient. A service must not outlive what it depends on; a transient one is safe here.
public class ProbeReporter
{
    private readonly SingletonProbe _singleton;
    private readonly ScopedProbe _scoped;
    private readonly TransientProbe _transient;

    public ProbeReporter(SingletonProbe singleton, ScopedProbe scoped, TransientProbe transient)
    {
        _singleton = singleton;
        _scoped = scoped;
        _transient = transient;
    }

    public string Describe() => $"singleton #{_singleton.Number}, scoped #{_scoped.Number}, transient #{_transient.Number}";
}

[ApiController]
[Route("api/lifetimes")]
public class LifetimesController : ControllerBase
{
    private readonly SingletonProbe _singleton;
    private readonly ScopedProbe _scoped;
    private readonly TransientProbe _transient;
    private readonly ProbeReporter _reporter;

    public LifetimesController(SingletonProbe singleton, ScopedProbe scoped, TransientProbe transient, ProbeReporter reporter)
    {
        _singleton = singleton;
        _scoped = scoped;
        _transient = transient;
        _reporter = reporter;
    }

    [HttpGet]
    public string Get()
    {
        string fromController = $"singleton #{_singleton.Number}, scoped #{_scoped.Number}, transient #{_transient.Number}";
        return $"controller got: {fromController} | reporter got: {_reporter.Describe()}";
    }
}
