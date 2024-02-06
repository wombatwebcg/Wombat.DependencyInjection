using Castle.DynamicProxy;
using System;
using System.Reflection;

namespace Wombat.DependencyInjection
{
    public interface IAOPContext
    {
        IServiceProvider ServiceProvider { get; }

        IInvocation Invocation { get; }
    }
}
