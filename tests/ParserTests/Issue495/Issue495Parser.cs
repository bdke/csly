using System.Collections.Generic;
using sly.lexer;
using sly.parser.generator;

namespace ParserTests.Issue495;

public class Issue495Parser
{

    public Issue495Parser()
    {
        
    }

    [Production("STRING: StartQuote StringValue* EndQuote")]
    public object stringValue(Token<Issue495Token> open, Token<Issue495Token> value, Token<Issue495Token> close)
    {
        return null;
    }

    [Production("statement : Identifier Assign STRING End")]
    public object Statement(Token<Issue495Token> id, Token<Issue495Token> assign, object value,
        Token<Issue495Token> end)
    {
        return null;
    }

    [Production("program: statement*")]
    public object Program(List<object> statements)
    {
        return null;
    }
}