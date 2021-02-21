using System;
using System.Linq;
using System.Linq.Expressions;

namespace GraphQL.POCO
{
    public static class ExpressionHelper
    {
        public static string GetPropertyName<T>(Expression<Func<T, object>> expression)
        {
            var info = GetMemberInfo(expression);
            return info.Member.Name;
        }

        public static MemberExpression GetMemberInfo(Expression method)
        {
            LambdaExpression lambda = method as LambdaExpression;
            if (lambda == null)
                throw new ArgumentNullException("method");

            MemberExpression memberExpr = null;

            if (lambda.Body.NodeType == ExpressionType.Convert)
            {
                memberExpr =
                    ((UnaryExpression)lambda.Body).Operand as MemberExpression;
            }
            else if (lambda.Body.NodeType == ExpressionType.MemberAccess)
            {
                memberExpr = lambda.Body as MemberExpression;
            }

            if (memberExpr == null)
                throw new ArgumentException("method");

            return memberExpr;
        }

        public static TVal GetValue<TObj, TVal>(this TObj obj, Expression<Func<TObj, TVal>> expression)
        {
            return expression.Compile().Invoke(obj);
        }

        public static void AssignValue<TObj, TVal>(this TObj obj, Expression<Func<TObj, TVal>> expression, TVal value)
        {
            ParameterExpression valueParameterExpression = Expression.Parameter(typeof(object));
            Expression targetExpression = expression.Body is UnaryExpression ? ((UnaryExpression)expression.Body).Operand : expression.Body;

            var newValue = Expression.Parameter(expression.Body.Type);
            var assign = Expression.Lambda<Action<TObj, object>>
            (
                Expression.Assign(targetExpression, Expression.Convert(valueParameterExpression, targetExpression.Type)),
                expression.Parameters.Single(),
                valueParameterExpression
            );

            assign.Compile().Invoke(obj, value);
        }
    }
}
