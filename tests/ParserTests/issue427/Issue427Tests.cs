using System;
using expressionparser;
using NFluent;
using sly.lexer;
using sly.parser.generator;
using Xunit;

namespace ParserTests.Issue364;


[Lexer]
public enum Issue427Lexer
{
    
    [Int] INTEGER,
    [Sugar("(")] LPAREN,
    [Sugar(")")] RPAREN,
    [Sugar("+")] PLUS,
    [Sugar(";")] SEMICOLON,
}

public class Issue427Parser
{
    [Production("main : expression")]
    public static int RootTag(int expression) => expression;

    [Operand]
    [Production("operand : INTEGER")]
    public static int IntegerTag(Token<Issue427Lexer> expressionToken) => int.Parse(expressionToken.Value.ToString());

    [Operation((int) Issue427Lexer.PLUS, Affix.InFix, Associativity.Left, 10)]
    public static int Addition(int left, Token<Issue427Lexer> operatorToken, int right) => left + right;

    [Production("expression : LPAREN [d] Issue427Parser_expressions RPAREN [d]")]
    [Production("expression : Issue427Parser_expressions")]
    [Production("expression : operand")]
    public static int Expression (int expression) => expression;
}

public class Issue427MinimalParser
{

    [Production("root : Issue427MinimalParser_expressions")]
    public static int Root(int value)
    {
        return value;
    }

    [Operand]
    [Production("operand : INTEGER")]
    public static int IntegerTag(Token<Issue427Lexer> expressionToken) => int.Parse(expressionToken.Value.ToString());

    [Operation((int) Issue427Lexer.PLUS, Affix.InFix, Associativity.Left, 10)]
    public static int Addition(int left, Token<Issue427Lexer> operatorToken, int right) => left + right;


}

public class Issue427Tests
{
    [Fact]
    public void TestIssue427()
    {
        ParserBuilder<Issue427Lexer, int> Parser = new ParserBuilder<Issue427Lexer, int>("en");
        Issue427Parser oparser = new Issue427Parser();
        
        var r = Parser.BuildParser(oparser,ParserType.LL_RECURSIVE_DESCENT,"main");
        Check.That(r).IsOk();
        var parser = r.Result;
        var result = parser.Parse("2 + 3");
        Console.WriteLine(result.IsOk);

        result = parser.Parse("(2 + 3)");
        Console.WriteLine(result.IsOk);
        
        result = parser.Parse("((2+3)+4)");
        Console.WriteLine(result.IsOk);

        result = parser.Parse("(2+3)+4)");
        Console.WriteLine(result.IsOk);




    }
    
    [Fact]
    public void TestIssueMinimal427()
    {
        ParserBuilder<Issue427Lexer, int> Parser = new ParserBuilder<Issue427Lexer, int>("en");
        Issue427MinimalParser oparser = new Issue427MinimalParser();
        
        var r = Parser.BuildParser(oparser,ParserType.LL_RECURSIVE_DESCENT,"root");
        Check.That(r).IsOk();
        var parser = r.Result;
        var result = parser.Parse("2 + 3");
        Console.WriteLine(result.IsOk);

    }

}