using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Verse;

namespace PressR.Debugger.Resolver
{
    public class InitialParseResult
    {
        public Expression InitialExpression { get; set; }
        public Type InitialType { get; set; }
        public int PartStartIndex { get; set; }
        public string CurrentPathSegment { get; set; }
        public string Error { get; set; }
        public bool Success => Error == null;
    }

    public class PathParser
    {
        private static readonly string[] KnownNamespaces = new[]
        {
            "Verse",
            "RimWorld",
            "UnityEngine",
            "System",
            "System.Collections.Generic",
        };

        private static HashSet<string> _knownTypes;
        private static Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();

        public PathParser()
        {
            EnsureKnownTypesInitialized();
        }

        private static void EnsureKnownTypesInitialized()
        {
            if (_knownTypes == null)
            {
                _knownTypes = [];

                foreach (Type type in typeof(PathParser).Assembly.GetTypes())
                {
                    _knownTypes.Add(type.FullName);
                }
            }
        }

        public InitialParseResult ParseInitialSegment(string expressionPath, string[] parts)
        {
            var result = new InitialParseResult();

            if (string.IsNullOrEmpty(expressionPath))
            {
                result.Error = "Expression path cannot be empty";
                return result;
            }

            if (expressionPath.Contains(","))
            {
                return ParseAssemblyQualifiedName(expressionPath, result);
            }
            else
            {
                return ParseStandardPath(parts, result);
            }
        }

        private InitialParseResult ParseAssemblyQualifiedName(
            string expressionPath,
            InitialParseResult result
        )
        {
            string[] typeAssemblySplit = expressionPath.Split(new[] { ',' }, 2);
            if (typeAssemblySplit.Length < 2)
            {
                result.Error = "Invalid AQN format: missing comma separator";
                return result;
            }

            string typeName = typeAssemblySplit[0].Trim();
            string assemblyAndMemberPart = typeAssemblySplit[1].Trim();
            int firstDotIndex = assemblyAndMemberPart.IndexOf('.');

            string assemblyName;
            string memberPath;

            if (firstDotIndex <= 0)
            {
                assemblyName = assemblyAndMemberPart;
                memberPath = string.Empty;
            }
            else
            {
                assemblyName = assemblyAndMemberPart.Substring(0, firstDotIndex).Trim();
                memberPath = assemblyAndMemberPart.Substring(firstDotIndex + 1);
            }

            result.InitialType = Type.GetType($"{typeName}, {assemblyName}", false);

            if (result.InitialType == null)
            {
                result.InitialType = GenTypes.GetTypeInAnyAssembly(typeName);

                if (result.InitialType == null)
                {
                    result.Error =
                        $"Could not find type '{typeName}' in assembly '{assemblyName}' or any loaded assembly";
                    return result;
                }
            }

            result.InitialExpression = null;
            result.PartStartIndex = 0;
            result.CurrentPathSegment = typeName;

            return result;
        }

        private InitialParseResult ParseStandardPath(string[] parts, InitialParseResult result)
        {
            if (parts.Length == 0)
            {
                result.Error = "Path has no parts to parse";
                return result;
            }

            string firstPart = parts[0];
            result.CurrentPathSegment = firstPart;

            if (firstPart == "Current")
            {
                result.InitialExpression = Expression.Constant(Verse.Current.Game);
                result.InitialType = typeof(Game);
                result.PartStartIndex = 1;
                return result;
            }

            if (firstPart == "Find")
            {
                result.InitialType = typeof(Verse.Find);
                result.InitialExpression = null;
                result.PartStartIndex = 1;
                return result;
            }

            if (firstPart == "Game" && parts.Length > 1 && parts[1] == "Current")
            {
                result.InitialExpression = Expression.Constant(Verse.Current.Game);
                result.InitialType = typeof(Game);
                result.PartStartIndex = 2;
                return result;
            }

            Type resolvedType = null;
            int maxParts = Math.Min(5, parts.Length);

            for (int i = 1; i <= maxParts; i++)
            {
                string combinedName = string.Join(".", parts.Take(i));
                resolvedType = AttemptMultiPartTypeResolution(combinedName, out int unused);

                if (resolvedType != null)
                {
                    result.InitialType = resolvedType;
                    result.InitialExpression = null;
                    result.PartStartIndex = i;
                    result.CurrentPathSegment = combinedName;
                    return result;
                }
            }

            resolvedType = AttemptResolveType(
                firstPart,
                parts.Length > 1 ? parts[1] : null,
                out int partsConsumed
            );

            if (resolvedType != null)
            {
                result.InitialType = resolvedType;
                result.InitialExpression = null;
                result.PartStartIndex = partsConsumed;
                result.CurrentPathSegment = resolvedType.Name;
                return result;
            }

            result.Error = $"Could not resolve type from '{firstPart}'";
            return result;
        }

        private Type AttemptMultiPartTypeResolution(string typeName, out int partsConsumed)
        {
            partsConsumed = typeName.Split('.').Length;

            if (_typeCache.TryGetValue(typeName, out Type cachedType))
            {
                return cachedType;
            }

            Type type = AttemptDirectTypeResolution(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            foreach (string ns in KnownNamespaces)
            {
                string fullName = ns + "." + typeName;
                type = AttemptDirectTypeResolution(fullName);

                if (type != null)
                {
                    _typeCache[typeName] = type;
                    return type;
                }
            }

            if (_knownTypes.Contains(typeName))
            {
                type = GenTypes.GetTypeInAnyAssembly(typeName);
                if (type != null)
                {
                    _typeCache[typeName] = type;
                    return type;
                }
            }

            type = GenTypes.GetTypeInAnyAssembly(typeName);
            if (type != null)
            {
                _knownTypes.Add(typeName);
                _typeCache[typeName] = type;
                return type;
            }

            return null;
        }

        private Type AttemptResolveType(string part1, string part2, out int partsConsumed)
        {
            partsConsumed = 1;

            Type type = AttemptDirectTypeResolution(part1);
            if (type != null)
                return type;

            if (part2 != null)
            {
                string combinedName = part1 + "." + part2;

                type = AttemptDirectTypeResolution(combinedName);
                if (type != null)
                {
                    partsConsumed = 2;
                    return type;
                }

                foreach (string ns in KnownNamespaces)
                {
                    string fullName = ns + "." + part1;
                    type = Type.GetType(fullName);
                    if (type != null)
                        return type;

                    fullName = ns + "." + combinedName;
                    type = Type.GetType(fullName);
                    if (type != null)
                    {
                        partsConsumed = 2;
                        return type;
                    }
                }
            }

            foreach (string ns in KnownNamespaces)
            {
                string fullName = ns + "." + part1;
                type = GenTypes.GetTypeInAnyAssembly(fullName);
                if (type != null)
                    return type;
            }

            return null;
        }

        private Type AttemptDirectTypeResolution(string typeName)
        {
            if (_typeCache.TryGetValue(typeName, out Type cachedType))
            {
                return cachedType;
            }

            Type type = Type.GetType(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            type = GenTypes.GetTypeInAnyAssembly(typeName);
            if (type != null)
            {
                _typeCache[typeName] = type;
                return type;
            }

            return null;
        }
    }
}
