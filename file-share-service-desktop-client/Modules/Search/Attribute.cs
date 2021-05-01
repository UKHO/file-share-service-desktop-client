using System;
using System.Collections.Generic;
using UKHO.FileShareService.DesktopClient.Core.Models;

namespace UKHO.FileShareService.DesktopClient.Modules.Search
{
    public class Attribute : IFssBatchAttribute
    {
        public string DisplayName { get; }
        public string AttributeName { get; }
        public AttributeType Type { get; }

        public Attribute(string displayName, AttributeType type)
        {
            if (type != AttributeType.UserAttributeString)
                throw new ArgumentException(
                    $"This constructor is only valid for AttributeType UserAttributeString. Other types must provide an attribute name",
                    nameof(type));
            DisplayName = displayName;
            AttributeName = displayName;
            Type = type;
        }

        public Attribute(string displayName, string attributeName, AttributeType type)
        {
            DisplayName = displayName;
            AttributeName = attributeName;
            Type = type;
        }

        public IEnumerable<Operators> AvailableOperators()
        {
            return Type switch
            {
                AttributeType.String => new[]
                {
                    Operators.Equals, Operators.NotEquals, Operators.Contains, Operators.StartsWith,
                    Operators.EndsWith
                },
                AttributeType.UserAttributeString => new[]
                {
                    Operators.Equals, Operators.NotEquals, Operators.Exists, Operators.NotExists,
                    Operators.Contains, Operators.StartsWith, Operators.EndsWith
                },
                AttributeType.Number => new[]
                {
                    Operators.Equals, Operators.NotEquals, Operators.GreaterThan, Operators.GreaterThanOrEquals,
                    Operators.LessThan, Operators.LessThanOrEquals
                },
                AttributeType.Date => new[]
                {
                    Operators.Equals, Operators.NotEquals, Operators.GreaterThan, Operators.GreaterThanOrEquals,
                    Operators.LessThan, Operators.LessThanOrEquals
                },
                AttributeType.NullableDate => new[]
                {
                    Operators.Equals, Operators.NotEquals, Operators.Exists, Operators.NotExists,
                    Operators.GreaterThan, Operators.GreaterThanOrEquals, Operators.LessThan,
                    Operators.LessThanOrEquals
                },
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}