using System;
using System.Threading.Tasks;

namespace Wombat.DependencyInjection
{
    /// <summary>
    /// AOP基类
    /// 注:不支持控制器,需要定义接口并实现接口,自定义AOP特性放到接口实现类上
    /// </summary>
    /// 
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public abstract class AOPBaseAttribute : Attribute
    {
        /// <summary>
        /// 函数执行异常事件
        /// </summary>
        public Action<IAOPContext, Exception> ExceptionEvent { get; set; }

        /// <summary>
        /// 函数执行前
        /// </summary>
        /// <param name="aopContext"></param>
        public virtual void Before(IAOPContext aopContext)
        {

        }

       

        /// <summary>
        /// 函数执行后
        /// </summary>
        /// <param name="aopContext"></param>
        public virtual void After(IAOPContext aopContext)
        {

        }

        /// <summary>
        /// 函数执行前 异步函数 带有泛型 只有 Task《TResult》 返回类型才触发
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="aopContext"></param>
        public virtual void Before<TResult>(IAOPContext aopContext)
        {

        }

        /// <summary>
        /// 函数执行后 异步函数 带有泛型 只有 Task《TResult》 返回类型才触发
        /// </summary>
        /// <param name="aopContext"></param>
        /// <param name="result"></param>
        /// <typeparam name="TResult"></typeparam>
        public virtual void After<TResult>(IAOPContext aopContext, TResult result)
        {

        }
    }
}
