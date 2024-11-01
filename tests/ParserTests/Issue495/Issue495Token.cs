using sly.lexer;

namespace ParserTests.Issue495;

public enum Issue495Token
{
    [Sugar("\"")]
    [Push("defaultString")]
    StartQuote,
    [Sugar("\"")]
    [Mode("defaultString")]
    [Pop]
    EndQuote,
    [UpTo("\"")]
    [Mode("defaultString")]
    StringValue,
    [Sugar(";")]
    End,
    [AlphaNumDashId]
    Identifier,
    [Sugar("=")]
    Assign
}