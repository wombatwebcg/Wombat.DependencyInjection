using Castle.DynamicProxy;
using System;
using System.Reflection;

namespace Wombat.DependencyInjection
{
    public class AOPContext : IAOPContext
    {
        private readonly IInvocation _invocation;
        public AOPContext(IInvocation invocation, IServiceProvider serviceProvider)
        {
            _invocation = invocation;
            ServiceProvider = serviceProvider;
        }
        public IServiceProvider ServiceProvider { get; }

        public IInvocation Invocation => _invocation;
    }
}
