using System;
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

        // 异步无返回值的拦截
        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous(invocation);
        }

        // 异步无返回值的内部处理
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
                if (!aopContext.IsJumpOutInternalMethod)
                {
                    invocation.Proceed();
                }
                await AwaitTask(invocation.ReturnValue);
                aopBaseAttribute.After(aopContext);
            }
            catch (Exception exception)
            {
                HandleException(aopBaseAttribute, aopContext, exception);
            }
        }

        // 异步有返回值的拦截
        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = InternalInterceptAsynchronous<TResult>(invocation);
        }

        // 异步有返回值的内部处理
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

            try
            {
                if (!aopContext.IsJumpOutInternalMethod)
                {
                    invocation.Proceed();
                }
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

        // 同步拦截
        public void InterceptSynchronous(IInvocation invocation)
        {
            var aopBaseAttribute = GetAOPBaseAttribute(invocation);

            if (aopBaseAttribute == null)
            {
                invocation.Proceed();
                return;
            }

            var aopContext = new AOPContext(invocation, _serviceProvider);
            aopBaseAttribute.Before(aopContext);

            try
            {
                if (!aopContext.IsJumpOutInternalMethod)
                {
                    invocation.Proceed();
                }
                aopBaseAttribute.After(aopContext);
            }
            catch (Exception exception)
            {
                HandleException(aopBaseAttribute, aopContext, exception);
            }
        }

        // 处理Task的等待
        private async Task AwaitTask(object task)
        {
            if (task is Task awaitableTask)
            {
                await awaitableTask;
            }
        }

        // 处理Task<TResult>的等待
        private async Task<TResult> AwaitTask<TResult>(Task<TResult> task)
        {
            return await task;
        }

        // 处理异常
        private void HandleException(AOPBaseAttribute aopBaseAttribute, AOPContext aopContext, Exception exception)
        {
            if (aopBaseAttribute.ExceptionEvent != null)
            {
                aopBaseAttribute.ExceptionEvent(aopContext, exception);
            }
            else
            {
                throw exception;
            }
        }

        // 获取AOP基础特性
        private AOPBaseAttribute GetAOPBaseAttribute(IInvocation invocation)
        {
            var aopBaseAttribute = invocation.Method.GetCustomAttribute<AOPBaseAttribute>();

            // 从类上获取标记
            if (aopBaseAttribute == null)
            {
                aopBaseAttribute = invocation.MethodInvocationTarget.GetCustomAttribute<AOPBaseAttribute>();
            }

            // 从属性上获取标记
            if (aopBaseAttribute == null && invocation.MethodInvocationTarget.Name.StartsWith("get_") || invocation.MethodInvocationTarget.Name.StartsWith("set_"))
            {
                var propertyName = invocation.MethodInvocationTarget.Name.Substring(4);
                var propertyInfo = invocation.Method.DeclaringType.GetProperty(propertyName);
                if (propertyInfo != null)
                {
                    aopBaseAttribute = propertyInfo.GetCustomAttribute<AOPBaseAttribute>();
                }
            }

            return aopBaseAttribute;
        }
    }
}
