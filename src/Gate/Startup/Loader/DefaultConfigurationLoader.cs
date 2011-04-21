using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gate.Startup.Loader
{
    public class DefaultConfigurationLoader : IConfigurationLoader
    {
        public Action<AppBuilder> Load(string configurationString)
        {
            if (string.IsNullOrWhiteSpace(configurationString))
            {
                return LoadDefault();
            }
            foreach (var hit in HuntForAssemblies(configurationString))
            {
                var longestPossibleName = hit.Item1;
                var assembly = hit.Item2;

                // try the longest 2 possibilities at most (because you can't have a dot in the method name)
                foreach (var typeName in DotByDot(longestPossibleName).Take(2))
                {
                    var type = assembly.GetType(typeName, false);
                    if (type == null)
                        continue;

                    // default to the "Configuration" method if only the type name was provided
                    var methodName = typeName == longestPossibleName
                        ? "Configuration"
                        : longestPossibleName.Substring(typeName.Length + 1);

                    var methodInfo = type.GetMethod(methodName);

                    return MakeDelegate(type, methodInfo);
                }
            }
            return null;
        }

        Action<AppBuilder> LoadDefault()
        {
            var info = AppDomain.CurrentDomain.SetupInformation;
            var assembliesPath = Path.Combine(info.ApplicationBase, info.PrivateBinPath ?? "");

            var files = Directory.GetFiles(assembliesPath, "*.dll")
                .Concat(Directory.GetFiles(assembliesPath, "*.exe"));

            foreach (var file in files)
            {
                var reflectionOnlyAssembly = Assembly.ReflectionOnlyLoadFrom(file);

                var assemblyName = reflectionOnlyAssembly.GetName().Name;
                var assemblyFullName = reflectionOnlyAssembly.FullName;

                foreach (var possibleType in new[] {"Startup", assemblyName + ".Startup"})
                {
                    var startupType = reflectionOnlyAssembly.GetType(possibleType, false);
                    if (startupType != null)
                        return Load(possibleType + ", " + assemblyFullName);
                }
            }
            return null;
        }

        static IEnumerable<Tuple<string, Assembly>> HuntForAssemblies(string configurationString)
        {
            var commaIndex = configurationString.IndexOf(',');
            if (commaIndex >= 0)
            {
                // break the type and assembly apart
                var longestPossibleName = DotByDot(configurationString.Substring(0, commaIndex)).FirstOrDefault();
                var assemblyName = configurationString.Substring(commaIndex + 1).Trim();
                var assembly = TryAssemblyLoad(assemblyName);
                if (assembly != null)
                    yield return Tuple.Create(longestPossibleName, assembly);
            }
            else
            {
                var longestPossibleName = DotByDot(configurationString).FirstOrDefault();

                // go through each segment except the first (assuming the last segment is a class name at a minimum))
                foreach (var assemblyName in DotByDot(longestPossibleName).Skip(1))
                {
                    var assembly = TryAssemblyLoad(assemblyName);
                    if (assembly != null)
                        yield return Tuple.Create(longestPossibleName, assembly);
                }
            }
        }

        static Assembly TryAssemblyLoad(string assemblyName)
        {
            try
            {
                return Assembly.Load(assemblyName);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        public static IEnumerable<string> DotByDot(string text)
        {
            if (text == null)
                yield break;

            text = text.Trim('.');
            for (var length = text.Length;
                length > 0;
                length = text.LastIndexOf('.', length - 1, length - 1))
            {
                yield return text.Substring(0, length);
            }
        }

        static Action<AppBuilder> MakeDelegate(Type type, MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return null;
            }

            if (Matches(methodInfo, typeof (void), typeof (AppBuilder)))
            {
                var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);
                return builder => methodInfo.Invoke(instance, new[] {builder});
            }

            if (Matches(methodInfo, typeof (AppDelegate)))
            {
                var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);
                return builder => builder.Run((AppDelegate) methodInfo.Invoke(instance, new object[0] {}));
            }

            return null;
        }

        static bool Matches(MethodInfo methodInfo, Type returnType, params Type[] parameterTypes)
        {
            if (methodInfo.ReturnType != returnType)
                return false;

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != parameterTypes.Length)
                return false;

            return parameters.Zip(parameterTypes, (pi, t) => pi.ParameterType == t).All(b => b);
        }
    }
}