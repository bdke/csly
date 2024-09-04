using System;
using System.Collections.Generic;
using aot.parser;
using csly.indentedWhileLang.parser;
using csly.whileLang.model;
using sly.lexer;
using sly.parser.generator;
using sly.parser.parser;

namespace ParserTests.aot;

public class AotIndentedWhileParserBuilder
{
    public IAotLexerBuilder<IndentedWhileTokenGeneric> BuildAotWhileLexer()
    {
        var builder = AotLexerBuilder<IndentedWhileTokenGeneric>.NewBuilder();
        builder.IsIndentationAware()
            .UseIndentations("\t")
            // keywords
            .Keyword(IndentedWhileTokenGeneric.IF, "if")
            .Keyword(IndentedWhileTokenGeneric.THEN, "then")
            .Keyword(IndentedWhileTokenGeneric.ELSE, "else")
            .Keyword(IndentedWhileTokenGeneric.WHILE, "while")
            .Keyword(IndentedWhileTokenGeneric.DO, "do")
            .Keyword(IndentedWhileTokenGeneric.SKIP, "skip")
            .Keyword(IndentedWhileTokenGeneric.PRINT, "print")
            .Keyword(IndentedWhileTokenGeneric.TRUE, "true")
            .Keyword(IndentedWhileTokenGeneric.FALSE, "false")
            .Keyword(IndentedWhileTokenGeneric.NOT, "not")
            .Keyword(IndentedWhileTokenGeneric.AND, "and")
            .Keyword(IndentedWhileTokenGeneric.OR, "or")
            .Keyword(IndentedWhileTokenGeneric.RETURN, "return")
            // literals
            .AlphaNumDashId(IndentedWhileTokenGeneric.IDENTIFIER)
            .String(IndentedWhileTokenGeneric.STRING)
            .Integer(IndentedWhileTokenGeneric.INT)
            // operators
            .Sugar(IndentedWhileTokenGeneric.GREATER, ">")
            .Sugar(IndentedWhileTokenGeneric.LESSER, "<")
            .Sugar(IndentedWhileTokenGeneric.EQUALS, "==")
            .Sugar(IndentedWhileTokenGeneric.DIFFERENT, "!==")
            .Sugar(IndentedWhileTokenGeneric.CONCAT, ".")
            .Sugar(IndentedWhileTokenGeneric.ASSIGN, ":=")
            .Sugar(IndentedWhileTokenGeneric.PLUS, "+")
            .Sugar(IndentedWhileTokenGeneric.MINUS, "-")
            .Sugar(IndentedWhileTokenGeneric.TIMES, "*")
            .Sugar(IndentedWhileTokenGeneric.DIVIDE, "/")
            .Sugar(IndentedWhileTokenGeneric.SEMICOLON, ";")
            .Sugar(IndentedWhileTokenGeneric.SEMICOLON, ";")
            .SingleLineComment(IndentedWhileTokenGeneric.COMMENT, "#");
        return builder;
    }

    public IAotEBNFParserBuilder<IndentedWhileTokenGeneric, WhileAST> BuildAotWhileParser()
    {
        IndentedWhileParserGeneric instance = new IndentedWhileParserGeneric();
        var builder = AotEBNFParserBuilder<IndentedWhileTokenGeneric, WhileAST>
            .NewBuilder(instance, "program", "en");
        Func<object[], WhileAST> comparisons = (object[] args) =>
        {
            return instance.binaryComparisonExpression((WhileAST)args[0], (Token<IndentedWhileTokenGeneric>)args[1],
                (WhileAST)args[2]);
        };

        Func<object[], WhileAST> numericTerm = args => instance.binaryTermNumericExpression((WhileAST)args[0],
            (Token<IndentedWhileTokenGeneric>)args[1],
            (WhileAST)args[2]);
        Func<object[], WhileAST> numericFactor = args => instance.binaryFactorNumericExpression((WhileAST)args[0],
            (Token<IndentedWhileTokenGeneric>)args[1],
            (WhileAST)args[2]);

        builder.Right(50, IndentedWhileTokenGeneric.LESSER, comparisons)
            .Right(50, IndentedWhileTokenGeneric.GREATER, comparisons)
            .Right(50, IndentedWhileTokenGeneric.EQUALS, comparisons)
            .Right(50, IndentedWhileTokenGeneric.DIFFERENT, comparisons)
            .Right(10, IndentedWhileTokenGeneric.CONCAT, (args) =>
            {
                return instance.binaryStringExpression((WhileAST)args[0], (Token<IndentedWhileTokenGeneric>)args[1],
                    (WhileAST)args[2]);
            })
            .Production("program : sequence", args => { return instance.program((WhileAST)args[0]); })
            .Production("block : INDENT[d] sequence UINDENT[d]",
                args => { return instance.sequenceStatements((SequenceStatement)args[0]); })
            .Production("statement : block", args => { return instance.blockStatement((WhileAST)args[0]); })
            .Production("sequence : statement*", args => { return instance.sequence((List<WhileAST>)args[0]); })
            .Production("statement: IF[d] IndentedWhileParserGeneric_expressions THEN[d] block (ELSE[d] block)?",
                args =>
                {
                    return instance.ifStmt((WhileAST)args[0], (WhileAST)args[1],
                        (ValueOption<Group<IndentedWhileTokenGeneric, WhileAST>>)args[2]);
                })
            .Production("statement: WHILE[d] IndentedWhileParserGeneric_expressions DO[d] block",
                args => { return instance.whileStmt((WhileAST)args[0], (WhileAST)args[1]); })
            .Production("statement: IDENTIFIER ASSIGN[d] IndentedWhileParserGeneric_expressions",
                args => { return instance.assignStmt((Token<IndentedWhileTokenGeneric>)args[0], (Expression)args[1]); })
            .Production("statement: SKIP[d]", args => { return instance.skipStmt(); })
            .Production("statement: RETURN[d] IndentedWhileParserGeneric_expressions",
                args => { return instance.ReturnStmt((Expression)args[0]); })
            .Production("statement: PRINT[d] IndentedWhileParserGeneric_expressions",
                args => { return instance.printStmt((Expression)args[0]); })
            .Production("primary : INT",
                args => { return instance.PrimaryInt(((Token<IndentedWhileTokenGeneric>)args[0])); })
            .Production("primary : TRUE",
                args => { return instance.PrimaryBool(((Token<IndentedWhileTokenGeneric>)args[0])); })
            .Production("primary : FALSE",
                args => { return instance.PrimaryBool(((Token<IndentedWhileTokenGeneric>)args[0])); })
            .Production("primary : STRING",
                args => { return instance.PrimaryString(((Token<IndentedWhileTokenGeneric>)args[0])); })
            .Production("primary : IDENTIFIER",
                args => { return instance.PrimaryId(((Token<IndentedWhileTokenGeneric>)args[0])); })
            .Operand("operand : primary", args => instance.Operand((WhileAST)args[0]))
            .Right(10, IndentedWhileTokenGeneric.MINUS, numericTerm)
            .Right(10, IndentedWhileTokenGeneric.PLUS, numericTerm)
            .Right(50, IndentedWhileTokenGeneric.TIMES, numericFactor)
            .Right(50, IndentedWhileTokenGeneric.DIVIDE, numericFactor)
            .Prefix(100, IndentedWhileTokenGeneric.MINUS,
                args => instance.unaryNumericExpression((Token<IndentedWhileTokenGeneric>)args[0], (WhileAST)args[1]))
            .Right(10, IndentedWhileTokenGeneric.OR,
                args => instance.binaryOrExpression((WhileAST)args[0], (Token<IndentedWhileTokenGeneric>)args[1],
                    (WhileAST)args[2]))
            .Right(50, IndentedWhileTokenGeneric.AND,
                args => instance.binaryAndExpression((WhileAST)args[0], (Token<IndentedWhileTokenGeneric>)args[1],
                    (WhileAST)args[2]))
            .Prefix(100, IndentedWhileTokenGeneric.NOT,
                args => instance.unaryNotExpression((Token<IndentedWhileTokenGeneric>)args[0], (WhileAST)args[1]))
            .UseMemoization()
            .UseAutoCloseIndentations()
            .WithLexerbuilder(BuildAotWhileLexer())
            ;
        
        return builder;
    }
}