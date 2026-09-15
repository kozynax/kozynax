using System;
using ZXMAK2.Dependency;
using ZXMAK2.Hardware.Circuits.Sound;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.Tests.Infrastructure;

internal sealed class NullUserMessage : IUserMessage
{
    public void ErrorDetails(Exception ex) { }
    public void Error(Exception ex) { }
    public void Error(string fmt, params object[] args) { }
    public void Warning(Exception ex) { }
    public void Warning(string fmt, params object[] args) { }
    public void Info(string fmt, params object[] args) { }
}

internal sealed class NullUserQuery : IUserQuery
{
    public DlgResult Show(string message, string caption, DlgButtonSet buttonSet, DlgIcon icon)
        => DlgResult.Cancel;

    public object ObjectSelector(object[] objArray, string caption) => null!;

    public bool QueryText(string caption, string text, ref string value) => false;

    public bool QueryValue(string caption, string text, string format, ref int value, int min, int max)
        => false;
}

/// <summary>
/// Ensures <see cref="Locator"/> has stubs so engine code can resolve host services headlessly.
/// </summary>
public sealed class EmulatorServicesFixture : IDisposable
{
    private static readonly object Sync = new();
    private static int _refcount;

    public EmulatorServicesFixture()
    {
        lock (Sync)
        {
            if (_refcount++ == 0)
            {
                var resolver = new ResolverSimple();
                resolver.RegisterInstance<IUserMessage>(new NullUserMessage());
                resolver.RegisterInstance<IUserQuery>(new NullUserQuery());
                resolver.RegisterType<IPsgChip, PsgChip>();
                Locator.Init(resolver);
            }
        }
    }

    public void Dispose()
    {
        lock (Sync)
        {
            if (--_refcount == 0)
            {
                Locator.Shutdown();
            }
        }
    }
}
