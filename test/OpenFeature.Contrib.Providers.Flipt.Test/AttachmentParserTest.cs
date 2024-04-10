using AutoFixture;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace OpenFeature.Contrib.Providers.Flipt.Test
{
    public class AttachmentParserTest
    {
        private readonly IFixture _fixture = new Fixture();

        /// <summary>
        /// DateTime test data
        /// </summary>
        /// <remarks>Attachment | Expected value</remarks>
        public static IEnumerable<object[]> DateTimeData
        {
            get
            {
                var fixture = new Fixture();
                var date = fixture.Create<DateTime>();
                yield return new object[] { $"\"{date:O}\"", date };
                yield return new object[] { $"\"{date.Date:O}\"", date.Date };
            }
        }

        [Theory]
        [InlineData("", null, false)]
        [InlineData("\"value\"", "value", true)]
        [InlineData("value", "value", true)]
        public void TryParseString_ShouldBeExpectedResult(string attachment, string expectedValue, bool expectedResult)
        {
            // Act
            var result = AttachmentParser.TryParseString(attachment, out var value);

            // Assert
            result.Should().Be(expectedResult);
            value.Should().Be(expectedValue);
        }

        [Theory]
        [MemberData(nameof(DateTimeData))]
        public void TryParseJsonValue_DateTimeValue_ShouldReturnTrue(string attachment, DateTime expectedValue)
        {
            // Act
            var result = AttachmentParser.TryParseJsonValue(attachment, out var value);

            // Assert
            result.Should().BeTrue();
            value.IsDateTime.Should().BeTrue();
            value.AsDateTime.Should().Be(expectedValue);
        }

        [Fact]
        public void TryParseJsonValue_DoubleValue_ShouldReturnTrue()
        {
            // Arrange
            var value = _fixture.Create<double>();
            var attachment = JsonSerializer.Serialize(value);

            // Act
            var result = AttachmentParser.TryParseJsonValue(attachment, out var output);

            // Assert
            result.Should().BeTrue();
            output.IsNumber.Should().BeTrue();
            output.AsDouble.Should().Be(value);
        }

        [Fact]
        public void TryParseJsonValue_IntegerValue_ShouldReturnTrue()
        {
            // Arrange
            var value = _fixture.Create<int>();
            var attachment = JsonSerializer.Serialize(value);

            // Act
            var result = AttachmentParser.TryParseJsonValue(attachment, out var output);

            // Assert
            result.Should().BeTrue();
            output.IsNumber.Should().BeTrue();
            output.AsInteger.Should().Be(value);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("false", false)]
        public void TryParseJsonValue_BooleanValue_ShouldReturnTrue(string attachment, bool expectedValue)
        {
            // Act
            var result = AttachmentParser.TryParseJsonValue(attachment, out var output);

            // Assert
            result.Should().BeTrue();
            output.IsBoolean.Should().BeTrue();
            output.AsBoolean.Should().Be(expectedValue);
        }

        [Fact]
        public void TryParseJsonValue_ObjectValue_ShouldReturnFalse()
        {
            // Arrange
            var value = new
            {
                booleanValue = _fixture.Create<bool>(),
                integerValue = _fixture.Create<int>(),
                doubleValue = _fixture.Create<double>(),
                stringValue = _fixture.Create<string>(),
                dateTimeValue = _fixture.Create<DateTime>(),
                nullValue = default(object),
                nested = new
                {
                    stringValue = _fixture.Create<string>()
                }
            };

            var attachment = JsonSerializer.Serialize(value);

            // Act
            var result = AttachmentParser.TryParseJsonValue(attachment, out var output);

            // Assert
            result.Should().BeTrue();
            output.IsStructure.Should().BeTrue();

            output.AsStructure[nameof(value.booleanValue)].IsBoolean.Should().BeTrue();
            output.AsStructure[nameof(value.booleanValue)].AsBoolean.Should().Be(value.booleanValue);

            output.AsStructure[nameof(value.integerValue)].IsNumber.Should().BeTrue();
            output.AsStructure[nameof(value.integerValue)].AsInteger.Should().Be(value.integerValue);

            output.AsStructure[nameof(value.doubleValue)].IsNumber.Should().BeTrue();
            output.AsStructure[nameof(value.doubleValue)].AsDouble.Should().Be(value.doubleValue);

            output.AsStructure[nameof(value.stringValue)].IsString.Should().BeTrue();
            output.AsStructure[nameof(value.stringValue)].IsString.Should().BeTrue();

            output.AsStructure[nameof(value.dateTimeValue)].IsDateTime.Should().BeTrue();
            output.AsStructure[nameof(value.dateTimeValue)].AsDateTime.Should().Be(value.dateTimeValue);

            output.AsStructure[nameof(value.nullValue)].IsNull.Should().BeTrue();

            output.AsStructure[nameof(value.nested)].IsStructure.Should().BeTrue();
            output.AsStructure[nameof(value.nested)].AsStructure[nameof(value.nested.stringValue)].IsString.Should().BeTrue();
            output.AsStructure[nameof(value.nested)].AsStructure[nameof(value.nested.stringValue)].AsString.Should().Be(value.nested.stringValue);
        }

        [Fact]
        public void TryParseJsonValue_PlainArrayValue_ShouldReturnTrue()
        {
            // Arrange
            var value = _fixture.CreateMany<string>();
            var attachment = JsonSerializer.Serialize(value);

            // Act
            var result = AttachmentParser.TryParseJsonValue(attachment, out var output);

            // Assert
            result.Should().BeTrue();
            output.IsList.Should().BeTrue();
            output.AsList.Select(s => s.AsString).Should().BeEquivalentTo(value);
        }

        [Fact]
        public void TryParseJsonValue_ObjectArrayValue_ShouldReturnTrue()
        {
            // Arrange
            var value = Enumerable.Range(0, 10).Select(i =>
                new
                {
                    booleanValue = _fixture.Create<bool>(),
                    integerValue = _fixture.Create<int>(),
                    stringValue = _fixture.Create<string>(),
                });

            var attachment = JsonSerializer.Serialize(value);

            // Act
            var result = AttachmentParser.TryParseJsonValue(attachment, out var output);

            // Assert
            result.Should().BeTrue();
            output.IsList.Should().BeTrue();
            output.AsList.Should().OnlyContain(v => v.IsStructure);
        }

        [Fact]
        public void TryParseJsonValue_NullValue_ShouldReturnTrue()
        {
            // Arrange
            var attachment = "null";

            // Act
            var result = AttachmentParser.TryParseJsonValue(attachment, out var output);

            // Assert
            result.Should().BeTrue();
            output.IsNull.Should().BeTrue();
        }
    }
}
