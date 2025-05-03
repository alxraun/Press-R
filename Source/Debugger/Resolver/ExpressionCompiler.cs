using System;
using System.Linq.Expressions;
using Verse;

namespace PressR.Debugger.Resolver
{
    public class ExpressionCompiler
    {
        private readonly PathParser _pathParser;
        private readonly MemberAccessor _memberAccessor;
        private readonly InterfaceFieldAccessor _interfaceFieldAccessor;
        private const string LogPrefix = "[Debugger] ";

        public ExpressionCompiler()
        {
            _pathParser = new PathParser();
            _memberAccessor = new MemberAccessor();
            _interfaceFieldAccessor = new InterfaceFieldAccessor();
        }

        public Func<object> CompileGetterDelegate(string expressionPath, out string error)
        {
            error = null;
            Expression currentExpression = null;
            Type currentType = null;

            try
            {
                string[] parts = expressionPath.Split('.');
                int partStartIndex = 0;
                string currentPathSegment = "";

                InitialParseResult initialResult = _pathParser.ParseInitialSegment(
                    expressionPath,
                    parts
                );

                if (!initialResult.Success)
                {
                    error = initialResult.Error;
                    return null;
                }

                currentExpression = initialResult.InitialExpression;
                currentType = initialResult.InitialType;
                partStartIndex = initialResult.PartStartIndex;
                currentPathSegment = initialResult.CurrentPathSegment;

                if (expressionPath.Contains(","))
                {
                    string[] typeAssemblySplit = expressionPath.Split(new[] { ',' }, 2);
                    if (typeAssemblySplit.Length < 2)
                    {
                        error = "Invalid assembly qualified name format";
                        return null;
                    }

                    string assemblyAndMemberPart = typeAssemblySplit[1].Trim();
                    int firstDotIndex = assemblyAndMemberPart.IndexOf('.');

                    if (firstDotIndex <= 0)
                    {
                        if (currentType != null)
                        {
                            Expression typeDefaultConversion = Expression.Convert(
                                Expression.Default(currentType),
                                typeof(object)
                            );
                            var typeDefaultLambda = Expression.Lambda<Func<object>>(
                                typeDefaultConversion
                            );
                            return typeDefaultLambda.Compile();
                        }
                        else
                        {
                            error = "Type has no members to access";
                            return null;
                        }
                    }

                    string memberPath = assemblyAndMemberPart.Substring(firstDotIndex + 1);
                    parts = memberPath.Split('.');
                    partStartIndex = 0;
                }

                for (int i = partStartIndex; i < parts.Length; i++)
                {
                    string part = parts[i];

                    if (string.IsNullOrEmpty(part))
                    {
                        error = $"Empty path segment at position {i} in path '{expressionPath}'";
                        return null;
                    }

                    currentPathSegment += (i > partStartIndex ? "." : "") + part;

                    if (currentType == null && currentExpression == null)
                    {
                        error =
                            $"Cannot access member '{part}' - current type and expression are null at '{currentPathSegment}'";
                        return null;
                    }

                    Expression previousExpression = currentExpression;
                    Type previousType = currentType ?? currentExpression?.Type;

                    string accessError;
                    (currentExpression, currentType, accessError) = _memberAccessor.AccessMember(
                        currentExpression,
                        previousType,
                        part
                    );

                    if (accessError != null)
                    {
                        if (
                            previousExpression != null
                            && previousType != null
                            && previousType.IsInterface
                        )
                        {
                            var interfaceResult =
                                _interfaceFieldAccessor.TryCompileInterfaceFieldGetter(
                                    previousExpression,
                                    previousType,
                                    part,
                                    currentPathSegment
                                );

                            if (interfaceResult.Getter != null)
                            {
                                return interfaceResult.Getter;
                            }
                            else
                            {
                                error =
                                    $"{interfaceResult.Error ?? accessError} at '{currentPathSegment}'";
                                return null;
                            }
                        }
                        else
                        {
                            error = $"{accessError} at '{currentPathSegment}'";
                            return null;
                        }
                    }
                }

                if (currentExpression == null)
                {
                    error =
                        $"Compiled expression is null after processing path '{expressionPath}'. Path might be incomplete or point to a static type/member without further access.";
                    return null;
                }

                Expression finalConversion = Expression.Convert(currentExpression, typeof(object));
                var finalLambda = Expression.Lambda<Func<object>>(finalConversion);
                return finalLambda.Compile();
            }
            catch (Exception ex)
            {
                DebuggerLog.Warning(
                    $"{LogPrefix}Exception compiling getter for path '{expressionPath}': {ex.Message}\n{ex.StackTrace}"
                );
                error = $"Compilation error: {ex.GetType().Name} - {ex.Message}";
                return null;
            }
        }
    }
}
