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

            foreach (var (c, index) in searchCriteria.Select((c, i) => (c, i)))
            {
                if (c == null || c.SelectedFssAttribute == null || c.Operator == null)
                    continue;

                if (index > 0)
                {
                    string andOr = $" {c.And} ";
                    query.Append(andOr);
                }

                switch (c.SelectedFssAttribute.Type)
                {
                    case AttributeType.UserAttributeString:
                        query.Append(
                            c.Operator == Operators.Exists || c.Operator == Operators.NotExists
                                ? $"$batch({c.SelectedFssAttribute.AttributeName}) {MapOperator(c.Operator)}"
                                : $"$batch({c.SelectedFssAttribute.AttributeName}) {MapOperator(c.Operator)} '{GetValueForOperator(c.Operator.Value, c.Value)}'");
                        break;
                    case AttributeType.String:
                        query.Append($"{c.SelectedFssAttribute.AttributeName} {MapOperator(c.Operator)} '{c.Value}'");
                        break;
                    case AttributeType.Number:
                    case AttributeType.Date:
                    case AttributeType.NullableDate:
                        query.Append($"{c.SelectedFssAttribute.AttributeName} {MapOperator(c.Operator)} {GetValueForOperator(c.Operator.Value, c.Value)}"
                                .TrimEnd());
                        break;
                    default:
                        throw new NotImplementedException(
                            $"Not implemented search builder for {c.SelectedFssAttribute.Type}");
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
                _ => throw new ArgumentOutOfRangeException(nameof(argOperator), argOperator, null)
            };
        }
    }
}