using System;
using System.Threading.Tasks;

namespace Wombat.DependencyInjection
{
    /// <summary>
    /// 属性注入实例
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class AutowiredAttribute : AOPBaseAttribute
    {



        /// <summary>
        /// 之后
        /// </summary>
        /// <param name="aopContext"></param>
        public override void After(IAOPContext aopContext)
        {
            if (aopContext.Invocation.ReturnValue != null)
            {
                return;

            }
            var name = aopContext.Invocation.Method.Name;
            name = name.Replace("get_", "");
            name = name.Replace("Set_", "");
            var type = aopContext.Invocation.Method.DeclaringType.GetProperty(name).PropertyType;
            var service = aopContext.ServiceProvider.GetService(type);
            aopContext.Invocation.ReturnValue = service;
            return ;
        }

    }
}