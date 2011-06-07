﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Gate
{
    using AppAction = Action< // app
        IDictionary<string, object>, // env
        Action< // result
            string, // status
            IDictionary<string, string>, // headers
            Func< // body
                Func< // next
                    ArraySegment<byte>, // data
                    Action, // continuation
                    bool>, // async
                Action<Exception>, // error
                Action, // complete
                Action>>, // cancel
        Action<Exception>>; // error

    public class GateConfigurationLoader : IConfigurationLoader
    {
        public Action<IAppBuilder> Load(string configurationString)
        {
            var typeAndMethod = TypeAndMethodNameForConfiguration(configurationString);

            if (typeAndMethod == null)
                return null;

            var type = typeAndMethod.Item1;
            // default to the "Configuration" method if only the type name was provided
            var methodName = typeAndMethod.Item2 ?? "Configuration";
            var methodInfo = type.GetMethod(methodName);

            return MakeDelegate(type, methodInfo);
        }

        public static Tuple<Type, string> TypeAndMethodNameForConfiguration(string configurationString)
        {
            if (string.IsNullOrWhiteSpace(configurationString))
            {
                configurationString = GetDefaultConfigurationString();
            }

            foreach (var hit in HuntForAssemblies(configurationString))
            {
                var longestPossibleName = hit.Item1; // method or type name
                var assembly = hit.Item2;

                // try the longest 2 possibilities at most (because you can't have a dot in the method name)
                // so, typeName could specify a method or a type. we're looking for a type.
                foreach (var typeName in DotByDot(longestPossibleName).Take(2))
                {
                    var type = assembly.GetType(typeName, false);
                    if (type == null) // must have been a method name (or doesn't exist), next!
                        continue;

                    var methodName = typeName == longestPossibleName
                        ? null
                        : longestPossibleName.Substring(typeName.Length + 1);

                    return new Tuple<Type, string>(type, methodName);
                }
            }
            return null;
        }

        static string GetDefaultConfigurationString()
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
                        return possibleType + ", " + assemblyFullName;
                }
            }
            return null;
        }

        static IEnumerable<Tuple<string, Assembly>> HuntForAssemblies(string configurationString)
        {
            var commaIndex = configurationString.IndexOf(',');
            if (commaIndex >= 0) // assembly is given
            {
                // break the type and assembly apart
                var methodOrTypeName = DotByDot(configurationString.Substring(0, commaIndex)).FirstOrDefault();
                var assemblyName = configurationString.Substring(commaIndex + 1).Trim();
                var assembly = TryAssemblyLoad(assemblyName);
                if (assembly != null)
                    yield return Tuple.Create(methodOrTypeName, assembly);
            }
            else // assembly is inferred from type name
            {
                var methodOrTypeName = DotByDot(configurationString).FirstOrDefault();

                // go through each segment except the first (assuming the last segment is a type name at a minimum))
                foreach (var assemblyName in DotByDot(methodOrTypeName).Skip(1))
                {
                    var assembly = TryAssemblyLoad(assemblyName);
                    if (assembly != null)
                        yield return Tuple.Create(methodOrTypeName, assembly);
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

        static IEnumerable<string> DotByDot(string text)
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

        Action<IAppBuilder> MakeDelegate(Type type, MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                return null;
            }

            if (Matches(methodInfo, typeof (void), typeof (IAppBuilder)))
            {
                var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);
                return builder => methodInfo.Invoke(instance, new[] {builder});
            }

            if (Matches(methodInfo, typeof (AppDelegate)))
            {
                var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);
                return builder => builder.Run((AppDelegate) methodInfo.Invoke(instance, new object[0] {}));
            }

            if (Matches(methodInfo, typeof(AppAction))) {
                var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);
                return builder => builder.Run(((AppAction)methodInfo.Invoke(instance, new object[0] { })).ToDelegate());
            }

            return null;
        }

        bool Matches(MethodInfo methodInfo, Type returnType, params Type[] parameterTypes)
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