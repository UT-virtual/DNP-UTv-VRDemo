using System;
using System.Collections.Generic;
using System.Reflection;
using Basis.BasisUI;
using DNPDemo.Menu;

namespace DNPDemo.BasisPolicy
{
    /// <summary>
    /// Leaves only the demo's entries on the Basis main menu. Every other <see cref="BasisMenuActionProvider{TMenu}"/>
    /// type found in the loaded assemblies is suppressed through <see cref="BasisMenuBase{TMenu}.SuppressProvider(Type)"/>,
    /// so entries added by future Basis updates are blocked as well. Basis registers its entries after the scene loads,
    /// so suppressing before the scene loads blocks them up front; Basis logs a warning per type when it does that.
    /// </summary>
    static class DemoMenuPolicy
    {
        // Assemblies that cannot contain menu providers; skipping them keeps the startup scan short on Quest.
        static readonly string[] SkippedAssemblyPrefixes =
        {
            "System", "mscorlib", "netstandard", "Mono.", "Unity.", "UnityEngine", "UnityEditor", "nunit.",
        };

        internal static void Install()
        {
            foreach (Type type in FindSuppressedTypes(AppDomain.CurrentDomain.GetAssemblies()))
            {
                BasisMenuBase<BasisMainMenu>.SuppressProvider(type);
            }

            // Remove first so the entries are not doubled when play mode starts without a domain reload.
            BasisMenuBase<BasisMainMenu>.Providers.RemoveAll(provider => provider is DemoMenuProvider);
            foreach (DemoMenuProvider provider in CreateDemoProviders())
            {
                BasisMenuBase<BasisMainMenu>.AddProvider(provider);
            }
        }

        /// <summary>The entries shown on the main menu, left to right by <see cref="BasisMenuActionProvider{TMenu}.Order"/>.</summary>
        internal static DemoMenuProvider[] CreateDemoProviders() => new DemoMenuProvider[]
        {
            new PenMenuProvider(),
            new EnvironmentMenuProvider(),
        };

        internal static List<Type> FindSuppressedTypes(IEnumerable<Assembly> assemblies)
        {
            var result = new List<Type>();
            foreach (Assembly assembly in assemblies)
            {
                if (IsSkipped(assembly))
                {
                    continue;
                }
                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (ShouldSuppress(type))
                    {
                        result.Add(type);
                    }
                }
            }
            return result;
        }

        internal static bool ShouldSuppress(Type type) =>
            type != null
            && type.IsClass
            && !type.IsAbstract
            && !type.ContainsGenericParameters
            && typeof(BasisMenuActionProvider<BasisMainMenu>).IsAssignableFrom(type)
            && !typeof(DemoMenuProvider).IsAssignableFrom(type);

        static bool IsSkipped(Assembly assembly)
        {
            if (assembly.IsDynamic)
            {
                return true;
            }
            string name = assembly.GetName().Name;
            foreach (string prefix in SkippedAssemblyPrefixes)
            {
                if (name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }
            return false;
        }

        static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return Array.FindAll(e.Types, type => type != null);
            }
        }
    }
}
