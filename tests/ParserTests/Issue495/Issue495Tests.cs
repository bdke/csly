using NFluent;
using sly.parser;
using sly.parser.generator;
using Xunit;

namespace ParserTests.Issue495;

public class Issue495Tests
{
    public Parser<Issue495Token,object> _parser { get; set; }

    public Parser<Issue495Token, object> GetParser()
    {
        if (_parser == null)
        {
            ParserBuilder<Issue495Token, object> builder = new ParserBuilder<Issue495Token, object>("en");
            var build = builder.BuildParser(new Issue495Parser(), ParserType.EBNF_LL_RECURSIVE_DESCENT, "program");
            Check.That(build).IsOk();
            _parser = build.Result;
        }

        return _parser;
    }

    [Fact]
    public void TestIssue495()
    {
        var parser = GetParser();
        Check.That(parser).IsNotNull();
        var parsed = parser.Parse("test = \"3 3\"");
        Check.That(parsed).IsOkParsing();
    } 
    
}