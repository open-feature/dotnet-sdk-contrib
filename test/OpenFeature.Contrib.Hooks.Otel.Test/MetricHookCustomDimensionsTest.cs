using Xunit;

namespace OpenFeature.Contrib.Hooks.Otel.Test
{
    public class MetricHookCustomDimensionsTest
    {
        [Fact]
        public void Adds_CustomDimension_HasValues()
        {
            // Arrange
            var customDimensions = new MetricHookCustomDimensions();
            string key = "dimensionKey";
            object value = "dimensionValue";

            // Act
            customDimensions.Add(key, value);

            // Assert
            var tagList = customDimensions.GetTagList();
            Assert.Single(tagList);
            Assert.Equal(key, tagList[0].Key);
            Assert.Equal(value, tagList[0].Value);
        }

        [Fact]
        public void CustomDimensionToList_IsEmpty()
        {
            // Arrange
            var customDimensions = new MetricHookCustomDimensions();

            // Act

            // Assert
            var tagList = customDimensions.GetTagList();
            Assert.Empty(tagList);
        }
    }
}