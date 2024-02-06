using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Wombat.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ComponentAttribute : Attribute
    {
        /// <summary>
        /// 注册单个的类型
        /// </summary>
        public Type Interface { get; }


        /// <summary>
        /// 注册单个的类型
        /// </summary>
        public Type Instance { get; }

        /// <summary>
        /// 作用域
        /// </summary>
        public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Scoped;


        /// <summary>
        /// 默认注入
        /// </summary>
        public ComponentAttribute()
        {

        }

        /// <summary>
        /// 指定接口注入
        /// </summary>
        /// <param name="接口"></param>
        public ComponentAttribute(Type interfaces)
        {
            Interface = interfaces;
        }

        /// <summary>
        /// 指定接口、实例注入
        /// </summary>
        /// <param name="接口"></param>
        /// <param name="实例"></param>
        public ComponentAttribute(Type interfaces, Type instances)
        {
            Interface = interfaces;
            Instance = instances;
        }

    }
}
