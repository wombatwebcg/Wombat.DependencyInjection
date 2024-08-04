using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Wombat.DependencyInjection;

namespace Wombat.DependencyInjectionTest
{
    /// <summary>
    /// 事务拦截
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class TransactionalAttribute : AOPBaseAttribute
    {

        /// <summary>
        /// 事务拦截
        /// </summary>
        /// <param name="dbContextTypes">数据上下文</param>
        public TransactionalAttribute()
        {

        }

        public override void Before(IAOPContext context)
        {
            Debug.WriteLine("拦截前执行方法");

            context.Invocation.ReturnValue = "123";
            context.JumpOutInternalMethod();
            return;
            //context.Invocation.ReturnValue = 100;
            //Console.WriteLine(111112);

            //return;
        }
        public override void After(IAOPContext context)
        {
            var ssss = context.Invocation.ReturnValue;
            Debug.WriteLine(context.Invocation.ReturnValue);
            Debug.WriteLine("拦截后执行方法");
        }


    }

}