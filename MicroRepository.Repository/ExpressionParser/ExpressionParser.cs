using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace MicroRepository.Repository
{
    // inspired by http://www.bradoncode.com/blog/2012/12/creating-data-repository-using-dapper.html
    internal static class ExpressionParser
    {
        internal static void ParseExpression(Expression body, ExpressionType linkingType, ref List<QueryParameter> queryProperties)
        {
            if (body.NodeType != ExpressionType.AndAlso && body.NodeType != ExpressionType.OrElse)
            {
                QueryParameter parameter = null;
                if (body is BinaryExpression)
                    parameter = ParseBinary((BinaryExpression)body, linkingType);
                else if (body is MethodCallExpression)
                    parameter = ParseMethodCall((MethodCallExpression)body, linkingType);
                else if (body is MemberExpression)
                    parameter = ParseMember((MemberExpression)body, linkingType);
                else if (body is UnaryExpression)
                    parameter = ParseUnary((UnaryExpression)body, linkingType);

                else
                    throw new NotSupportedException(string.Format("{0} is not supported", body.Type));

                if (parameter != null)
                {
                    //remove the first linkinOperator
                    if (queryProperties.Count == 0)
                        parameter.LinkingOperator = null;

                    queryProperties.Add(parameter);
                }
            }
            else
            {
                ParseExpression(((BinaryExpression)body).Left, body.NodeType, ref queryProperties);
                ParseExpression(((BinaryExpression)body).Right, body.NodeType, ref queryProperties);
            }


        }

        internal static string GetPropertyName(Expression body)
        {
            string propertyName = string.Empty;
            if (body is MemberExpression)
            {
                MemberExpression expression = body as MemberExpression;
                var dict = Caching.TableDefinitionCache.GetPropertiesDictionary(expression.Member.DeclaringType);
                propertyName = dict[expression.Member.Name].EnquotedFullName;
            }
            else if(body is MethodCallExpression)
            {
                MethodCallExpression expression = body as MethodCallExpression;
                return GetPropertyName(expression.Object);
            }
            

            //// hack to remove the trailing ) when convering.
            //if (body.NodeType == ExpressionType.Convert)
            //    propertyName = propertyName.TrimEnd(')');

            //// hack for mono 
            //if (propertyName.IndexOf(',') > -1)
            //    propertyName = propertyName.Split(',')[0].Trim();

            return propertyName;
        }

        private static QueryParameter ParseMethodCall(MethodCallExpression methodCallExpression, ExpressionType linkingType)
        {
            QueryParameter parameter = new QueryParameter();
            parameter.LinkingOperator = GetOperator(linkingType);
            // by default it's LIKE;
            parameter.QueryOperator = linkingType == ExpressionType.NotEqual ? "NOT LIKE" : "LIKE";

            parameter.PropertyName = GetPropertyName(methodCallExpression.Object);
            parameter.PropertyValue = GetValue(methodCallExpression.Arguments[0]);

            string format = string.Empty;
            switch (methodCallExpression.Method.Name)
            {
                case "Contains":
                    parameter.PropertyValue = $"%{parameter.PropertyValue}%";
                    break;
                case "EndsWith":
                    parameter.PropertyValue = $"{parameter.PropertyValue}%";
                    break;
                case "StartsWith":
                    parameter.PropertyValue = $"%{parameter.PropertyValue}";
                    break;
                // not working well 
                case "HasFlag":
                    parameter.QueryOperator = linkingType == ExpressionType.NotEqual ? "<>" : "=";
                    parameter.PropertyFormat = $"({parameter.PropertyName} & @p{{0}})";
                    break;
                default:
                    throw new NotSupportedException(string.Format("{0} is not supported", methodCallExpression.Method.Name));
            }

            return parameter;
        }

        internal static QueryParameter ParseUnary(UnaryExpression body, ExpressionType linkingType)
        {
            QueryParameter parameter = new QueryParameter();
            if(body.Operand is MethodCallExpression)
                parameter = ParseMethodCall((MethodCallExpression)body.Operand, body.NodeType == ExpressionType.Not ? ExpressionType.NotEqual : ExpressionType.Call);
            else if(body.Operand is MemberExpression)
                parameter = ParseMember((MemberExpression)body.Operand, body.NodeType == ExpressionType.Not ? ExpressionType.NotEqual : ExpressionType.Call);

            parameter.LinkingOperator = GetOperator(linkingType);
            return parameter;
        }

        private static QueryParameter ParseMember(MemberExpression body, ExpressionType linkingType)
        {
            QueryParameter parameter = new QueryParameter();

            parameter.PropertyName = GetPropertyName(body);
            parameter.PropertyValue = linkingType == ExpressionType.Not ? "False" : "True";
            parameter.QueryOperator = GetOperator(ExpressionType.Equal);
            return parameter;
        }

        internal static QueryParameter ParseBinary(BinaryExpression body, ExpressionType linkingType)
        {

            QueryParameter parameter = new QueryParameter();
            if (body.Left is MethodCallExpression)
            {
                ExpressionType operand = ExpressionType.Equal;
                if (body.Type == typeof(Boolean))
                {
                    ConstantExpression result = (ConstantExpression)body.Right;

                    if ((bool)result.Value == false && body.NodeType == ExpressionType.Equal)
                        operand = ExpressionType.NotEqual;
                    if ((bool)result.Value == true && body.NodeType == ExpressionType.NotEqual)
                        operand = ExpressionType.NotEqual;

                    parameter = ParseMethodCall((MethodCallExpression)body.Left, operand);
                }
                else
                    parameter = ParseMethodCall((MethodCallExpression)body.Left, body.NodeType);

                parameter.LinkingOperator = GetOperator(linkingType);

                return parameter;
            }


            parameter.PropertyName = GetPropertyName(body.Left);            
            parameter.PropertyValue = GetValue(body.Right);
            if(parameter.PropertyValue.ToString() == "[table]")
            {
                parameter.PropertyFormat = "[table]";
                parameter.PropertyValue = GetPropertyName(body.Right);
            }

            parameter.QueryOperator = GetOperator(body.NodeType);
            parameter.LinkingOperator = GetOperator(linkingType);

            if (parameter.PropertyValue == null)
            {
                if (body.NodeType == ExpressionType.Equal)
                    parameter.QueryOperator = "IS NULL";
                if (body.NodeType == ExpressionType.NotEqual)
                    parameter.QueryOperator = "IS NOT NULL";
            }
            return parameter;
        }

        internal static object GetValue(Expression expr)
        {
            object value = null;
            if (expr is ConstantExpression)
                value = ((ConstantExpression)expr).Value;
            else if (expr is MethodCallExpression || expr.NodeType == ExpressionType.Convert)
                value = Expression.Lambda(expr).Compile().DynamicInvoke();
            else if(expr is MemberExpression)
            {
                MemberExpression propertyValue = (MemberExpression)expr;
                switch(propertyValue.Expression.NodeType)
                {
                    case ExpressionType.Constant:
                    case ExpressionType.MemberAccess:
                        value = Expression.Lambda(propertyValue).Compile().DynamicInvoke();
                        break;
                    case ExpressionType.Parameter:
                        value = "[table]";
                        break;
                }                
                
            }
            return value;
        }

        internal static string GetOperator(ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    return "AND";
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return "OR";
                case ExpressionType.Default:
                    return string.Empty;
                default:
                    throw new NotImplementedException();
            }
        }
    }


    /// <summary>
    /// Class that models the data structure in coverting the expression tree into SQL and Params.
    /// </summary>
    internal class QueryParameter
    {
        public string LinkingOperator { get; set; }
        public string PropertyName { get; set; }
        public object PropertyValue { get; set; }
        public string QueryOperator { get; set; }
        public string PropertyFormat { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryParameter" /> class.
        /// </summary>
        /// <param name="linkingOperator">The linking operator.</param>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="queryOperator">The query operator.</param>
        internal QueryParameter(string linkingOperator, string propertyName, object propertyValue, string queryOperator)
        {
            this.LinkingOperator = linkingOperator;
            this.PropertyName = propertyName;
            this.PropertyValue = propertyValue;
            this.QueryOperator = queryOperator;
        }

        internal QueryParameter() { }
    }
}
