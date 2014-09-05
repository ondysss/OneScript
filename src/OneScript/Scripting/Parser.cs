﻿using OneScript.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace OneScript.Scripting
{
    public class Parser : ILexemExtractor
    {
        private Lexer _lexer;
        private Lexem _lastExtractedLexem;
        private IModuleBuilder _builder;
        private ICompilerContext _ctx;
        private bool _wasErrorsInBuild;
        private PositionInModule _parserPosition;

        private enum PositionInModule
        {
            Begin,
            VarSection,
            MethodSection,
            MethodHeader,
            MethodVarSection,
            MethodBody,
            ModuleBody
        }

        public Parser(IModuleBuilder builder)
        {
            _builder = builder;
        }

        public bool Build(ICompilerContext context, Lexer lexer)
        {
            _lexer = lexer;
            _ctx = context;
            _lastExtractedLexem = default(Lexem);
            _lexer.UnexpectedCharacterFound += _lexer_UnexpectedCharacterFound;

            return BuildModule();
        }

        void _lexer_UnexpectedCharacterFound(object sender, LexerErrorEventArgs e)
        {
            // синтаксические ошибки пока не обрабатываются.
        }

        public event EventHandler<CompilerErrorEventArgs> CompilerError;

        private bool BuildModule()
        {
            try
            {
                _ctx.PushScope(new SymbolScope());
                _builder.BeginModule(_ctx);

                DispatchModuleBuild();
                ProcessForwardedDeclarations();

            }
            catch (ScriptException e)
            {
                ReportError(e);
            }
            catch(Exception e)
            {
                var newExc = new CompilerException(new CodePositionInfo(), "Внутренняя ошибка компилятора", e);
                throw newExc;
            }
            finally
            {
                _builder.CompleteModule();
                _ctx.PopScope();
            }

            return !_wasErrorsInBuild;
        }

        private void DispatchModuleBuild()
        {
            NextLexem();

            do
            {
                bool success = false;
                try
                {
                    success = SelectAndBuildOperation();
                }
                catch(CompilerException e)
                {
                    ReportError(e);
                    success = false;
                }

                if (success && CheckCorrectStatementEnd())
                {
                    // это точка с запятой или конец блока
                    if (_lastExtractedLexem.Token != Token.EndOfText)
                        NextLexem();
                }
                else
                {
                    SkipToNextStatement();
                }

            }
            while (_lastExtractedLexem.Token != Token.EndOfText);
        }

        private void ProcessForwardedDeclarations()
        {
            //throw new NotImplementedException();
        }

        private bool SelectAndBuildOperation()
        {
            bool success = false;

            if (_lastExtractedLexem.Token == Token.VarDef)
            {
                SetPosition(PositionInModule.VarSection);
                
                success = BuildVariableDefinition();
            }
            else if (_lastExtractedLexem.Token == Token.Procedure || _lastExtractedLexem.Token == Token.Function)
            {

            }
            else if (_lastExtractedLexem.Type == LexemType.Identifier)
            {
                if (_parserPosition == PositionInModule.VarSection || _parserPosition == PositionInModule.MethodSection)
                    SetPosition(PositionInModule.ModuleBody);

                success = BuildStatement();
            }
            else
            {
                success = false;
                ReportError(CompilerException.UnexpectedOperation());
            }

            return success;
        }

        private bool BuildVariableDefinition()
        {
            Debug.Assert(_lastExtractedLexem.Token == Token.VarDef);
            
            NextLexem();
            while (true)
            {
                if (LanguageDef.IsUserSymbol(ref _lastExtractedLexem))
                {
                    var symbolicName = _lastExtractedLexem.Content;
                    NextLexem();

                    if (_lastExtractedLexem.Token == Token.Export)
                    {
                        _builder.BuildExportVariable(symbolicName);
                        NextLexem();
                    }
                    else
                    {
                        _builder.BuildVariable(symbolicName);
                    }

                    if (_lastExtractedLexem.Token == Token.Comma)
                    {
                        NextLexem();
                        continue;
                    }
                    else
                    {
                        // переменная объявлена.
                        // далее, диспетчер определит - нужна ли точка с запятой
                        // и переведет обработку дальше
                        break;
                    }
                }
                else
                {
                    ReportError(CompilerException.IdentifierExpected());
                    return false;
                }
            }

            return true;
    
        }

        private bool BuildStatement()
        {
            Debug.Assert(_lastExtractedLexem.Type == LexemType.Identifier);

            if(LanguageDef.IsBeginOfStatement(_lastExtractedLexem.Token))
            {
                throw new NotImplementedException();
            }
            else if(LanguageDef.IsUserSymbol(ref _lastExtractedLexem))
            {
                return BuildSimpleStatement();
            }
            else
            {
                ReportError(CompilerException.UnexpectedOperation());
                return false;
            }
        }

        private bool BuildSimpleStatement()
        {
            string identifier = _lastExtractedLexem.Content;

            NextLexem();

            switch(_lastExtractedLexem.Token)
            {
                case Token.Equal:
                    // simple assignment
                    NextLexem();
                    if(BuildSourceExpression(Token.Semicolon))
                    {
                        var sb = DefineOrGetVariable(identifier);
                        _builder.BuildLoadVariable(sb);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case Token.Dot:
                    // access chain
                    throw new NotImplementedException();
                    break;
                case Token.OpenPar:
                    // call
                    throw new NotImplementedException();
                    break;
                case Token.OpenBracket:
                    // access by index
                    throw new NotImplementedException();
                    break;
                default:
                    ReportError(CompilerException.UnexpectedOperation());
                    return false;
            }
        }

        private bool BuildSourceExpression(Token stopToken)
        {
            var exprBuilder = new ExpressionBuilder(_builder, this, _ctx);
            try
            {
                exprBuilder.Build(stopToken);
                return true;
            }
            catch(CompilerException e)
            {
                ReportError(e);
                return false;
            }

        }

        private SymbolBinding DefineOrGetVariable(string identifier)
        {
            SymbolBinding sb;
            if (!_ctx.TryGetVariable(identifier, out sb))
                sb = _ctx.DefineVariable(identifier);

            return sb;
        }

        
        #region Helper methods

        public void NextLexem()
        {
            if (_lastExtractedLexem.Token != Token.EndOfText)
            {
                _lastExtractedLexem = _lexer.NextLexem();
            }
            else
            {
                throw CompilerException.UnexpectedEndOfText();
            }
        }

        

        public static ConstDefinition CreateConstDefinition(ref Lexem lex)
        {
            ConstType constType = ConstType.Undefined;
            switch (lex.Type)
            {
                case LexemType.BooleanLiteral:
                    constType = ConstType.Boolean;
                    break;
                case LexemType.DateLiteral:
                    constType = ConstType.Date;
                    break;
                case LexemType.NumberLiteral:
                    constType = ConstType.Number;
                    break;
                case LexemType.StringLiteral:
                    constType = ConstType.String;
                    break;
            }

            ConstDefinition cDef = new ConstDefinition()
            {
                Type = constType,
                Presentation = lex.Content
            };
            return cDef;
        }

        private void ReportError(ScriptException compilerException)
        {
            _wasErrorsInBuild = true;
            ScriptException.AppendCodeInfo(compilerException, _lexer.GetIterator().GetPositionInfo());

            if (CompilerError != null)
            {
                var eventArgs = new CompilerErrorEventArgs();
                eventArgs.Exception = compilerException;
                eventArgs.LexerState = _lexer;
                CompilerError(this, eventArgs);

                if (!eventArgs.IsHandled)
                    throw compilerException;

                _builder.OnError(eventArgs);

            }
            else
            {
                throw compilerException;
            }
        }

        private bool CheckCorrectStatementEnd()
        {
            if (!(_lastExtractedLexem.Token == Token.Semicolon ||
                 _lastExtractedLexem.Token == Token.EndOfText))
            {
                ReportError(CompilerException.SemicolonExpected());
                return false;
            }

            return true;
        }

        private void SkipToNextStatement()
        {
            while (!(_lastExtractedLexem.Token == Token.EndOfText
                    || LanguageDef.IsBeginOfStatement(_lastExtractedLexem.Token)))
            {
                NextLexem();
            }
        }

        private void SetPosition(PositionInModule newPosition)
        {
            switch(newPosition)
            {
                case PositionInModule.VarSection:
                {
                    if(!(_parserPosition == PositionInModule.Begin ||
                        _parserPosition == PositionInModule.VarSection ||
                        _parserPosition == PositionInModule.MethodVarSection))

                        throw CompilerException.LateVarDefinition();

                    break;
                }
            }
            
            _parserPosition = newPosition;
        }

        Lexem ILexemExtractor.LastExtractedLexem
        {
            get { return _lastExtractedLexem; }
        }

        void ILexemExtractor.NextLexem()
        {
            this.NextLexem();
        }

        #endregion
   
    }
}
