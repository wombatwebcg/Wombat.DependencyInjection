
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
            //if (assemblies == null)
            //    throw new ArgumentNullException(nameof(assemblies), "The assemblies collection cannot be null.");
            //if (assemblies.Any(a => a == null))
            //    throw new ArgumentException("The assemblies collection cannot contain null elements.");

            NamedServices.Clear();
            serviceCollection.AddTransient<IAsyncInterceptor, AOPInterceptor>();
                foreach (var localServiceLifetime in Enum.GetValues(typeof(ServiceLifetime)))
                {
                    var serviceLifetime = (ServiceLifetime)localServiceLifetime;
                    List<Type> types = assemblies
                        .Where(a => a != null)
                        .SelectMany(t => t?.GetTypes() ?? Array.Empty<Type>())
                        .Where(t => t != null
                                    && t.GetCustomAttributes(typeof(ComponentAttribute), false).Length > 0
                                    && t.GetCustomAttribute<ComponentAttribute>()?.Lifetime == serviceLifetime
                                    && t.IsClass
                                    && !t.IsAbstract)
                        .ToList();

                foreach (var aType in types)
                {
                    try
                    {

                        var interfaces = assemblies
                            .Where(a => a != null)
                            .SelectMany(x => x?.GetTypes() ?? Array.Empty<Type>())
                            .Where(x => x != null && x.IsAssignableFrom(aType) && x.IsInterface)
                            .ToList();

                        var classAopBaseAttributes = aType.GetCustomAttribute<AOPBaseAttribute>() != null;
                        var propertyAopBaseAttributes = aType.GetProperties().Count(w => w.GetCustomAttribute<AOPBaseAttribute>() != null) > 0;
                        var methodAopBaseAttributes = aType.GetMethods().Count(w => w.GetCustomAttribute<AOPBaseAttribute>() != null) > 0;
                        var constructors = aType.GetConstructors()?.FirstOrDefault()?.GetParameters()?.Select(w => w.ParameterType)?.ToArray();
                        var serviceName = aType.GetCustomAttribute<ComponentAttribute>()?.ServiceName;

                        #region 指定接口注入
                        var componentInterface = aType.GetCustomAttribute<ComponentAttribute>()?.Interface;
                        if (componentInterface != null)
                        {
                            var instance = aType.GetCustomAttribute<ComponentAttribute>()?.Instance;
                            if (instance != null)
                            {
                                inject(serviceLifetime, instance, componentInterface, serviceName);
                                if (!classAopBaseAttributes && !propertyAopBaseAttributes && !methodAopBaseAttributes)
                                {
                                    continue;
                                }
                                serviceCollection.Add(new ServiceDescriptor(componentInterface, serviceProvider =>
                                {
                                    var constructorArguments = constructors?.Select(w => serviceProvider.GetService(w)).ToArray();
                                    return _proxyGenerator.CreateInterfaceProxyWithTarget(componentInterface, serviceProvider.GetService(instance), serviceProvider.GetService<IAsyncInterceptor>());
                                }, serviceLifetime));

                                serviceCollection.Add(new ServiceDescriptor(instance, serviceProvider =>
                                {
                                    var constructorArguments = constructors?.Select(w => serviceProvider.GetService(w)).ToArray();
                                    return _proxyGenerator.CreateClassProxy(instance, constructorArguments, serviceProvider.GetService<IAsyncInterceptor>());
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
                                    var constructorArguments = constructors?.Select(w => serviceProvider.GetService(w)).ToArray();
                                    return _proxyGenerator.CreateInterfaceProxyWithTarget(componentInterface, serviceProvider.GetService(aType), serviceProvider.GetService<IAsyncInterceptor>());
                                }, serviceLifetime));

                                serviceCollection.Add(new ServiceDescriptor(aType, serviceProvider =>
                                {
                                    var constructorArguments = constructors?.Select(w => serviceProvider.GetService(w)).ToArray();
                                    return _proxyGenerator.CreateClassProxy(aType, constructorArguments, serviceProvider.GetService<IAsyncInterceptor>());
                                }, serviceLifetime));
                                continue;
                            }
                        }
                        #endregion

                        #region 继承接口注入
                        if (interfaces.Count == 0)
                        {
                            inject(serviceLifetime, aType, serviceName: serviceName);

                            if (!classAopBaseAttributes && !propertyAopBaseAttributes && !methodAopBaseAttributes)
                            {
                                continue;
                            }
                            serviceCollection.Add(new ServiceDescriptor(aType, serviceProvider =>
                            {
                                var constructorArguments = constructors?.Select(w => serviceProvider.GetService(w)).ToArray();
                                return _proxyGenerator.CreateClassProxy(aType, constructorArguments, serviceProvider.GetService<IAsyncInterceptor>());
                            }, serviceLifetime));
                            continue;
                        }
                        bool isInjectNameService = false;
                        foreach (var aInterface in interfaces)
                        {
                            if (serviceName != null && !string.IsNullOrWhiteSpace(serviceName) && !isInjectNameService)
                            {
                                inject(serviceLifetime, aType, aInterface, serviceName: serviceName);
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
                            serviceCollection.Add(new ServiceDescriptor(aInterface, serviceProvider =>
                            {
                                var constructorArguments = constructors?.Select(w => serviceProvider.GetService(w)).ToArray();
                                return _proxyGenerator.CreateInterfaceProxyWithTarget(aInterface, serviceProvider.GetService(aType), serviceProvider.GetService<IAsyncInterceptor>());
                            }, serviceLifetime));

                            serviceCollection.Add(new ServiceDescriptor(aType, serviceProvider =>
                            {
                                var constructorArguments = constructors?.Select(w => serviceProvider.GetService(w)).ToArray();
                                return _proxyGenerator.CreateClassProxy(aType, constructorArguments, serviceProvider.GetService<IAsyncInterceptor>());
                            }, serviceLifetime));
                        }
                        #endregion
                    }

                    catch (Exception ex)
                    {
                        throw new ArgumentException($"{aType}Injection Exception.{ex.InnerException},{ex.StackTrace}");
                    }
                }
            }

            void inject(ServiceLifetime serviceLifetime, Type type, Type typeInterface = null, string serviceName = null)
            {
                try
                {
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
                catch (Exception ex)
                {
                    throw new ArgumentException($"Injection Exception.{serviceLifetime},{type},{typeInterface},{serviceName}");
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
