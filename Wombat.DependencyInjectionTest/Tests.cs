using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wombat.DependencyInjection;
using Xunit;

namespace Wombat.DependencyInjectionTest
{
    public class Tests
    {
        [Fact]
        public void Test1()
        {
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff"));
            ServiceCollection services = new ServiceCollection();
            services.AddAppSettings();
            services.AddServices();
            var serviceProvider = services.BuildServiceProvider();
            serviceProvider.UseCustomServiceProvider();
            var y1 = (Class2)CustomServiceProvider.GetService(nameof(Class2));
           var ssss = y1.Test1;
            var y2 = (Class1)CustomServiceProvider.GetService(nameof(Class1));
           y2.HelloWorld();

            var sss = serviceProvider.GetRequiredService<Class2>();
            var ppp = sss.Test1;
            sss.HelloWorld();
            //var sss1 = serviceProvider.GetRequiredService<IClass1>();
            //sss1.HelloWorld2();
            System.Diagnostics.Debug.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss:fff"));

            //var sss1 = serviceProvider.GetService<Setting>();
            //Console.WriteLine(sss1.Test1);
            //Console.WriteLine(sss.Value1.ToString());
            //Console.WriteLine(sss.Test1);

            //    string text = Console.ReadLine();

            //    //if (text=="1")
            //    //{
            //    //    var sss = serviceProvider.GetRequiredService<Class1>();
            //    //    sss = serviceProvider.GetRequiredService<Class1>();
            //    //    sss.HelloWorld();

            //    //    Class1 class1 = new Class1(serviceProvider);
            //    //    class1.HelloWorld();
            //    //  var sss1 = serviceProvider.GetRequiredService<Class2>();

            //    //    sss1.HelloWorld();
            //    //}
            //    //IOCUtil.GetServiceProvider().GetService<IClass1>().HelloWorld2();



            //    Console.ReadKey();
            //}

        }

    }

}
