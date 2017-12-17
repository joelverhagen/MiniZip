using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Knapcode.MiniZip
{
    public class BlockListTest
    {
        public class Prepend : Base
        {
            [Fact]
            public void IncreasesLength()
            {
                // Arrange & Act
                _target.Prepend(_bufferE);

                // Assert
                Assert.Equal(_bufferE.Length, _bufferE.Length);
            }
        }

        public class Append : Base
        {
            [Fact]
            public void IncreasesLength()
            {
                // Arrange & Act
                _target.Append(_bufferE);

                // Assert
                Assert.Equal(_bufferE.Length, _bufferE.Length);
            }
        }

        public class Search : Base
        {
            private readonly string _expected;

            public Search()
            {
                _target.Append(_bufferC);
                _target.Prepend(_bufferB);
                _target.Append(_bufferD);
                _target.Append(_bufferE);
                _target.Prepend(_bufferA);

                _expected = "0123456789ABCDE";
            }

            [Theory]
            [MemberData(nameof(ReturnsCorrectPositionData))]
            public void ReturnsCorrectPosition(int position)
            {
                // Arrange & Act
                var result = _target.Search(position);

                // Assert
                if (position < 0 || position >= _expected.Length)
                {
                    Assert.Null(result.Node);
                    Assert.Equal(0, result.Offset);
                }
                else
                {
                    var actual = (char)result.Node.Value[result.Offset];
                    Assert.Equal(_expected[position], actual);
                }
            }

            public static IEnumerable<object[]> ReturnsCorrectPositionData => Enumerable
                .Range(-2, 15 + 4)
                .Select(x => new object[] { x });
        }

        public class Base
        {
            internal readonly BlockList _target;
            internal readonly byte[] _bufferA;
            internal readonly byte[] _bufferB;
            internal readonly byte[] _bufferC;
            internal readonly byte[] _bufferD;
            internal readonly byte[] _bufferE;

            public Base()
            {
                _bufferA = Encoding.ASCII.GetBytes("0");
                _bufferB = Encoding.ASCII.GetBytes("12");
                _bufferC = Encoding.ASCII.GetBytes("345");
                _bufferD = Encoding.ASCII.GetBytes("6789");
                _bufferE = Encoding.ASCII.GetBytes("ABCDE");

                _target = new BlockList();
            }
        }
    }
}
