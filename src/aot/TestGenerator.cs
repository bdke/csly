using aot.lexer;
using aot.parser;
using cslyGenerator;
using sly.lexer;
using sly.lexer.fsm;

namespace aot;

[ParserGenerator(typeof(AotLexer), typeof(AotParser), typeof(double))]
public partial class TestGenerator  : AbstractParserGenerator<AotLexer>
{
    public override Action<AotLexer, LexemeAttribute, GenericLexer<AotLexer>> UseTokenExtensions()
    {
        return null;
    }
    
    public override LexerPostProcess<AotLexer> UseTokenPostProcessor()
    {
        return (List<Token<AotLexer>> tokens) =>
        {
            return tokens.Select(x =>
            {
                if (x.TokenID == AotLexer.IDENTIFIER && x.Value == "add")
                {
                    x.TokenID = AotLexer.PLUS;
                }

                return x;
            }).ToList();
        };
    }   
}

