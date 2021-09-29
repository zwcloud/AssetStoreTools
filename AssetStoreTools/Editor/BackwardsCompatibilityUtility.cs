using System;
using System.Collections.Generic;
using System.Reflection;

namespace AssetStoreTools
{
    public static class BackwardsCompatibilityUtility
    {
        public static MethodInfo GetMethodInfo(List<string> methods, Type[] parametersType = null)
        {
            MethodInfo methodInfo = null;
            foreach (string text in methods)
            {
                string[] array = text.Split(new char[]
                {
                '.'
                });
                Assembly assembly = Assembly.Load(array[0]);
                string name = string.Format("{0}.{1}", array[0], array[1]);
                string name2 = array[2];
                Type type = assembly.GetType(name);
                if (type != null)
                {
                    if (parametersType == null)
                    {
                        methodInfo = type.GetMethod(name2, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    }
                    else
                    {
                        methodInfo = type.GetMethod(name2, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parametersType, null);
                    }
                }
                if (methodInfo != null)
                {
                    break;
                }
            }
            if (methodInfo == null)
            {
                throw new MissingMethodException(methods[0]);
            }
            return methodInfo;
        }

        public static object TryStaticInvoke(List<string> methods, object[] parameters)
        {
            MethodInfo methodInfo = BackwardsCompatibilityUtility.GetMethodInfo(methods, null);
            return methodInfo.Invoke(null, parameters);
        }

        public static Type FindTypeByName(string name)
        {
            if (BackwardsCompatibilityUtility.m_TypeCache.ContainsKey(name))
            {
                return BackwardsCompatibilityUtility.m_TypeCache[name];
            }
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                if (BackwardsCompatibilityUtility.AllowLookupForAssembly(assembly.FullName))
                {
                    try
                    {
                        Type[] types = assembly.GetTypes();
                        foreach (Type type in types)
                        {
                            if (type.FullName == name)
                            {
                                BackwardsCompatibilityUtility.m_TypeCache[type.FullName] = type;
                                return type;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(string.Format("Count not fetch list of types from assembly {0} due to error: {1}", assembly.FullName, ex.Message));
                    }
                }
            }
            return null;
        }

        private static bool AllowLookupForAssembly(string name)
        {
            return Array.Exists<string>(BackwardsCompatibilityUtility.k_WhiteListedAssemblies, new Predicate<string>(name.StartsWith));
        }

        private static Dictionary<string, Type> m_TypeCache = new Dictionary<string, Type>();

        private static string[] k_WhiteListedAssemblies = new string[]
        {
        "UnityEditor"
        };
    }

}