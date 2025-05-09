using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Verse;

namespace PressR.Debug.ValueMonitor.Resolver
{
    public class MemberAccessor
    {
        private const BindingFlags AllInstanceOrStatic =
            BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.FlattenHierarchy;

        public (Expression NewExpression, Type NewType, string Error) AccessMember(
            Expression currentExpr,
            Type currentType,
            string memberName
        )
        {
            if (currentType == null)
            {
                return (null, null, "Cannot access member on null type");
            }

            MemberInfo memberInfo = FindMember(currentType, memberName);

            if (memberInfo == null)
            {
                return (
                    null,
                    null,
                    $"Could not find member '{memberName}' in type '{currentType.FullName}' or its hierarchy"
                );
            }

            try
            {
                bool isStatic = IsMemberStatic(memberInfo);
                Expression newExpression;

                if (currentExpr == null && !isStatic)
                {
                    return (
                        null,
                        null,
                        $"Cannot access instance member '{memberName}' on static type '{currentType.FullName}' without an instance"
                    );
                }
                else if (currentExpr != null && isStatic)
                {
                    newExpression = Expression.MakeMemberAccess(null, memberInfo);
                }
                else
                {
                    newExpression = Expression.MakeMemberAccess(currentExpr, memberInfo);
                }

                Type newType = GetMemberType(memberInfo);
                return (newExpression, newType, null);
            }
            catch (ArgumentException argEx)
            {
                return (
                    null,
                    null,
                    $"Error accessing member '{memberName}' on type '{currentType.FullName}'. Details: {argEx.Message}"
                );
            }
        }

        private MemberInfo FindMember(Type type, string memberName)
        {
            return type.GetMember(memberName, AllInstanceOrStatic).FirstOrDefault();
        }

        private bool IsMemberStatic(MemberInfo memberInfo)
        {
            return (memberInfo is FieldInfo fi && fi.IsStatic)
                || (memberInfo is PropertyInfo pi && pi.GetGetMethod(true)?.IsStatic == true)
                || (memberInfo is MethodInfo mi && mi.IsStatic);
        }

        private static Type GetMemberType(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                MethodInfo methodInfo => methodInfo.ReturnType,
                _ => null,
            };
        }
    }
}
