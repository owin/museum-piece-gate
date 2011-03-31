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
            // normalize away leading/trailing dots and whatnot
            var longestPossibleName = DotByDot(configurationString).FirstOrDefault();

            // go through each segment except the first (assuming the last segment is a class name at a minimum)
            foreach (var assemblyName in DotByDot(longestPossibleName).Skip(1))
            {
                var assembly = TryAssemblyLoad(assemblyName);
                if (assembly == null)
                    continue;

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

            if (methodInfo.ReturnType != typeof (void))
            {
                return null;
            }

            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 1 ||
                parameters[0].ParameterType != typeof (AppBuilder))
            {
                return null;
            }


            var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);

            return builder => methodInfo.Invoke(instance, new[] {builder});
        }
    }
}