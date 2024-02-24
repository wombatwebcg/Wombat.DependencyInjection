using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.DependencyInjection;

namespace Wombat.DependencyInjectionTest
{

    [Component(Lifetime = ServiceLifetime.Transient)]
    public class Class2 
    {

        private readonly IClass _serviceProvider;

        public Class2(IClass serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var ssss = Test1;
        }
        [AppSettings("Test:Value1")]
        public virtual double Value1 { get; }


        [AppSettings("Test:Name")]
        public virtual string Test1 { get; }




        [Transactional]
        public virtual void HelloWorld()
        {
            Console.WriteLine("执行");
        }



    }
}
