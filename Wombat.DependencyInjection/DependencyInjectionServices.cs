
using Castle.DynamicProxy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Wombat.DependencyInjection
{
    /// <summary>
    /// IOC 依赖注入服务
    /// </summary>
    public static class DependencyInjectionServices
    {
        private readonly static ProxyGenerator _proxyGenerator;

        static DependencyInjectionServices()
        {
            if (_proxyGenerator == null)
            {
                _proxyGenerator = new ProxyGenerator();
            }

        }




        #region 动态服务注册


        public static IConfiguration AddAppSettings(this IServiceCollection serviceCollection)
        {
            // 注入配置
            IConfigurationBuilder configurationBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true, reloadOnChange: false);
            IConfiguration configuration = configurationBuilder.Build();
            serviceCollection.AddSingleton(configuration);
            return configuration;
        }


        /// <summary>
        /// 扫描服务 自动注入服务
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="assemblyFilter"></param>
        public static void AddServices(this IServiceCollection serviceCollection, params string[] assemblyNames)
        {
            IEnumerable<Assembly> assemblies = default;

            if (assemblyNames.Length == 0)
            {
                assemblies = AssemblyLoader.GetAssemblyList();

            }
            else
            {
                assemblies = AssemblyLoader.GetAssemblyList(assemblyNames);
            }

            if (assemblies == null) return;

            // 服务自动注册
            ScanComponent(serviceCollection, assemblies);
        }












        internal static Dictionary<string, Type> NamedServices = new Dictionary<string, Type>();



        /// <summary>
        /// 服务自动注册
        /// </summary>
        private static void ScanComponent(this IServiceCollection serviceCollection, IEnumerable<Assembly> assemblies)
        {
            NamedServices.Clear();
            serviceCollection.AddTransient<IAsyncInterceptor, AOPInterceptor>();
            try
            {
                foreach (var localServiceLifetime in Enum.GetValues(typeof(ServiceLifetime)))
                {
                    var serviceLifetime = (ServiceLifetime)localServiceLifetime;
                    List<Type> types = assemblies.SelectMany(t => t.GetTypes()).Where(t => t.GetCustomAttributes(typeof(ComponentAttribute), false).Length > 0 && t.GetCustomAttribute<ComponentAttribute>()?.Lifetime == (ServiceLifetime)localServiceLifetime && t.IsClass && !t.IsAbstract).ToList();
                    foreach (var aType in types)
                    {
                        var interfaces = assemblies.SelectMany(x => x.GetTypes()).ToArray().Where(x => x.IsAssignableFrom(aType) && x.IsInterface).ToList();
                        var classAopBaseAttributes = aType.GetCustomAttribute<AOPBaseAttribute>() != null;
                        var propertyAopBaseAttributes = aType.GetProperties().Count(w => w.GetCustomAttribute<AOPBaseAttribute>() != null) > 0;
                        var methodAopBaseAttributes = aType.GetMethods().Count(w => w.GetCustomAttribute<AOPBaseAttribute>() != null) > 0;
                        var constructors = aType.GetConstructors()?.FirstOrDefault()?.GetParameters()?.Select(w => w.ParameterType)?.ToArray();
                        var serviceName = aType.GetCustomAttribute<ComponentAttribute>()?.ServiceName;

                        #region 指定接口注入
                        var componentInterface = aType.GetCustomAttribute<ComponentAttribute>().Interface;
                        if (componentInterface != null)
                        {
                            var instacne = aType.GetCustomAttribute<ComponentAttribute>().Instance;
                            if (instacne != null)
                            {
                                inject(serviceLifetime, instacne, componentInterface, serviceName);
                                if (!classAopBaseAttributes && !propertyAopBaseAttributes && !methodAopBaseAttributes)
                                {
                                    continue;
                                }
                                serviceCollection.Add(new ServiceDescriptor(componentInterface, serviceProvider =>
                                {
                                    var constructorArguments = constructors.Select(w => serviceProvider.GetService(w)).ToArray();
                                    return _proxyGenerator.CreateInterfaceProxyWithTarget(componentInterface, serviceProvider.GetService(instacne), serviceProvider.GetService<IAsyncInterceptor>());
                                }, serviceLifetime));

                                serviceCollection.Add(new ServiceDescriptor(instacne, serviceProvider =>
                                {
                                    var constructorArguments = constructors.Select(w => serviceProvider.GetService(w)).ToArray();
                                    return _proxyGenerator.CreateClassProxy(instacne, constructorArguments, serviceProvider.GetService<IAsyncInterceptor>());
                                }, serviceLifetime));
                                continue;
                            }
                            else
                            {
                                inject(serviceLifetime, aType, componentInterface, serviceName);
                                if (!classAopBaseAttributes && !propertyAopBaseAttributes && !methodAopBaseAttributes)
                                {
                                    continue;
                                }
                                serviceCollection.Add(new ServiceDescriptor(componentInterface, serviceProvider =>
                                {
                                    var constructorArguments = constructors.Select(w => serviceProvider.GetService(w)).ToArray();
                                    return _proxyGenerator.CreateInterfaceProxyWithTarget(componentInterface, serviceProvider.GetService(aType), serviceProvider.GetService<IAsyncInterceptor>());
                                }, serviceLifetime));

                                serviceCollection.Add(new ServiceDescriptor(aType, serviceProvider =>
                                {
                                    var constructorArguments = constructors.Select(w => serviceProvider.GetService(w)).ToArray();
                                    return _proxyGenerator.CreateClassProxy(aType, constructorArguments, serviceProvider.GetService<IAsyncInterceptor>());
                                }, serviceLifetime));
                                continue;
                            }
                        }
                        #endregion

                        #region 继承接口注入
                        if (interfaces.Count == 0)
                        {
                            inject(serviceLifetime, aType,serviceName: serviceName);

                            if (!classAopBaseAttributes && !propertyAopBaseAttributes && !methodAopBaseAttributes)
                            {
                                continue;
                            }
                            //injectProxy(serviceLifetime, aType);
                            serviceCollection.Add(new ServiceDescriptor(aType, serviceProvider =>
                            {
                                var constructorArguments = constructors.Select(w => serviceProvider.GetService(w)).ToArray();
                                return _proxyGenerator.CreateClassProxy(aType, constructorArguments, serviceProvider.GetService<IAsyncInterceptor>());
                                //return _proxyGenerator.CreateClassProxyWithTarget(aType, serviceProvider.GetService(aType), castleInterceptor);
                            }, serviceLifetime));
                            continue;
                        }
                        bool isInjectNameService = false;
                        foreach (var aInterface in interfaces)
                        {
                            if(serviceName != null && !string.IsNullOrWhiteSpace(serviceName) && !isInjectNameService)
                            {
                                inject(serviceLifetime, aType, aInterface,serviceName: serviceName);
                                isInjectNameService = true;

                            }
                            else
                            {
                                inject(serviceLifetime, aType, aInterface);

                            }
                            if (!classAopBaseAttributes && !propertyAopBaseAttributes && !methodAopBaseAttributes)
                            {
                                continue;
                            }
                            //注入AOP
                            serviceCollection.Add(new ServiceDescriptor(aInterface, serviceProvider =>
                            {
                                var constructorArguments = constructors.Select(w => serviceProvider.GetService(w)).ToArray();
                                return _proxyGenerator.CreateInterfaceProxyWithTarget(aInterface, serviceProvider.GetService(aType), serviceProvider.GetService<IAsyncInterceptor>());
                            }, serviceLifetime));

                            serviceCollection.Add(new ServiceDescriptor(aType, serviceProvider =>
                            {
                                var constructorArguments = constructors.Select(w => serviceProvider.GetService(w)).ToArray();
                                return _proxyGenerator.CreateClassProxy(aType, constructorArguments, serviceProvider.GetService<IAsyncInterceptor>());
                            }, serviceLifetime));




                        }
                        #endregion
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Injection Exception.{ex.InnerException},{ex.StackTrace}");
            }
            void inject(ServiceLifetime serviceLifetime, Type type, Type typeInterface = null,string serviceName = null)
            {

                //服务非继承自接口的直接注入
                switch (serviceLifetime)
                {
                    case ServiceLifetime.Singleton:
                        serviceCollection.AddSingleton(type);
                        break;
                    case ServiceLifetime.Scoped:
                        serviceCollection.AddScoped(type);
                        break;
                    case ServiceLifetime.Transient:
                        serviceCollection.AddTransient(type);
                        break;
                }
                if (typeInterface != null)
                {
                    //服务继承自接口的和接口一起注入
                    switch (serviceLifetime)
                    {
                        case ServiceLifetime.Singleton:
                            serviceCollection.AddSingleton(typeInterface, type);
                            break;
                        case ServiceLifetime.Scoped:
                            serviceCollection.AddScoped(typeInterface, type);
                            break;
                        case ServiceLifetime.Transient:
                            serviceCollection.AddTransient(typeInterface, type);
                            break;
                    }
                }
                if (serviceName != null && !string.IsNullOrWhiteSpace(serviceName))
                {
                    if (!NamedServices.TryGetValue(serviceName, out _))
                    {
                        NamedServices.Add(serviceName, type);
                    }
                    else
                    {
                        throw new ArgumentException($"A service with the name '{serviceName}' is already registered.");
                    }
                }

            }
        }


        #endregion

        public static List<TypeInfo> GetTypesAssignableTo(this Assembly assembly, Type compareType)
        {
            var typeInfoList = assembly.DefinedTypes.Where(x => x.IsClass
                                && !x.IsAbstract
                                && x != compareType
                                && x.GetInterfaces()
                                        .Any(i => i.IsGenericType
                                                && i.GetGenericTypeDefinition() == compareType))?.ToList();

            return typeInfoList;
        }


    }
}
