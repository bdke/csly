using System;
using System.Collections.Generic;
using System.Text;
using csly.whileLang.compiler;
using Sigil;
using sly.lexer;

namespace csly.whileLang.model;

public class FString : Expression
{
    public LexerPosition Position { get; set; }
    public Scope CompilerScope { get; set; }

    public List<FStringElement> Elements { get; set; }

    public FString(List<FStringElement> elements, LexerPosition position)
    {
        this.Elements = elements;
        Position = position;
    }

    public string Dump(string tab)
    {
        StringBuilder builder = new StringBuilder();
        builder.Append($"{{tab}}\"");
        foreach (var element in Elements)
        {
            builder.Append(element.Dump(""));
        }

        builder.Append("\"");
        return builder.ToString();
    }

    public string Transpile(CompilerContext context)
    {
        throw new NotImplementedException();
    }

    public Emit<Func<int>> EmitByteCode(CompilerContext context, Emit<Func<int>> emiter)
    {
        throw new NotImplementedException();
    }

    public WhileType Whiletype { get; set; } = WhileType.STRING;
}