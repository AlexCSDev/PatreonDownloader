using System;
using System.Linq;
using PatreonDownloader.Implementation;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class PageFilterParserTests
    {
        [Fact]
        public void Parse_SinglePage_ReturnsExpectedSet()
        {
            var result = PageFilterParser.Parse("5");
            Assert.Equal(new[] { 5 }, result.OrderBy(x => x));
        }

        [Fact]
        public void Parse_MixedRangesAndPages_ReturnsExpectedSet()
        {
            var result = PageFilterParser.Parse("1-3,5");
            Assert.Equal(new[] { 1, 2, 3, 5 }, result.OrderBy(x => x));
        }

        [Fact]
        public void Parse_DuplicateValues_DeduplicatesPages()
        {
            var result = PageFilterParser.Parse("1-3,2,3");
            Assert.Equal(new[] { 1, 2, 3 }, result.OrderBy(x => x));
        }

        [Fact]
        public void Parse_InvalidDescendingRange_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => PageFilterParser.Parse("3-1"));
        }

        [Fact]
        public void Parse_InvalidToken_ThrowsFormatException()
        {
            Assert.Throws<FormatException>(() => PageFilterParser.Parse("1,a"));
        }
    }
}
