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

                switch (MapOperatorType(c.Operator))
                {
                    case OperatorType.ComparisonOperator:
                        query.Append(MapForComparisonOperators(c.SelectedFssAttribute, MapOperator(c.Operator), c.Value));
                        break;
                    case OperatorType.FunctionOperator:
                        query.Append(MapForFunctionOperators(c.SelectedFssAttribute, MapOperator(c.Operator), c.Value));
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(c.Operator), c.Operator, null);
                }
            }
            return query.ToString();
        }

        private string GetValueForOperator(Operators @operator, string value)
        {
            return @operator switch
            {
                Operators.Equals => value,
                Operators.NotEquals => value,
                Operators.GreaterThan => value,
                Operators.GreaterThanOrEquals => value,
                Operators.LessThan => value,
                Operators.LessThanOrEquals => value,
                Operators.Contains => value,
                Operators.StartsWith => value,
                Operators.EndsWith => value,
                Operators.Exists => "",
                Operators.NotExists => "",
                _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null)
            };
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
                Operators.Exists => OperatorType.ComparisonOperator,
                Operators.NotExists => OperatorType.ComparisonOperator,
                Operators.Contains => OperatorType.FunctionOperator,
                Operators.StartsWith => OperatorType.FunctionOperator,
                Operators.EndsWith => OperatorType.FunctionOperator,
                _ => throw new ArgumentOutOfRangeException(nameof(@operator), @operator, null)
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="operator"></param>
        /// <param name="value"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="operator"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private string MapForFunctionOperators(IFssBatchAttribute attribute, string @operator, string value)
        {
            switch (attribute.Type)
            {
                case AttributeType.String:
                    return $"{@operator}({attribute.AttributeName}, '{value}')";

                case AttributeType.UserAttributeString:
                    return $"{@operator}($batch({attribute.AttributeName}), '{value}')";

                default:
                    throw new ArgumentOutOfRangeException(nameof(attribute), attribute, null);
            }
        }
    }
}