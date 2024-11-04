using System;
using csly.whileLang.compiler;
using Sigil;
using sly.lexer;

namespace csly.whileLang.model;

public class FStringElement : WhileAST
{

    public LexerPosition Position { get; set; }

    public Scope CompilerScope { get; set; }
    public Variable VariableElement { get; set; }

    public StringConstant StringElement { get; set; }

    public bool IsStringElement => StringElement != null;

    public bool IsVariable => VariableElement != null;

    public FStringElement(Variable variableElement, LexerPosition position)
    {
        VariableElement = variableElement;
        Position = position;
    }

    public FStringElement(StringConstant stringElement, LexerPosition position)
    {
        StringElement = stringElement;
        Position = position;
    }


    public string Dump(string tab)
    {
        if (IsStringElement)
        {
            return tab + StringElement.Value;
        }
        else
        {
            return $"{{tab}}{{{{{VariableElement.Dump("")}}}";
        }
    }

    public string Transpile(CompilerContext context)
    {
        throw new NotImplementedException();
    }

    public Emit<Func<int>> EmitByteCode(CompilerContext context, Emit<Func<int>> emiter)
    {
        throw new NotImplementedException();
    }
}