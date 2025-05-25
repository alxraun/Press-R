using System;
using System.Linq.Expressions;
using System.Reflection;
using Verse;

namespace PressR.Debug.ValueMonitor.Resolver
{
    public class InterfaceFieldAccessor
    {
        private const string LogPrefix = "[ValueMonitor] ";

        private const BindingFlags AllInstanceOrStatic =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.FlattenHierarchy;

        public (Func<object> Getter, string Error) TryCompileInterfaceFieldGetter(
            Expression previousExpression,
            Type previousType,
            string fieldName,
            string fullPathSegmentForError
        )
        {
            Func<object> getRealObjectFunc;
            try
            {
                getRealObjectFunc = Expression
                    .Lambda<Func<object>>(Expression.Convert(previousExpression, typeof(object)))
                    .Compile();
            }
            catch (Exception compileEx)
            {
                return (
                    null,
                    $"Failed to compile expression to get interface instance at '{fullPathSegmentForError}': {compileEx.Message}"
                );
            }

            ParameterExpression instanceParam = Expression.Parameter(typeof(object), "instance");
            Expression getFieldExpr = BuildReflectionFieldGetterExpression(
                instanceParam,
                fieldName
            );

            try
            {
                var getFieldValueLambda = Expression
                    .Lambda<Func<object, object>>(getFieldExpr, instanceParam)
                    .Compile();

                var finalLambda = Expression
                    .Lambda<Func<object>>(
                        Expression.Invoke(
                            Expression.Constant(getFieldValueLambda),
                            Expression.Invoke(Expression.Constant(getRealObjectFunc))
                        )
                    )
                    .Compile();

                return (finalLambda, null);
            }
            catch (Exception ex)
            {
                ValueMonitorLog.Warning(
                    $"{LogPrefix}Exception compiling interface field accessor for '{fieldName}' at '{fullPathSegmentForError}': {ex.Message}"
                );
                return (
                    null,
                    $"Compilation Exception during interface field access: {ex.GetType().Name}"
                );
            }
        }

        private Expression BuildReflectionFieldGetterExpression(
            ParameterExpression instanceParam,
            string fieldName
        )
        {
            var getTypeMethod = typeof(object).GetMethod("GetType");
            var getTypeCall = Expression.Call(instanceParam, getTypeMethod);

            var getFieldMethod = typeof(Type).GetMethod(
                "GetField",
                new[] { typeof(string), typeof(BindingFlags) }
            );
            var fieldInfoExpr = Expression.Call(
                getTypeCall,
                getFieldMethod,
                Expression.Constant(fieldName),
                Expression.Constant(AllInstanceOrStatic)
            );

            var fieldInfoVar = Expression.Variable(typeof(FieldInfo), "fieldInfo");
            var assignFieldInfo = Expression.Assign(fieldInfoVar, fieldInfoExpr);

            var isFieldInfoNull = Expression.Equal(
                fieldInfoVar,
                Expression.Constant(null, typeof(FieldInfo))
            );

            var getValueMethod = typeof(FieldInfo).GetMethod("GetValue", new[] { typeof(object) });
            var getValueCall = Expression.Call(fieldInfoVar, getValueMethod, instanceParam);

            var conditionalExpr = Expression.Condition(
                Expression.Not(isFieldInfoNull),
                Expression.Convert(getValueCall, typeof(object)),
                Expression.Constant(null, typeof(object))
            );

            var block = Expression.Block(
                typeof(object),
                new[] { fieldInfoVar },
                assignFieldInfo,
                conditionalExpr
            );

            return block;
        }
    }
}
