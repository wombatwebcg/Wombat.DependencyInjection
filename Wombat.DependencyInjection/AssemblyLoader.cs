using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Wombat.Extensions.DataTypeExtensions;

namespace Wombat
{
   public class AssemblyLoader
    {
        /// <summary>
        /// 获取当前工程下所有要用到的dll
        /// </summary>
        /// <returns></returns>
        public static List<Assembly> GetAssemblyList(params string[] assemblyNames)
        {
            try
            {

                List<Assembly> result = new List<Assembly>();
                List<string> ignoreList = new List<string> {};

                //IEnumerable<Assembly> source = from w in GetLoadedAssemblies()
                //    .Where(w => !ignoreList.Any(s => (w.GetName().Name ?? "").Contains(s)))
                //    .Where(w => !result.Any(s => (w.GetName().Name ?? "").Contains(s.GetName().Name ?? "")))
                //                               select w;
                IEnumerable<Assembly> source = from w in GetLoadedAssemblies()
                                               where !ignoreList.Any(s => (w.GetName().Name ?? "").Contains(s))
                                               where !result.Any(s => (w.GetName().Name ?? "").Contains(s.GetName().Name ?? ""))
                                               select w;

                List<Assembly> collection = source.ToList();

                if (assemblyNames != null && assemblyNames.Length != 0)
                {
                    collection = source.Where(w => assemblyNames.Any(s => (w.GetName().Name ?? "").Contains(s))).ToList();
                }

                result.AddRange(collection);

                Assembly entryAssembly = Assembly.GetEntryAssembly();
                if (entryAssembly != null && !result.Any(w => (entryAssembly.GetName().Name ?? "").Contains(w.GetName().Name ?? "")) && !ignoreList.Any(w => (entryAssembly.GetName().Name ?? "").Contains(w)))
                {
                    result.Add(entryAssembly);
                }

                IEnumerable<AssemblyName> source2 = entryAssembly.GetReferencedAssemblies()
                    .Where(w => !ignoreList.Any(s => (w.Name ?? "").Contains(s)))
                    .Where(w => !result.Any(s => (w.Name ?? "").Contains(s.GetName().Name ?? "")));

                List<Assembly> list = (assemblyNames == null || assemblyNames.Length == 0)
                    ? source2.Select(w => LoadAssemblyByName(w.Name)).ToList()
                    : source2.Where(w => assemblyNames.Any(s => (w.Name ?? "").Contains(s))).Select(w => LoadAssemblyByName(w.Name)).ToList();

                result.AddRange(list);

                IEnumerable<string> enumerable = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory)
                    .Where(w => w.EndsWith(".dll"))
                    .Where(w => !ignoreList.Any(s => Path.GetFileName(w)?.Contains(s) ?? false))
                    .Where(w => !result.Any(s => s?.Location == w));

                if (assemblyNames != null && assemblyNames.Length != 0)
                {
                    enumerable = enumerable.Where(w => assemblyNames.Any(s => Path.GetFileName(w)?.Contains(s) ?? false));
                }

                foreach (string item in enumerable)
                {
                    if (!File.Exists(item))
                    {
                        continue;
                    }

                    try
                    {
                        Assembly assembly = LoadAssemblyByName(item);
                        if (!result.Any(w => w?.FullName == assembly?.FullName))
                        {
                            result.Add(assembly);
                        }

                    }
                    catch (Exception)
                    {

                    }
                }

                return result;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// 获取当前工程下所有要用到的dll
        /// </summary>
        /// <returns></returns>
       public static List<Assembly> GetAssemblyList()
        {
            List<Assembly> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
            var loadedPaths = loadedAssemblies.Select(
                a =>
                {
                    // prevent exception accessing Location
                    try
                    {
                        return a.Location;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            ).ToArray();
            var referencedPaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll");
            var toLoad = referencedPaths.Where(r => !loadedPaths.Contains(r, StringComparer.InvariantCultureIgnoreCase)).ToList();
            toLoad.ForEach(
                path =>
                {
                    // prevent exception loading some assembly
                    try
                    {
                        loadedAssemblies.Add(AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path)));
                    }
                    catch (Exception)
                    {
                        ; // DO NOTHING
                    }
                }
            );

            // prevent loading of dynamic assembly, autofac doesn't support dynamic assembly
            loadedAssemblies.RemoveAll(i => i.IsDynamic);

            return loadedAssemblies;
        }




        private static Assembly LoadAssemblyByName(string assemblyName)
        {
            try
            {
                return Assembly.Load(new AssemblyName(assemblyName));
            }
            catch (Exception)
            {
                // 处理加载程序集失败的异常
                return null;
            }
        }

        private static IEnumerable<Assembly> GetLoadedAssemblies()
        {
            var loadedAssemblies = new List<Assembly>();
            var loadedAppDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly.GetEntryAssembly().GetReferencedAssemblies().ForEach((name) => { AddIfNotAlreadyAdded(loadedAssemblies, Assembly.Load(name)); }) ;
            foreach (var assembly in loadedAppDomainAssemblies)
            {
                AddIfNotAlreadyAdded(loadedAssemblies, assembly);
            }

            var entryAssembly = Assembly.GetEntryAssembly();
            var executingAssembly = Assembly.GetExecutingAssembly();

            // 添加入口程序集和当前执行程序集
            AddIfNotAlreadyAdded(loadedAssemblies, entryAssembly);
            AddIfNotAlreadyAdded(loadedAssemblies, executingAssembly);
            loadedAssemblies.RemoveAll(i => i.IsDynamic);

            return loadedAssemblies;
        }

        private static void AddIfNotNull(List<Assembly> assemblies, Assembly assembly)
        {
            if (assembly != null)
            {
                assemblies.Add(assembly);
            }
        }

        private static void AddIfNotAlreadyAdded(List<Assembly> assemblies, Assembly assembly)
        {
            if (assembly != null && !assemblies.Contains(assembly))
            {
                assemblies.Add(assembly);
            }
        }

    }
}
