using sly.lexer;

namespace ParserTests.Issue414;

public enum Issue414Token
{
[Lexeme(GenericToken.KeyWord, "IF")]
    [Lexeme(GenericToken.KeyWord, "if")]
    IF = 1,

    [Lexeme(GenericToken.KeyWord, "THEN")]
    [Lexeme(GenericToken.KeyWord, "then")]
    THEN = 2,

    [Lexeme(GenericToken.KeyWord, "ELSE")]
    [Lexeme(GenericToken.KeyWord, "else")]
    ELSE = 3,

    [Lexeme(GenericToken.KeyWord, "WHILE")]
    [Lexeme(GenericToken.KeyWord, "while")]
    WHILE = 4,

    [Lexeme(GenericToken.KeyWord, "DO")]
    [Lexeme(GenericToken.KeyWord, "do")]
    DO = 5,

    [Lexeme(GenericToken.KeyWord, "SKIP")]
    [Lexeme(GenericToken.KeyWord, "skip")]
    SKIP = 6,

    [Lexeme(GenericToken.KeyWord, "TRUE")]
    [Lexeme(GenericToken.KeyWord, "true")]
    TRUE = 7,

    [Lexeme(GenericToken.KeyWord, "FALSE")]
    [Lexeme(GenericToken.KeyWord, "false")]
    FALSE = 8,

    [Lexeme(GenericToken.KeyWord, "NOT")]
    [Lexeme(GenericToken.KeyWord, "not")]
    NOT = 9,

    [Lexeme(GenericToken.KeyWord, "AND")]
    [Lexeme(GenericToken.KeyWord, "and")]
    AND = 10,

    [Lexeme(GenericToken.KeyWord, "OR")]
    [Lexeme(GenericToken.KeyWord, "or")]
    OR = 11,

    [Lexeme(GenericToken.KeyWord, "PRINT")]
    [Lexeme(GenericToken.KeyWord, "print")]
    PRINT = 12,

    //[Lexeme(GenericToken.KeyWord, "BEGIN")]
    //[Lexeme(GenericToken.KeyWord, "begin")]
    //BEGIN = 13,

    //[Lexeme(GenericToken.KeyWord, "END")]
    //[Lexeme(GenericToken.KeyWord, "end")]
    //END = 14,

    

    #region literals 20 -> 29

    [Lexeme(GenericToken.Identifier)]
    IDENTIFIER = 20,

    [Lexeme(GenericToken.String)]
    STRING = 21,

    [Lexeme(GenericToken.Int)]
    INT = 22,

    [Lexeme(GenericToken.Double)]
    DOUBLE = 23,

    #endregion

    #region operators 30 -> 49

    [Lexeme(GenericToken.SugarToken, ">")]
    GREATER = 30,

    [Lexeme(GenericToken.SugarToken, "<")]
    LESSER = 31,

    [Lexeme(GenericToken.SugarToken, "==")]
    EQUALS = 32,

    [Lexeme(GenericToken.SugarToken, "!=")]
    DIFFERENT = 33,

    [Lexeme(GenericToken.SugarToken, ".")]
    DOT = 34,

    [Lexeme(GenericToken.SugarToken, ":=")]
    ASSIGN = 35,

    [Lexeme(GenericToken.SugarToken, "+")]
    PLUS = 36,

    [Lexeme(GenericToken.SugarToken, "-")]
    MINUS = 37,

    [Lexeme(GenericToken.SugarToken, "*")]
    TIMES = 38,

    [Lexeme(GenericToken.SugarToken, "/")]
    DIVIDE = 39,

    #endregion

    #region sugar 50 ->

    [Lexeme(GenericToken.SugarToken, "(")]
    LPAREN = 50,

    [Lexeme(GenericToken.SugarToken, ")")]
    RPAREN = 51,

    [Lexeme(GenericToken.SugarToken, "{")]
    LCURLYBRACE = 52,

    [Lexeme(GenericToken.SugarToken, "}")]
    RCURLYBRACE = 53,

    [Lexeme(GenericToken.SugarToken, "[")]
    LBRACKET = 54,

    [Lexeme(GenericToken.SugarToken, "]")]
    RBRACKET = 55,

    [Lexeme(GenericToken.SugarToken, ";")]
    SEMICOLON = 56,

    [Lexeme(GenericToken.SugarToken, ",")]
    COMMA = 57,

    EOF = 0

    #endregion
}