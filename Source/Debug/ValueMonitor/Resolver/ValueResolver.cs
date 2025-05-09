using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using Verse;

namespace PressR.Debug.ValueMonitor.Resolver
{
    public class ValueResolver
    {
        private readonly MemoryResolverCache _cache;
        private readonly ExpressionCompiler _compiler;
        private const string LogPrefix = "[ValueMonitor] ";

        public ValueResolver(MemoryResolverCache cache, ExpressionCompiler compiler)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
        }

        public ValueResolutionResult Resolve(string expressionPath)
        {
            if (string.IsNullOrWhiteSpace(expressionPath))
                return ValueResolutionResult.Failure("Path cannot be empty");

            if (_cache.TryGetError(expressionPath, out var cachedError))
                return ValueResolutionResult.Failure(cachedError);

            if (_cache.TryGetGetter(expressionPath, out var getter))
            {
                try
                {
                    return ValueResolutionResult.Success(getter());
                }
                catch (Exception ex)
                {
                    return HandleRuntimeError(ex, expressionPath);
                }
            }

            Func<object> compiledGetter = _compiler.CompileGetterDelegate(
                expressionPath,
                out string compilationError
            );

            if (compiledGetter != null)
            {
                _cache.AddGetter(expressionPath, compiledGetter);
                try
                {
                    return ValueResolutionResult.Success(compiledGetter());
                }
                catch (Exception ex)
                {
                    return HandleRuntimeError(ex, expressionPath);
                }
            }
            else
            {
                _cache.AddError(expressionPath, compilationError);
                return ValueResolutionResult.Failure(compilationError);
            }
        }

        private ValueResolutionResult HandleRuntimeError(Exception ex, string path)
        {
            string errorMessage;

            if (ex is NullReferenceException)
            {
                string nullLocation = FindNullValueLocation(ex, path);
                errorMessage = $"Null value encountered at '{nullLocation}' in path '{path}'";
            }
            else if (ex is TargetInvocationException tiEx && tiEx.InnerException != null)
            {
                errorMessage =
                    $"Error in invoked method: {tiEx.InnerException.GetType().Name} - {tiEx.InnerException.Message}";
            }
            else if (ex is InvalidCastException)
            {
                errorMessage = $"Type conversion error: {ex.Message}";
            }
            else if (ex is MissingMethodException)
            {
                errorMessage = $"Method not found: {ex.Message}";
            }
            else
            {
                errorMessage = $"Runtime Error: {ex.GetType().Name} - {ex.Message}";
            }

            ValueMonitorLog.Warning($"{LogPrefix}{errorMessage}");
            return ValueResolutionResult.Failure(errorMessage);
        }

        private string FindNullValueLocation(Exception ex, string fullPath)
        {
            string stackTrace = ex.StackTrace ?? string.Empty;

            Match match = Regex.Match(stackTrace, @"at System\.Reflection\.MethodBase\.Invoke\(");
            if (match.Success)
            {
                string[] pathParts = fullPath.Split('.');
                if (pathParts.Length > 1)
                {
                    return string.Join(".", pathParts, 0, pathParts.Length - 1);
                }
            }

            if (ex.InnerException != null)
            {
                return FindNullValueLocation(ex.InnerException, fullPath);
            }

            string[] segments = fullPath.Split('.');

            if (segments.Length > 1)
            {
                return string.Join(".", segments, 0, segments.Length - 1) + "?";
            }

            return fullPath;
        }
    }
}
