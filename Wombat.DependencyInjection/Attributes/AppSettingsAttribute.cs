using System;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Wombat.DependencyInjection
{
    /// <summary>
    /// 配置文件属性注入
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class AppSettingsAttribute : AOPBaseAttribute
    {
        /// <summary>
        /// 配置节点地址地址
        /// </summary>
        private string OptionNodePath { get; }

        /// <summary>
        /// 配置文件属性注入
        /// </summary>
        /// <param name="optionNodePath"></param>
        public AppSettingsAttribute(string optionNodePath)
        {
            OptionNodePath = optionNodePath;
        }

        /// <summary>
        /// 之后
        /// </summary>
        /// <param name="aopContext"></param>
        public override void After(IAOPContext aopContext)
        {
            var name = aopContext.Invocation.Method.Name;
            name = name.Replace("get_", "");
            name = name.Replace("Set_", "");
            var type = aopContext.Invocation.Method.DeclaringType.GetProperty(name).PropertyType;
            var configuration = aopContext.ServiceProvider.GetService<IConfiguration>();
            aopContext.Invocation.ReturnValue = configuration.GetSection(OptionNodePath).Get(type);
        }
    }
}