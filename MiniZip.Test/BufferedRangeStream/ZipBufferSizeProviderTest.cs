using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Knapcode.MiniZip
{
    public class ZipBufferSizeProviderTest
    {
        public class GetNextBufferSize
        {
            [Fact]
            public void GivesExpectedSequence()
            {
                // Arrange
                var target = new ZipBufferSizeProvider(100, 5000, 25);

                // Act
                var values = Enumerable
                    .Range(0, 8)
                    .Select(x => target.GetNextBufferSize())
                    .ToList();

                // Assert
                Assert.Equal(
                    new[] { 100, 5000, 125000, 3125000, 78125000, 1953125000, int.MaxValue, int.MaxValue },
                    values);
            }

            [Fact]
            public void UsesSpecificValueForDefaultInitial()
            {
                // Arrange
                var target = new ZipBufferSizeProvider(3333, 123);

                // Act
                var values = Enumerable
                    .Range(0, 8)
                    .Select(x => target.GetNextBufferSize())
                    .ToList();

                // Assert
                Assert.Equal(
                    new[] { 22, 3333, 409959, 50424957, int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue },
                    values);
            }
        }
    }
}
