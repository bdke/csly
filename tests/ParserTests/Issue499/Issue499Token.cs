using sly.lexer;

namespace ParserTests.Issue499;
internal enum Issue499Token
{
    [UpTo("\"", Channel = 1)]
    Test,
    [Sugar("\"", Channel = 0)]
    Quote
}
