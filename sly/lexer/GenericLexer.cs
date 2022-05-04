﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using sly.buildresult;
using sly.i18n;
using sly.lexer.fsm;

namespace sly.lexer
{
    public enum GenericToken
    {
        Default,
        Identifier,
        Int,
        Double,
        KeyWord,
        String,
        Char,
        SugarToken,

        Extension,

        Comment
    }

    public enum IdentifierType
    {
        Alpha,
        AlphaNumeric,
        AlphaNumericDash,
        Custom
    }

    public enum EOLType
    {
        Windows,
        Nix,

        Mac,
        Environment,

        No
    }

    public class GenericLexer<IN> : ILexer<IN> where IN : struct
    {
        public class Config
        {
            public Config()
            {
                IdType = IdentifierType.Alpha;
                IgnoreEOL = true;
                IgnoreWS = true;
                WhiteSpace = new[] { ' ', '\t' };
                
            }

            public IdentifierType IdType { get; set; }

            public bool IgnoreEOL { get; set; }

            public bool IgnoreWS { get; set; }

            public char[] WhiteSpace { get; set; }
            
            public bool KeyWordIgnoreCase { get; set; }
            
            public bool IndentationAware { get; set; }
            
            public string Indentation { get; set; }
            
            public IEnumerable<char[]> IdentifierStartPattern { get; set; }
            
            public IEnumerable<char[]> IdentifierRestPattern { get; set; }

            public BuildExtension<IN> ExtensionBuilder { get; set; }

            public IEqualityComparer<string> KeyWordComparer => KeyWordIgnoreCase ? StringComparer.OrdinalIgnoreCase : null;
        }
        
        public LexerPostProcess<IN> LexerPostProcess { get; set; }
        
        public string I18n { get; set; }

        public const string in_string = "in_string";
        public const string string_end = "string_end";
        public const string start_char = "start_char";
        public const string escapeChar_char = "escapeChar_char";
        public const string unicode_char = "unicode_char";
        public const string in_char = "in_char";
        public const string end_char = "char_end";
        public const string start = "start";
        public const string in_int = "in_int";
        public const string start_double = "start_double";
        public const string in_double = "in_double";
        public const string in_identifier = "in_identifier";
        public const string token_property = "token";
        public const string DerivedToken = "derivedToken";
        public const string defaultIdentifierKey = "identifier";
        public const string escape_string = "escape_string";
        public const string escape_char = "escape_char";

        public const string single_line_comment_start = "single_line_comment_start";

        public const string multi_line_comment_start = "multi_line_comment_start";

        protected readonly Dictionary<GenericToken, Dictionary<string, IN>> derivedTokens;
        protected IN doubleDerivedToken;
        protected char EscapeStringDelimiterChar;

        protected readonly BuildExtension<IN> ExtensionBuilder;
        public FSMLexerBuilder<GenericToken> FSMBuilder;
        protected IN identifierDerivedToken;
        protected IN intDerivedToken;

        protected FSMLexer<GenericToken> LexerFsm;
        protected int StringCounter;
        protected int CharCounter;


        protected Dictionary<IN, Func<Token<IN>, Token<IN>>> CallBacks = new Dictionary<IN, Func<Token<IN>, Token<IN>>>();

        protected char StringDelimiterChar;

        private readonly IEqualityComparer<string> KeyWordComparer;

        public GenericLexer(IdentifierType idType = IdentifierType.Alpha,
                            BuildExtension<IN> extensionBuilder = null,
                            params GenericToken[] staticTokens)
            : this(new Config { IdType = idType, ExtensionBuilder = extensionBuilder }, staticTokens)
        { }

        public GenericLexer(Config config, GenericToken[] staticTokens)
        {
            derivedTokens = new Dictionary<GenericToken, Dictionary<string, IN>>();
            ExtensionBuilder = config.ExtensionBuilder;
            KeyWordComparer = config.KeyWordComparer;
            InitializeStaticLexer(config, staticTokens);
        }

        public string SingleLineComment { get; set; }

        public string MultiLineCommentStart { get; set; }

        public string MultiLineCommentEnd { get; set; }

        public void AddCallBack(IN token, Func<Token<IN>, Token<IN>> callback)
        {
            CallBacks[token] = callback;
        }

        public void AddDefinition(TokenDefinition<IN> tokenDefinition) { }


        public LexerResult<IN> Tokenize(string source)
        {
            var memorySource = new ReadOnlyMemory<char>(source.ToCharArray());
            return Tokenize(memorySource);
        }
        
        public LexerResult<IN> Tokenize(ReadOnlyMemory<char> memorySource)
        {
            LexerPosition position = new LexerPosition();
            
            var tokens = new List<Token<IN>>();
            string src = memorySource.ToString();
            if (src.Contains("commented"))
            {
                ;
            }
            
            var r = LexerFsm.Run(memorySource, new LexerPosition());
            
            var ignored = r.IgnoredTokens.Select(x =>
                new Token<IN>(default(IN), x.SpanValue, x.Position, x.IsComment,
                    x.CommentType, x.Channel)).ToList();
            tokens.AddRange(ignored);
            
            
            switch (r.IsSuccess)
            {
                case false when !r.IsEOS:
                {
                    var result = r.Result;
                    var error = new LexicalError(result.Position.Line, result.Position.Column, result.CharValue, I18n);
                    return new LexerResult<IN>(error);
                }
                case true when r.Result.IsComment:
                    position = r.NewPosition;
                    position = ConsumeComment(r.Result, memorySource, position);
                    break;
                case true when !r.Result.IsComment:
                    position = r.NewPosition;
                    break;
            }

            while (r.IsSuccess)
            {
                ComputePositionWhenIgnoringEOL(r, tokens);
                var transcoded = Transcode(r);
                
                if (CallBacks.TryGetValue(transcoded.TokenID, out var callback))
                {
                    transcoded = callback(transcoded);
                }
                
                if (transcoded.IsLineEnding)
                {
                    ComputePositionWhenIgnoringEOL(r, tokens);
                }

                if (r.IsUnIndent && r.UnIndentCount > 1)
                {
                    for (int i = 1; i < r.UnIndentCount; i++)
                    {
                        tokens.Add(transcoded);
                    }   
                }
                tokens.Add(transcoded);
                r = LexerFsm.Run(memorySource,position);
                switch (r.IsSuccess)
                {
                    case false when !r.IsEOS:
                    {
                        if (r.IsIndentationError)
                        {
                            var result = r.Result;
                            var error = new IndentationError(result.Position.Line, result.Position.Column,I18n);
                            return new LexerResult<IN>(error);
                        }
                        else
                        {
                            var result = r.Result;
                            var error = new LexicalError(result.Position.Line, result.Position.Column, result.CharValue,I18n);
                            return new LexerResult<IN>(error);
                        }
                    }
                    case true when r.Result.IsComment:
                        position = r.NewPosition;
                        position = ConsumeComment(r.Result, memorySource, position);
                        break;
                    case true when !r.Result.IsComment:
                        position = r.NewPosition;
                        break;
                }
            }

            var eos = new Token<IN>();
            var prev = tokens.LastOrDefault<Token<IN>>();
            if (prev == null)
            {
                eos.Position = new LexerPosition(1, 0, 0);
            }
            else
            {
                eos.Position = new LexerPosition(prev.Position.Index + 1, prev.Position.Line,
                    prev.Position.Column + prev.Value.Length);
            }
            tokens.Add(eos);
            return new LexerResult<IN>(tokens);
        }

        private void ComputePositionWhenIgnoringEOL(FSMMatch<GenericToken> r, List<Token<IN>> tokens)
        {
            if (!LexerFsm.IgnoreEOL)            
            {
                var newPosition = r.Result.Position.Clone();

                if (r.IsLineEnding) // only compute if token is eol
                {
                    var eols = tokens.Where<Token<IN>>(t => t.IsLineEnding).ToList<Token<IN>>();
                    int line = eols.Any<Token<IN>>() ? eols.Count : 0;
                    int column = 0;
                    int index = newPosition.Index;
                    // r.Result.Position.Line = line+1;
                    r.NewPosition.Line = line+1;
                    r.NewPosition.Column = column;
                }
            }                        
        }


        private void InitializeStaticLexer(Config config, GenericToken[] staticTokens)
        {
            FSMBuilder = new FSMLexerBuilder<GenericToken>();
            StringCounter = 0;

            // conf
            FSMBuilder
                .IgnoreWS(config.IgnoreWS)
                .WhiteSpace(config.WhiteSpace)
                .IgnoreEOL(config.IgnoreEOL)
                .Indentation(config.IndentationAware, config.Indentation);
            
            

            // start machine definition
            FSMBuilder.Mark(start);

            if (staticTokens.Contains<GenericToken>(GenericToken.Identifier) || staticTokens.Contains<GenericToken>(GenericToken.KeyWord))
            {
                InitializeIdentifier(config);
            }

            // numeric
            if (staticTokens.Contains<GenericToken>(GenericToken.Int) || staticTokens.Contains<GenericToken>(GenericToken.Double))
            {
                FSMBuilder = FSMBuilder.GoTo(start)
                    .RangeTransition('0', '9')
                    .Mark(in_int)
                    .RangeTransitionTo('0', '9', in_int)
                    .End(GenericToken.Int);
                if (staticTokens.Contains<GenericToken>(GenericToken.Double))
                    FSMBuilder.Transition('.')
                        .Mark(start_double)
                        .RangeTransition('0', '9')
                        .Mark(in_double)
                        .RangeTransitionTo('0', '9', in_double)
                        .End(GenericToken.Double);
            }

            LexerFsm = FSMBuilder.Fsm;
        }


        private void InitializeIdentifier(Config config)
        {
            // identifier
            if (config.IdType == IdentifierType.Custom)
            {
                var marked = false;
                foreach (var pattern in config.IdentifierStartPattern)
                {
                    FSMBuilder.GoTo(start);
                    if (pattern.Length == 1)
                    {
                        if (marked)
                        {
                            FSMBuilder.TransitionTo(pattern[0], in_identifier);
                        }
                        else
                        {
                            FSMBuilder.Transition(pattern[0]).Mark(in_identifier).End(GenericToken.Identifier);
                            marked = true;
                        }
                    }
                    else
                    {
                        if (marked)
                        {
                            FSMBuilder.RangeTransitionTo(pattern[0], pattern[1], in_identifier);
                        }
                        else
                        {
                            FSMBuilder.RangeTransition(pattern[0], pattern[1]).Mark(in_identifier).End(GenericToken.Identifier);
                            marked = true;
                        }
                    }
                }

                foreach (var pattern in config.IdentifierRestPattern)
                {
                    if (pattern.Length == 1)
                    {
                        FSMBuilder.TransitionTo(pattern[0], in_identifier);
                    }
                    else
                    {
                        FSMBuilder.RangeTransitionTo(pattern[0], pattern[1], in_identifier);
                    }
                }
            }
            else
            {
                FSMBuilder
                    .GoTo(start)
                    .RangeTransition('a', 'z')
                    .Mark(in_identifier)
                    .GoTo(start)
                    .RangeTransitionTo('A', 'Z', in_identifier)
                    .RangeTransitionTo('a', 'z', in_identifier)
                    .RangeTransitionTo('A', 'Z', in_identifier)
                    .End(GenericToken.Identifier);

                if (config.IdType == IdentifierType.AlphaNumeric || config.IdType == IdentifierType.AlphaNumericDash)
                {
                    FSMBuilder
                        .GoTo(in_identifier)
                        .RangeTransitionTo('0', '9', in_identifier);
                }
            
                if (config.IdType == IdentifierType.AlphaNumericDash)
                {
                    FSMBuilder
                        .GoTo(start)
                        .TransitionTo('_', in_identifier)
                        .TransitionTo('_', in_identifier)
                        .TransitionTo('-', in_identifier);
                }
            }
        }

        public void AddLexeme(GenericToken generic, IN token)
        {
            NodeCallback<GenericToken> callback = match =>
            {
                switch (match.Result.TokenID)
                {
                    case GenericToken.Identifier:
                        {
                            if (derivedTokens.ContainsKey(GenericToken.Identifier))
                            {
                                var possibleTokens = derivedTokens[GenericToken.Identifier];
                                if (possibleTokens.ContainsKey(match.Result.Value))
                                    match.Properties[DerivedToken] = possibleTokens[match.Result.Value];
                                else
                                    match.Properties[DerivedToken] = identifierDerivedToken;
                            }
                            else
                            {
                                match.Properties[DerivedToken] = identifierDerivedToken;
                            }

                            break;
                        }
                    case GenericToken.Int:
                        {
                            match.Properties[DerivedToken] = intDerivedToken;
                            break;
                        }
                    case GenericToken.Double:
                        {
                            match.Properties[DerivedToken] = doubleDerivedToken;
                            break;
                        }
                    default:
                        {
                            match.Properties[DerivedToken] = token;
                            break;
                        }
                }

                return match;
            };

            switch (generic)
            {
                case GenericToken.Double:
                    {
                        doubleDerivedToken = token;
                        FSMBuilder.GoTo(in_double);
                        FSMBuilder.CallBack(callback);
                        break;
                    }
                case GenericToken.Int:
                    {
                        intDerivedToken = token;
                        FSMBuilder.GoTo(in_int);
                        FSMBuilder.CallBack(callback);
                        break;
                    }
                case GenericToken.Identifier:
                    {
                        identifierDerivedToken = token;
                        FSMBuilder.GoTo(in_identifier);
                        FSMBuilder.CallBack(callback);
                        break;
                    }
            }
        }

        public void AddLexeme(GenericToken genericToken,BuildResult<ILexer<IN>> result, IN token, string specialValue)
        {
            if (genericToken == GenericToken.SugarToken)
            {
                AddSugarLexem(token, result, specialValue);
            }

            if (!derivedTokens.TryGetValue(genericToken, out var tokensForGeneric))
            {
                if (genericToken == GenericToken.Identifier)
                {
                    tokensForGeneric = new Dictionary<string, IN>(KeyWordComparer);
                }
                else
                {
                    tokensForGeneric = new Dictionary<string, IN>();
                }

                derivedTokens[genericToken] = tokensForGeneric;
            }

            tokensForGeneric[specialValue] = token;
        }

        public void AddKeyWord(IN token, string keyword, BuildResult<ILexer<IN>> result )
        {
            NodeCallback<GenericToken> callback = match =>
            {
                IN derivedToken;
                if (derivedTokens.TryGetValue(GenericToken.Identifier, out var derived))
                {
                    if (!derived.TryGetValue(match.Result.Value, out derivedToken))
                    {
                        derivedToken = identifierDerivedToken;
                    }
                }
                else
                {
                    derivedToken = identifierDerivedToken;
                }

                match.Properties[DerivedToken] = derivedToken;

                return match;
            };

            AddLexeme(GenericToken.Identifier, result, token, keyword);
            var node = FSMBuilder.GetNode(in_identifier);
            if (!FSMBuilder.Fsm.HasCallback(node.Id))
            {
                FSMBuilder.GoTo(in_identifier).CallBack(callback);
            }
        }


        public ReadOnlyMemory<char> diffCharEscaper(char escapeStringDelimiterChar, char stringDelimiterChar, ReadOnlyMemory<char> stringValue)
        {
            var value = stringValue;
            var i = 1;
            var substitutionHappened = false;
            var escaping = false;
            var r = string.Empty;
            while (i < value.Length - 1)
            {
                var current = value.At<char>(i);
                if (current == escapeStringDelimiterChar && i < value.Length - 2)
                {
                    escaping = true;
                    if (!substitutionHappened)
                    {
                        r = value.Slice(0, i).ToString();
                        substitutionHappened = true;
                    }
                }
                else
                {
                    if (escaping)
                    {
                        if (current != stringDelimiterChar)
                        {
                            r += escapeStringDelimiterChar;
                        }
                        escaping = false;
                    }
                    if (substitutionHappened)
                    {
                        r += current;
                    }
                }
                i++;
            }
            if (substitutionHappened)
            {
                r += value.At<char>(value.Length - 1);
                value = r.AsMemory();
            }

            return value;
        }

        public ReadOnlyMemory<char> sameCharEscaper(char escapeStringDelimiterChar, char stringDelimiterChar, ReadOnlyMemory<char> stringValue)
        {
            var value = stringValue;
            int i = 1;
            bool substitutionHappened = false;
            bool escaping = false;
            string r = string.Empty;
            while (i < value.Length - 1)
            {
                char current = value.At<char>(i);
                if (current == escapeStringDelimiterChar && !escaping && i < value.Length - 2)
                {
                    escaping = true;
                    if (!substitutionHappened)
                    {
                        r = value.Slice(0, i).ToString();
                        substitutionHappened = true;
                    }
                }
                else
                {
                    if (escaping)
                    {
                        r += escapeStringDelimiterChar;
                        escaping = false;
                    }
                    else if (substitutionHappened)
                    {
                        r += current;
                    }
                }
                i++;
            }
            if (substitutionHappened)
            {
                r += value.At<char>(value.Length - 1);
                value = r.AsMemory();
            }

            return value;
        }

        public void AddStringLexem(IN token,  BuildResult<ILexer<IN>> result , string stringDelimiter,
            string escapeDelimiterChar = "\\")
        {
            if (string.IsNullOrEmpty(stringDelimiter) || stringDelimiter.Length > 1)
                result.AddError(new LexerInitializationError(ErrorLevel.FATAL,
                    I18N.Instance.GetText(I18n,Message.StringDelimiterMustBe1Char,stringDelimiter,token.ToString()),
                    ErrorCodes.LEXER_STRING_DELIMITER_MUST_BE_1_CHAR));
            if (stringDelimiter.Length == 1 && char.IsLetterOrDigit(stringDelimiter[0]))
                result.AddError(new InitializationError(ErrorLevel.FATAL,
                    I18N.Instance.GetText(I18n,Message.StringDelimiterCannotBeLetterOrDigit,stringDelimiter,token.ToString()),
                    ErrorCodes.LEXER_STRING_DELIMITER_CANNOT_BE_LETTER_OR_DIGIT));

            if (string.IsNullOrEmpty(escapeDelimiterChar) || escapeDelimiterChar.Length > 1)
                result.AddError(new InitializationError(ErrorLevel.FATAL,
                    I18N.Instance.GetText(I18n,Message.StringEscapeCharMustBe1Char,escapeDelimiterChar,token.ToString()),
                    ErrorCodes.LEXER_STRING_ESCAPE_CHAR_MUST_BE_1_CHAR));
            if (escapeDelimiterChar.Length == 1 && char.IsLetterOrDigit(escapeDelimiterChar[0]))
                result.AddError(new InitializationError(ErrorLevel.FATAL,
                    I18N.Instance.GetText(I18n,Message.StringEscapeCharCannotBeLetterOrDigit,escapeDelimiterChar,token.ToString()),
                    ErrorCodes.LEXER_STRING_ESCAPE_CHAR_CANNOT_BE_LETTER_OR_DIGIT));

            StringDelimiterChar = (char)0;
            var stringDelimiterChar = (char)0;

            EscapeStringDelimiterChar = (char)0;
            var escapeStringDelimiterChar = (char)0;
            
            if (stringDelimiter.Length == 1)
            {
                StringCounter++;

                StringDelimiterChar = stringDelimiter[0];
                stringDelimiterChar = stringDelimiter[0];

                EscapeStringDelimiterChar = escapeDelimiterChar[0];
                escapeStringDelimiterChar = escapeDelimiterChar[0];
            }


            NodeCallback<GenericToken> callback = match =>
            {
                match.Properties[DerivedToken] = token;
                var value = match.Result.SpanValue;

                match.Result.SpanValue = value;

                match.StringDelimiterChar = stringDelimiterChar;
                match.IsString = true;
                if (stringDelimiterChar != escapeStringDelimiterChar)
                {
                    match.Result.SpanValue = diffCharEscaper(escapeStringDelimiterChar,stringDelimiterChar, match.Result.SpanValue);
                }
                else
                {                   
                    match.Result.SpanValue = sameCharEscaper(escapeStringDelimiterChar,stringDelimiterChar, match.Result.SpanValue);
                }

                return match;
            };

            if (stringDelimiterChar != escapeStringDelimiterChar)
            {

                FSMBuilder.GoTo(start);
                FSMBuilder.Transition(stringDelimiterChar)
                    .Mark(in_string + StringCounter)
                    .ExceptTransitionTo(new[] { stringDelimiterChar, escapeStringDelimiterChar },
                        in_string + StringCounter)
                    .Transition(escapeStringDelimiterChar)
                    .Mark(escape_string + StringCounter)
                    .ExceptTransitionTo(new[] { stringDelimiterChar }, in_string + StringCounter)
                    .GoTo(escape_string + StringCounter)
                    .TransitionTo(stringDelimiterChar, in_string + StringCounter)
                    .Transition(stringDelimiterChar)
                    .End(GenericToken.String)
                    .Mark(string_end + StringCounter)
                    .CallBack(callback);
                FSMBuilder.Fsm.StringDelimiter = stringDelimiterChar;
            }
            else
            {
                var exceptDelimiter = new[] { stringDelimiterChar };
                var in_string = "in_string_same";
                var escaped = "escaped_same";
                var delim = "delim_same";

                FSMBuilder.GoTo(start)
                    .Transition(stringDelimiterChar)
                    .Mark(in_string + StringCounter)
                    .ExceptTransitionTo(exceptDelimiter, in_string + StringCounter)
                    .Transition(stringDelimiterChar)
                    .Mark(escaped + StringCounter)
                    .End(GenericToken.String)
                    .CallBack(callback)
                    .Transition(stringDelimiterChar)
                    .Mark(delim + StringCounter)
                    .ExceptTransitionTo(exceptDelimiter, in_string + StringCounter);

                FSMBuilder.GoTo(delim + StringCounter)
                    .TransitionTo(stringDelimiterChar, escaped + StringCounter)
                    .ExceptTransitionTo(exceptDelimiter, in_string + StringCounter);
            }
        }
        
        public void AddCharLexem(IN token, BuildResult<ILexer<IN>> result ,string charDelimiter, string escapeDelimiterChar = "\\")
        {
            if (string.IsNullOrEmpty(charDelimiter) || charDelimiter.Length > 1)
               result.AddError(new InitializationError(ErrorLevel.FATAL,
                   I18N.Instance.GetText(I18n,Message.CharDelimiterMustBe1Char,charDelimiter,token.ToString()),
                    ErrorCodes.LEXER_CHAR_DELIMITER_MUST_BE_1_CHAR));
            if (charDelimiter.Length == 1 && char.IsLetterOrDigit(charDelimiter[0]))
                result.AddError(new InitializationError(ErrorLevel.FATAL,
                    I18N.Instance.GetText(I18n, Message.CharDelimiterCannotBeLetter,charDelimiter,token.ToString()), 
                    ErrorCodes.LEXER_CHAR_DELIMITER_CANNOT_BE_LETTER));

            if (string.IsNullOrEmpty(escapeDelimiterChar) || escapeDelimiterChar.Length > 1)
                result.AddError(new InitializationError(ErrorLevel.FATAL,
                    I18N.Instance.GetText(I18n,Message.CharEscapeCharMustBe1Char,escapeDelimiterChar,token.ToString()),
                    ErrorCodes.LEXER_CHAR_ESCAPE_CHAR_MUST_BE_1_CHAR));
            if (escapeDelimiterChar.Length == 1 && char.IsLetterOrDigit(escapeDelimiterChar[0]))
                result.AddError(new InitializationError(ErrorLevel.FATAL,
                    I18N.Instance.GetText(I18n,Message.CharEscapeCharCannotBeLetterOrDigit,escapeDelimiterChar,token.ToString()),
                    ErrorCodes.LEXER_CHAR_ESCAPE_CHAR_CANNOT_BE_LETTER_OR_DIGIT));

            CharCounter++;

            var charDelimiterChar = charDelimiter[0];

            var escapeChar = escapeDelimiterChar[0];


            NodeCallback<GenericToken> callback = match =>
            {
                match.Properties[DerivedToken] = token;
                var value = match.Result.SpanValue;

                match.Result.SpanValue = value;
                return match;
            };

            FSMBuilder.GoTo(start);
            FSMBuilder.Transition(charDelimiterChar)
                .Mark(start_char+"_"+CharCounter)
                .ExceptTransition(new[] { charDelimiterChar, escapeChar })
                .Mark(in_char+"_"+CharCounter)
                .Transition(charDelimiterChar)
                .Mark(end_char+"_"+CharCounter)
                .End(GenericToken.Char)
                .CallBack(callback)
                .GoTo(start_char+"_"+CharCounter)
                .Transition(escapeChar)
                .Mark(escapeChar_char+"_"+CharCounter)
                .ExceptTransitionTo(new[] { 'u' }, in_char + "_" + CharCounter)
                .CallBack(callback);
            FSMBuilder.Fsm.StringDelimiter = charDelimiterChar;
            
            // unicode transitions ?
            FSMBuilder = FSMBuilder.GoTo(escapeChar_char + "_" + CharCounter)
            .Transition('u')
            .Mark(unicode_char+"_"+CharCounter)
            .RepetitionTransitionTo(in_char + "_" + CharCounter,4,"[0-9,a-z,A-Z]");

        }

        public void AddSugarLexem(IN token, BuildResult<ILexer<IN>> buildResult, string specialValue, bool isLineEnding = false)
        {
            if (char.IsLetter(specialValue[0]))
            {
                buildResult.AddError(new InitializationError(ErrorLevel.FATAL,
                    I18N.Instance.GetText(I18n,Message.SugarTokenCannotStartWithLetter,specialValue,token.ToString()),
                    ErrorCodes.LEXER_SUGAR_TOKEN_CANNOT_START_WITH_LETTER));
                return;
            }
                
            NodeCallback<GenericToken> callback = match =>
            {
                match.Properties[DerivedToken] = token;
                return match;
            };

            FSMBuilder.GoTo(start);
            for (var i = 0; i < specialValue.Length; i++) FSMBuilder.SafeTransition(specialValue[i]);
            FSMBuilder.End(GenericToken.SugarToken, isLineEnding)
                .CallBack(callback);
        }

        public LexerPosition ConsumeComment(Token<GenericToken> comment, ReadOnlyMemory<char> source, LexerPosition lexerPosition)
        {
           ReadOnlyMemory<char> commentValue;

            if (comment.IsSingleLineComment)
            {
                var position = lexerPosition.Index;
                commentValue = EOLManager.GetToEndOfLine(source, position);
                position = position + commentValue.Length;
                comment.SpanValue = commentValue;
                return new LexerPosition(position, lexerPosition.Line + 1, 0);
                //LexerFsm.MovePosition(position, LexerFsm.CurrentLine + 1, 0);
            }
            else if (comment.IsMultiLineComment)
            {
                var position = lexerPosition.Index;

                var end = source.Span.Slice(position).IndexOf<char>(MultiLineCommentEnd.AsSpan());
                if (end < 0)
                    position = source.Length;
                else
                    position = end + position;
                commentValue = source.Slice(lexerPosition.Index, position - lexerPosition.Index);
                comment.SpanValue = commentValue;

                var newPosition = lexerPosition.Index + commentValue.Length + MultiLineCommentEnd.Length;
                var lines = EOLManager.GetLinesLength(commentValue);
                var newLine = lexerPosition.Line + lines.Count - 1;
                int newColumn;
                if (lines.Count > 1)
                    newColumn = lines.Last<int>() + MultiLineCommentEnd.Length;
                else
                    newColumn = lexerPosition.Column + lines[0] + MultiLineCommentEnd.Length;

                return new LexerPosition(newPosition, newLine, newColumn);
                // LexerFsm.MovePosition(newPosition, newLine, newColumn);
            }

            return lexerPosition;
        }

        public Token<IN> Transcode(FSMMatch<GenericToken> match)
        {
            var tok = new Token<IN>();
            var inTok = match.Result;
            tok.IsComment = inTok.IsComment;
            tok.IsEmpty = inTok.IsEmpty;
            tok.SpanValue = inTok.SpanValue;
            tok.CommentType = inTok.CommentType;
            tok.Position = inTok.Position;
            tok.Discarded = inTok.Discarded;
            tok.StringDelimiter = match.StringDelimiterChar;
            tok.TokenID = match.Properties.ContainsKey(DerivedToken) ? (IN) match.Properties[DerivedToken] : default(IN);
            tok.IsLineEnding = match.IsLineEnding;
            tok.IsEOS = match.IsEOS;
            tok.IsIndent = match.IsIndent;
            tok.IsUnIndent = match.IsUnIndent;
            tok.IndentationLevel = match.IndentationLevel;
            tok.Notignored = match.Result.Notignored;
            tok.Channel = match.Result.Channel;
            return tok;
        }

        [ExcludeFromCodeCoverage]
        public override string ToString()
        {
            return LexerFsm.ToString();
        }
        
        public string ToGraphViz()
        {
            return LexerFsm.ToGraphViz();
        }
        
    }
}