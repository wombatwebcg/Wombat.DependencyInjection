using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace Wombat.DependencyInjection
{
    public class AOPInterceptor : IAsyncInterceptor
    {
        private readonly IServiceProvider _serviceProvider;
        public AOPInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }



        /// <summary>
        /// 异步拦截方法。
        /// </summary>
        /// <param name="invocation">拦截上下文。</param>
        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }

        /// <summary>
        /// 内部异步拦截方法。
        /// </summary>
        /// <param name="invocation">拦截上下文。</param>
        private async Task InternalInterceptAsynchronous(IInvocation invocation)
        {
            var aopBaseAttribute = GetAOPBaseAttribute(invocation);

            if (aopBaseAttribute == null)
            {
                invocation.Proceed();
                await AwaitTask(invocation.ReturnValue);
                return;
            }

            var aopContext = new AOPContext(invocation, _serviceProvider);
            aopBaseAttribute.Before(aopContext);

            try
            {
                invocation.Proceed();
                await AwaitTask(invocation.ReturnValue);
                aopBaseAttribute.After(aopContext);
            }
            catch (Exception exception)
            {
                HandleException(aopBaseAttribute, aopContext, exception);
            }
        }

        private async Task AwaitTask(object task)
        {
            if (task is Task awaitableTask)
            {
                await awaitableTask;
            }
        }

        /// <summary>
        /// 异步拦截方法。
        /// </summary>
        /// <typeparam name="TResult">返回结果类型。</typeparam>
        /// <param name="invocation">拦截上下文。</param>
        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {

            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }

        /// <summary>
        /// 内部异步拦截方法。
        /// </summary>
        /// <typeparam name="TResult">返回结果类型。</typeparam>
        /// <param name="invocation">拦截上下文。</param>
        private async Task<TResult> InternalInterceptAsynchronous<TResult>(IInvocation invocation)
        {
            var aopBaseAttribute = GetAOPBaseAttribute(invocation);

            if (aopBaseAttribute == null)
            {
                invocation.Proceed();
                return await AwaitTask((Task<TResult>)invocation.ReturnValue);
            }

            var aopContext = new AOPContext(invocation, _serviceProvider);
            aopBaseAttribute.Before<TResult>(aopContext);

            if (invocation.ReturnValue != null)
            {
                return await AwaitTask((Task<TResult>)invocation.ReturnValue);
            }

            try
            {
                invocation.Proceed();
                var result = await AwaitTask((Task<TResult>)invocation.ReturnValue);
                aopBaseAttribute.After(aopContext, result);
                return result;
            }
            catch (Exception exception)
            {
                HandleException(aopBaseAttribute, aopContext, exception);
            }

            return default; // 根据实际情况返回默认值
        }

        private async Task<TResult> AwaitTask<TResult>(Task<TResult> task)
        {
            return await task;
        }

        /// <summary>
        /// 同步拦截方法。
        /// </summary>
        /// <param name="invocation">拦截上下文。</param>
        public void InterceptSynchronous(IInvocation invocation)
        {

            var aopBaseAttribute = GetAOPBaseAttribute(invocation);

            if (aopBaseAttribute == null)
            {
                invocation.Proceed();
                return;
            }

            var aopContext = new AOPContext(invocation, _serviceProvider);
            // 执行调用之前的函数
            aopBaseAttribute.Before(aopContext);

            #region 检查返回值有无 如果有了则不执行函数直接返回数据
            var result = invocation.ReturnValue;
            if (result != null)
            {
                aopBaseAttribute.After(aopContext);
                return;
            }
            #endregion
            try
            {
                invocation.Proceed();
                aopBaseAttribute.After(aopContext);
            }
            catch (Exception exception)
            {
                HandleException(aopBaseAttribute, aopContext, exception);
            }
        }

        private void HandleException(AOPBaseAttribute aopBaseAttribute, AOPContext aopContext, Exception exception)
        {
            if (aopBaseAttribute.ExceptionEvent != null)
            {
                aopBaseAttribute.ExceptionEvent(aopContext, exception);
            }
            else
            {
                throw exception; // 或者其他异常处理
            }
        }
        /// <summary>
        /// 获取AOP基础特性。
        /// </summary>
        /// <param name="invocation">拦截上下文。</param>
        /// <returns>AOP基础特性。</returns>
        private AOPBaseAttribute GetAOPBaseAttribute(IInvocation invocation)
        {

            var aopBaseAttribute = invocation.Method.GetCustomAttribute<AOPBaseAttribute>();

            // 从类上获取标记
            if (aopBaseAttribute == null)
            {
                aopBaseAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<AOPBaseAttribute>();
            }

            var name = invocation.MethodInvocationTarget.Name;
            // 从属性上获取标记
            if (aopBaseAttribute == null && (name.StartsWith("get_") || name.StartsWith("set_")))
            {
                name = name.Replace("get_", "");
                name = name.Replace("Set_", "");
                var propertyInfo = invocation.Method.DeclaringType.GetProperty(name);
                if (propertyInfo != null)
                {
                    aopBaseAttribute = propertyInfo.GetCustomAttribute<AOPBaseAttribute>();
                }
            }

            return aopBaseAttribute;

        }
    }
}
