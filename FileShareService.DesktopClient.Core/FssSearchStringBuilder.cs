using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UKHO.FileShareService.DesktopClient.Core.Models;

namespace UKHO.FileShareService.DesktopClient.Core
{
    public interface IFssSearchStringBuilder
    {
        string BuildSearch(IEnumerable<ISearchCriterion> searchCriteria);
    }

    public class FssSearchStringBuilder : IFssSearchStringBuilder
    {
        public string BuildSearch(IEnumerable<ISearchCriterion> searchCriteria)
        {
            StringBuilder query = new StringBuilder();
            bool isAndOrSelected = false;

            foreach (var (c, index) in searchCriteria.Select((c, i) => (c, i)))
            {
                if (c == null || c.SelectedFssAttribute == null || c.Operator == null)
                    continue;

                if (index > 0 && isAndOrSelected)
                {
                    string andOr = $" {c.And} ";
                    query.Append(andOr.ToLower());
                }

                string queryString = 
                    MapOperatorType(c.Operator) switch
                {
                    OperatorType.ComparisonOperator => 
                            MapForComparisonOperators(c.SelectedFssAttribute, MapOperator(c.Operator), c.Value),

                    OperatorType.FunctionOperator =>
                            MapForFunctionOperators(c.SelectedFssAttribute, MapOperator(c.Operator), c.Value),

                    OperatorType.LogicalOperator =>
                            MapForLogicalOperators(c.SelectedFssAttribute, MapOperator(c.Operator)),

                    _ => throw new NotImplementedException(
                               $"Not implemented search builder for operator {c.Operator} and type {MapOperatorType(c.Operator)}")
                };

                if(!string.IsNullOrWhiteSpace(queryString))
                {
                    query.Append(queryString);
                    isAndOrSelected = true;
                }
            }
            return query.ToString();
        }

        private string MapOperator(Operators? argOperator)
        {
            return argOperator switch
            {
                Operators.Equals => "eq",
                Operators.NotEquals => "ne",
                Operators.Exists => "ne null",
                Operators.NotExists => "eq null",
                Operators.GreaterThan => "gt",
                Operators.GreaterThanOrEquals => "ge",
                Operators.LessThan => "lt",
                Operators.LessThanOrEquals => "le",
                Operators.Contains => "contains",
                Operators.StartsWith => "startswith",
                Operators.EndsWith => "endswith",
                _ => throw new ArgumentOutOfRangeException(nameof(argOperator), argOperator, null)
            };
        }

        private OperatorType MapOperatorType(Operators? @operator)
        {
            return @operator switch
            {
                Operators.Equals => OperatorType.ComparisonOperator,
                Operators.NotEquals => OperatorType.ComparisonOperator,
                Operators.GreaterThan => OperatorType.ComparisonOperator,
                Operators.GreaterThanOrEquals => OperatorType.ComparisonOperator,
                Operators.LessThan => OperatorType.ComparisonOperator,
                Operators.LessThanOrEquals => OperatorType.ComparisonOperator,
                Operators.Exists => OperatorType.LogicalOperator,
                Operators.NotExists => OperatorType.LogicalOperator,
                Operators.Contains => OperatorType.FunctionOperator,
                Operators.StartsWith => OperatorType.FunctionOperator,
                Operators.EndsWith => OperatorType.FunctionOperator,
                _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null)
            };
        }

        private string MapForComparisonOperators(IFssBatchAttribute attribute, string @operator, string value)
        {
            switch(attribute.Type)
            {
                case AttributeType.String:
                    return $"{attribute.AttributeName} {@operator} '{value}'";

                case AttributeType.Number:
                case AttributeType.Date:
                case AttributeType.NullableDate:
                    return $"{attribute.AttributeName} {@operator} {value}";

                case AttributeType.UserAttributeString:
                    return $"$batch({attribute.AttributeName}) {@operator} '{value}'";

                default:
                    throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null);
            }
        }

        private string MapForFunctionOperators(IFssBatchAttribute attribute, string @operator, string value)
        {
            return attribute.Type switch
            {
                AttributeType.String => $"{@operator}({attribute.AttributeName}, '{value}')",
                AttributeType.UserAttributeString => $"{@operator}($batch({attribute.AttributeName}), '{value}')",
                _ => string.Empty
            };
        }

        private string MapForLogicalOperators(IFssBatchAttribute attribute, string @operator)
        {
            return attribute.Type switch
            {
                AttributeType.NullableDate => $"{attribute.AttributeName} {@operator}",
                AttributeType.UserAttributeString => $"$batch({attribute.AttributeName}) {@operator}",
                _ => string.Empty
            };
        }
    }
}