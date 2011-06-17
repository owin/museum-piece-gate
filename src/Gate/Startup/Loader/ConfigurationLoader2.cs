using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gate.Startup.Loader
{
    public class ConfigurationLoader2 : IConfigurationLoader
    {
        class ConfigStringParserDelegate : IConfigStringParserDelegate
        {
            public string Identifier = "", Assembly = "";

            public void OnIdentifier(string identifier)
            {
                Identifier += identifier;
            }

            public void OnAssembly(string assembly)
            {
                Assembly += assembly;
            }
        }

        ConfigStringParserDelegate parserDelegate;
        ConfigStringParser parser;

        public ConfigurationLoader2()
        {
            parserDelegate = new ConfigStringParserDelegate();
            parser = new ConfigStringParser(parserDelegate);
        }


        public Action<IAppBuilder> Load(string configurationString)
        {
            if (configurationString == null)
                configurationString = "";

            var stringData = new ArraySegment<byte>(Encoding.UTF8.GetBytes(configurationString));

            int parsed = parser.Execute(stringData);

            if (parsed != stringData.Count)
            {
                var sb = new StringBuilder();

                while (sb.Length < parsed)
                    sb.Append(' ');

                sb.Append('^');

                throw new Exception("Could not parse configuration string.\nError at character " + parsed + "\n" +
                configurationString + "\n" + sb.ToString());
            }

            if (0 != parser.Execute(default(ArraySegment<byte>)))
                throw new Exception("Error parsing eof");

            Console.WriteLine("Parsed identifier: " + parserDelegate.Identifier);
            Console.WriteLine("Parsed assembly: " + parserDelegate.Assembly);

            string defaultTypeName = "Startup";
            string defaultMethodName = "Configuration";

            var possibleAssemblies = string.IsNullOrEmpty(parserDelegate.Assembly) ?
                GetAssemblyNames()
                    .Concat(new[] { parserDelegate.Identifier }.SelectMany(s => new[] { s, s + ".exe", s + ".dll" }))
                :
                new[] { parserDelegate.Assembly };

            IEnumerable<Func<Assembly, string>> possibleIdentifiers =
                new Func<Assembly, string>[] { 
                    a => parserDelegate.Identifier, 
                    a => parserDelegate.Identifier + "." + defaultMethodName,
                    a => a.GetName().Name + "." + parserDelegate.Identifier, 
                    a => a.GetName().Name + "." + parserDelegate.Identifier + "." + defaultMethodName };

            if (string.IsNullOrEmpty(parserDelegate.Identifier))
                possibleIdentifiers.Concat(new Func<Assembly, string>[] {
                    a => defaultTypeName, 
                    a => defaultTypeName + "." + defaultMethodName,
                    a => a.GetName().Name + "." + defaultTypeName, 
                    a => a.GetName().Name + "." + defaultTypeName + "." + defaultMethodName
                });

            var typeAndMethod = FindTypeAndMethod(possibleIdentifiers, possibleAssemblies);

            if (typeAndMethod == null)
                return null;

            return MakeDelegate(typeAndMethod.Item1, typeAndMethod.Item2);
        }

        Tuple<Type, MethodInfo> FindTypeAndMethod(IEnumerable<Func<Assembly, string>> possibleIdentifiers, IEnumerable<string> possibleAssemblies)
        {
            var assemblies = possibleAssemblies
                .Select(name => TryReflectionOnlyLoad(name)).Where(a => a != null)
                .Select(a => TryAssemblyLoad(a.GetName()));

            foreach (var asm in assemblies)
            {
                foreach (var identifier in possibleIdentifiers.Select(i => i(asm)))
                {
                    // identifier could refer to a type or method.
                    foreach (var possibleType in DotByDot(identifier).Take(2).Select(id => asm.GetType(id, false)))
                    {
                        if (possibleType == null)
                            continue;

                        if (identifier == possibleType.FullName)
                            continue;

                        var possibleMethod = possibleType.GetMethod(identifier.Substring(possibleType.FullName.Length + 1));

                        if (possibleMethod == null)
                            continue;

                        return Tuple.Create(possibleType, possibleMethod);
                    }
                }
            }

            return null;
        }

        public IEnumerable<string> GetAssemblyNames()
        {
            var info = AppDomain.CurrentDomain.SetupInformation;
            var assembliesPath = Path.Combine(info.ApplicationBase, info.PrivateBinPath ?? "");

            return Directory.GetFiles(assembliesPath, "*.dll")
                .Concat(Directory.GetFiles(assembliesPath, "*.exe"));
        }

        Assembly TryReflectionOnlyLoad(string name)
        {
            try
            {
                return Assembly.ReflectionOnlyLoadFrom(name);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        Assembly TryAssemblyLoad(AssemblyName assemblyName)
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

        internal static IEnumerable<string> DotByDot(string text)
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

            if (Matches(methodInfo, typeof(void), typeof(IAppBuilder)))
            {
                var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);
                return builder => methodInfo.Invoke(instance, new[] { builder });
            }

            if (Matches(methodInfo, typeof(AppDelegate)))
            {
                var instance = methodInfo.IsStatic ? null : Activator.CreateInstance(type);
                return builder => builder.Run((AppDelegate)methodInfo.Invoke(instance, new object[0] { }));
            }

            if (Matches(methodInfo, typeof(AppAction)))
            {
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
