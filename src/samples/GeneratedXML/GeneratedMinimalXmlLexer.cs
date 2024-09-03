﻿using System;
using sly.lexer;

namespace XML
{
    [Lexer()]
    public enum GeneratedMinimalXmlLexer
    {
        EOS = 0,
        
        
        #region sea
        [Sugar("<",1512)]
        [Mode()]
        [Push("tag")]
        OPEN,
        
        [UpTo("<")]
        CONTENT,
        
        [Sugar("<?",48)]
        [Mode]
        [Push("pi")]
        OPEN_PI,


        #endregion
        
        
        [MultiLineComment("<!--","-->",channel:Channels.Main)]
        [Mode]
        COMMENT,
        
        
        #region pi
        
        #endregion
        
        
        #region in tag
        
        [AlphaId]
        [Mode("tag")]
        [Mode("pi")]
        ID,
        
        [Sugar("/")]
        [Mode("tag")]
        SLASH,
        
        [Sugar("=")]
        [Mode("tag","pi")]
        EQUALS,
        
        [String]
        [Mode("tag","pi")]
        [Mode("pi")]
        VALUE,
        
        [Sugar("?>")]
        [Mode("pi")]
        [Pop]
        CLOSE_PI,
        
        
        
        [Sugar(">")]
        [Mode("tag")]
        [Pop]
        CLOSE,
        
        #endregion
        
        
        
    }
}