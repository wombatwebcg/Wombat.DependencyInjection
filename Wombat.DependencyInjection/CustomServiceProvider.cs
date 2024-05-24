using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Wombat.DependencyInjection
{
   public static class CustomServiceProvider
    {
        #region  记录存储服务提供者

        internal static IServiceProvider _serviceProvider;
        internal static IConfiguration _configuration;

        /// <summary>
        /// 注册服务提供者
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void UseCustomServiceProvider(this IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 注册服务提供者
        /// </summary>
        /// <param name="serviceProvider"></param>
        public static void UseCustomConfigurationProvider(this IConfiguration configuration)
        {
            _configuration = configuration;
        }



        /// <summary>
        /// 获取服务提供者
        /// </summary>
        /// <returns></returns>
        public static T GetService<T>()
        {
          return  _serviceProvider.GetService<T>();
        }


        /// <summary>
        /// 获取服务提供者
        /// </summary>
        /// <returns></returns>
        public static T GetRequiredService<T>()
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        /// <returns></returns>
        public static object GetRequiredService(Type serviceType)
        {
            return _serviceProvider.GetRequiredService(serviceType);
        }


        /// <summary>
        /// 获取服务提供者
        /// </summary>
        /// <returns></returns>
        public static object GetService(Type serviceType)
        {
            return _serviceProvider.GetService(serviceType);
        }

        /// <summary>
        /// 获取服务提供者
        /// </summary>
        /// <returns></returns>
        public static object GetService(string serviceName)
        {
            if(DependencyInjectionServices.NamedServices.TryGetValue(serviceName,out Type type))
            {
                return GetService(type);

            }
            else
            {
                return null;
            }
        }


        public static IConfiguration GetConfiguration()
        {
            return _configuration;

        }

        public static IServiceProvider GetServiceProvider()
        {
            return _serviceProvider;

        }

        #endregion

    }
}
