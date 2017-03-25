using Xunit;

namespace GrokDotNet.tests
{
    public class GrokTests
    {
        [Theory()]
        [InlineData("%{INT:test_int}", "1024", "test_int", "1024")]
        [InlineData("%{NUMBER:test_num}", "1024", "test_num", "1024")]
        [InlineData("%{IP:ip}", "192.168.7.7", "ip", "192.168.7.7")]
        public void SingleResults(string grokString, string line, string extractName, string extractData)
        {
            var grok = new Grok(grokString);
            var response = grok.ParseLine(line);
            Assert.Equal(1, response.Captures.Count);
            Assert.Equal(extractName, response.Captures[0].Item1);
            Assert.Equal(extractData, response.Captures[0].Item2.ToString());
        }

        [Fact]
        public void MulipleResults()
        {
            var grok = new Grok("%{WORD:name} %{INT:age} %{QUOTEDSTRING:motto}");
            var response = grok.ParseLine("gary 25 \"never quit\"");
            Assert.Equal(3, response.Captures.Count);
            Assert.Equal("name", response.Captures[0].Item1);
            Assert.Equal("gary", response.Captures[0].Item2.ToString());
            Assert.Equal("age", response.Captures[1].Item1);
            Assert.Equal("25", response.Captures[1].Item2.ToString());
            Assert.Equal("motto", response.Captures[2].Item1);
            Assert.Equal("\"never quit\"", response.Captures[2].Item2.ToString());
        }

        [Fact]
        public void NegativeResult()
        {
            var grok = new Grok("%{WORD:name} %{INT:age} %{QUOTEDSTRING:motto}");
            var response = grok.ParseLine("gary mail \"never quit\"");
            Assert.Equal(0, response.Captures.Count);
        }

        [Fact]
        public void TypedResult()
        {
            var grok = new Grok("%{WORD:name} %{INT:age:int} %{QUOTEDSTRING:motto}");
            var response = grok.ParseLine("gary 1 \"never quit\"");
            Assert.Equal(3, response.Captures.Count);
            Assert.Equal("age", response.Captures[1].Item1);
            Assert.Equal(typeof(int).ToString(), response.Captures[1].Item2.GetType().ToString());
        }

    }
}