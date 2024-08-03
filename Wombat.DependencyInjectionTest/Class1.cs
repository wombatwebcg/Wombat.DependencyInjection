using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.DependencyInjection;

namespace Wombat.DependencyInjectionTest
{
    [Component(Lifetime = ServiceLifetime.Transient,ServiceName = nameof(Class1))]

    public class Class1
    {

        private readonly IServiceProvider _serviceProvider;

        public Class1(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [AppSettings("Test:Value1")]
        public virtual double Value1 { get; }


        [AppSettings("Test:Name")]
        public virtual string Test1 { get; }

        [Transactional]
        public virtual void HelloWorld()
        {
            Debug.WriteLine("class1拦截前内部1");
        }

        [Transactional]
        public virtual void HelloWorld2()
        {
            Debug.WriteLine("class1拦截前内部1");
        }
    }
}
