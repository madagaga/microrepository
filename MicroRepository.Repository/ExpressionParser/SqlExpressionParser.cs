using MicroRepository.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MicroRepository.Repository
{
    public interface IExpressionVisitor
    {
        void Visit(Expression expression);
        void VisitBinary(BinaryExpression expression);
        void VisitMethodCall(MethodCallExpression expression);
        void VisitMember(MemberExpression expression);
        void VisitUnary(UnaryExpression expression);
        void VisitConstant(ConstantExpression expression);
        List<QueryParameter> GetParameters();
    }

    public class SqlExpressionVisitor : IExpressionVisitor
    {
        private readonly List<QueryParameter> _parameters;
        private ExpressionType _linkingType;

        public SqlExpressionVisitor()
        {
            _parameters = new List<QueryParameter>();
            _linkingType = ExpressionType.Default;
        }

        public List<QueryParameter> GetParameters() => _parameters;

        public void Visit(Expression expression)
        {
            if (expression == null) return;

            switch (expression.NodeType)
            {
                case ExpressionType.AndAlso:
                case ExpressionType.OrElse:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                    VisitBinary((BinaryExpression)expression);
                    break;
                case ExpressionType.Call:
                    VisitMethodCall((MethodCallExpression)expression);
                    break;
                case ExpressionType.MemberAccess:
                    VisitMember((MemberExpression)expression);
                    break;
                case ExpressionType.Not:
                case ExpressionType.Convert:
                    VisitUnary((UnaryExpression)expression);
                    break;
                case ExpressionType.Constant:
                    VisitConstant((ConstantExpression)expression);
                    break;
                default:
                    throw new NotSupportedException($"Expression type {expression.NodeType} is not supported");
            }
        }

        public void VisitBinary(BinaryExpression expression)
        {
            if (expression.Right == null || expression.Left == null)
                throw new ArgumentException("expression.left/right cannot be null", nameof(expression));


            if (expression.NodeType == ExpressionType.AndAlso || expression.NodeType == ExpressionType.OrElse)
            {
                var previousLinkingType = _linkingType;
                _linkingType = expression.NodeType;
                Visit(expression.Left);
                Visit(expression.Right);
                _linkingType = previousLinkingType;
                return;
            }

            var parameter = new QueryParameter();

            if (expression.Left is MethodCallExpression methodCall)
            {
                HandleMethodCallInBinary(methodCall, expression, parameter);
                return;
            }

            parameter.PropertyName = GetPropertyName(expression.Left);
            parameter.PropertyValue = GetValue(expression.Right);
            parameter.QueryOperator = GetOperator(expression.NodeType);
            parameter.LinkingOperator = GetOperator(_linkingType);

            if (parameter.PropertyValue?.ToString() == "[table]")
            {
                parameter.PropertyFormat = "[table]";
                parameter.PropertyValue = GetPropertyName(expression.Right);
            }

            if (parameter.PropertyValue == null)
            {
                parameter.QueryOperator = expression.NodeType == ExpressionType.Equal ? "IS NULL" : "IS NOT NULL";
            }

            AddParameter(parameter);
        }

        public void VisitMethodCall(MethodCallExpression expression)
        {
            var parameter = new QueryParameter
            {
                LinkingOperator = GetOperator(_linkingType),
                QueryOperator = _linkingType == ExpressionType.NotEqual ? "NOT LIKE" : "LIKE",
                PropertyName = GetPropertyName(expression.Object),
                PropertyValue = GetValue(expression.Arguments[0])
            };

            switch (expression.Method.Name)
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
                case "HasFlag":
                    parameter.QueryOperator = _linkingType == ExpressionType.NotEqual ? "<>" : "=";
                    parameter.PropertyFormat = $"({parameter.PropertyName} & @p{{0}})";
                    break;
                default:
                    throw new NotSupportedException($"Method {expression.Method.Name} is not supported");
            }

            AddParameter(parameter);
        }

        public void VisitMember(MemberExpression expression)
        {
            var parameter = new QueryParameter
            {
                PropertyName = GetPropertyName(expression),
                PropertyValue = _linkingType == ExpressionType.Not ? "False" : "True",
                QueryOperator = GetOperator(ExpressionType.Equal),
                LinkingOperator = GetOperator(_linkingType)
            };

            AddParameter(parameter);
        }

        public void VisitUnary(UnaryExpression expression)
        {
            var previousLinkingType = _linkingType;
            _linkingType = expression.NodeType == ExpressionType.Not ? ExpressionType.NotEqual : ExpressionType.Equal;
            Visit(expression.Operand);
            _linkingType = previousLinkingType;
        }

        public void VisitConstant(ConstantExpression expression)
        {
            // Typically handled as part of other expressions
        }

        private void HandleMethodCallInBinary(MethodCallExpression methodCall, BinaryExpression parent, QueryParameter parameter)
        {
            if (parent.Type == typeof(bool))
            {
                
                var result = (ConstantExpression)parent.Right;
                var operand = ExpressionType.Equal;

                if (result.Value == null)
                    throw new ArgumentException("constant expression cannot be null", nameof(result));

                if ((bool)result.Value == false && parent.NodeType == ExpressionType.Equal)
                    operand = ExpressionType.NotEqual;
                if ((bool)result.Value == true && parent.NodeType == ExpressionType.NotEqual)
                    operand = ExpressionType.NotEqual;

                _linkingType = operand;
                VisitMethodCall(methodCall);
            }
            else
            {
                _linkingType = parent.NodeType;
                VisitMethodCall(methodCall);
            }
        }

        private void AddParameter(QueryParameter parameter)
        {
            if (_parameters.Count == 0)
                parameter.LinkingOperator = null;
            _parameters.Add(parameter);
        }

        private string GetPropertyName(Expression? expression)
        {
            if (expression is MemberExpression memberExpression)
            {
                if(memberExpression.Member.DeclaringType == null)
                    throw new ArgumentException("memberExpression.Member.DeclaringType cannot be null", nameof(memberExpression));

                var dict = TableDefinitionCache.GetPropertiesDictionary(memberExpression.Member.DeclaringType);
                return dict[memberExpression.Member.Name].FullDbName;
            }
            else if (expression is MethodCallExpression methodCallExpression)
            {
                return GetPropertyName(methodCallExpression.Object);
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                return GetPropertyName(unaryExpression.Operand);
            }
            throw new KeyNotFoundException($"{expression}");
        }

        private object? GetValue(Expression? expression)
        {
            if (expression == null)
                throw new ArgumentException("memberExpression.Expression cannot be null", nameof(expression));

            if (expression is ConstantExpression constant)
                return constant.Value;

            if (expression is MethodCallExpression || expression.NodeType == ExpressionType.Convert)
                return Expression.Lambda(expression).Compile().DynamicInvoke();

            if (expression is MemberExpression memberExpression)
            {
                if (memberExpression.Expression == null)
                    throw new ArgumentException("memberExpression.Expression cannot be null", nameof(memberExpression));

                switch (memberExpression.Expression.NodeType)
                {
                    case ExpressionType.Constant:
                    case ExpressionType.MemberAccess:
                        return Expression.Lambda(memberExpression).Compile().DynamicInvoke();
                    case ExpressionType.Parameter:
                        return "[table]";
                }
            }
            return null;
        }

        private string GetOperator(ExpressionType type)
        {
            return type switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.AndAlso or ExpressionType.And => "AND",
                ExpressionType.Or or ExpressionType.OrElse => "OR",
                ExpressionType.Default => string.Empty,
                _ => throw new NotImplementedException($"Operator {type} is not implemented")
            };
        }
    }
}
