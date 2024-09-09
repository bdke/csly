using sly.i18n;
using sly.lexer;
using sly.sourceGenerator;

namespace ParserTests.lexer;

public enum DuplicateLabels
{
    [Sugar("(")]
    [LexemeLabel("en","left paranthesis")]
    [LexemeLabel("fr","paranthèse ouvrante")]
    LeftPar,
        
    [Sugar(")")]
    [LexemeLabel("en","left paranthesis")]
    [LexemeLabel("fr","paranthèse ouvrante")]
    RightPar,
        
}

public class DuplicateLabelsParser
{
    
}


[ParserGenerator(typeof(DuplicateLabels),typeof(DuplicateLabelsParser),typeof(object))]
public partial class DuplicateLabelLexerGenerator : AbstractParserGenerator<DuplicateLabels>
{
    
}