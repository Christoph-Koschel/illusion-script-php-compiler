using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using IllusionScript.Runtime.Binding;
using IllusionScript.Runtime.Binding.Nodes.Expressions;
using IllusionScript.Runtime.Binding.Nodes.Statements;
using IllusionScript.Runtime.Binding.Operators;
using IllusionScript.Runtime.Compiling;
using IllusionScript.Runtime.Interpreting.Memory.Symbols;

namespace IllusionScript.Compiler.PHP8
{
    public sealed class CompilerFile : CompilerWriter
    {
        private readonly string name;

        public CompilerFile(FileStream writer) : base(writer)
        {
            name = Path.GetFileName(writer.Name);
        }

        public void Close()
        {
            writer.Close();
        }

        public void WriteHeader(Dictionary<string, List<FunctionSymbol>>.KeyCollection keyCollection)
        {
            writer.Write("<?php\n");

            writer.Write($"include_once \"./syscall.php\";\n");

            foreach (string s in keyCollection)
            {
                string file = Path.GetFileName(s);
                if (file == name)
                {
                    continue;
                }

                writer.Write($"include_once \"./{file}\";\n");
            }
        }

        public void Write(FunctionSymbol function,
            ImmutableDictionary<FunctionSymbol, BoundBlockStatement> functionBodies, bool isEntryPoint)
        {
            if (isEntryPoint)
            {
                writer.Write("(");
            }

            WriteFunctionHead(function);
            writer.Write("\n{\n");
            WriteStatement(functionBodies[function]);

            writer.Write(isEntryPoint ? "})();\n" : "}\n");
        }

        protected override void WriteBlockStatement(BoundBlockStatement body)
        {
            foreach (BoundStatement statement in body.statements)
            {
                writer.Write("    ");
                WriteStatement(statement);
            }
        }

        protected override void WriteConditionalGotoStatement(BoundConditionalGotoStatement statement)
        {
            writer.Write("if ((");
            WriteExpression(statement.condition);
            writer.Write(statement.jmpIfTrue ? ")==true) " : ")==false) ");
            writer.Write("goto ");
            writer.Write(statement.boundLabel.name);
        }

        protected override void WriteLabelStatement(BoundLabelStatement statement)
        {
            writer.Write("\n");
            writer.Write(statement.BoundLabel.name);
            writer.Write(":");
        }

        protected override void WriteGotoStatement(BoundGotoStatement statement)
        {
            writer.Write("goto ");
            writer.Write(statement.BoundLabel.name);
        }

        protected override void WriteReturnStatement(BoundReturnStatement statement)
        {
            writer.Write("return");
            if (statement.expression != null)
            {
                writer.Write(" ");
                WriteExpression(statement.expression);
            }
        }

        protected override void WriteExpressionStatement(BoundExpressionStatement statement)
        {
            WriteExpression(statement.expression);
        }

        protected override void WriteVariableDeclarationStatement(BoundVariableDeclarationStatement statement)
        {
            writer.Write("$");
            writer.Write(statement.variable.name);
            writer.Write(" = ");

            WriteExpression(statement.initializer);
        }


        protected override void WriteBinaryExpression(BoundBinaryExpression expression)
        {
            WriteExpression(expression.left);
            writer.Write(BoundBinaryOperator.GetText(expression.binaryOperator.operatorType));
            WriteExpression(expression.right);
        }

        protected override void WriteUnaryExpression(BoundUnaryExpression expression)
        {
            writer.Write(BoundUnaryOperator.GetText(expression.unaryOperator.operatorType));
            WriteExpression(expression.right);
        }

        protected override void WriteAssignmentExpression(BoundAssignmentExpression expression)
        {
            writer.Write("$");
            writer.Write(expression.variableSymbol.name);
            writer.Write(" = ");
            WriteExpression(expression.expression);
        }

        protected override void WriteLiteralExpression(BoundLiteralExpression expression)
        {
            if (expression.type == TypeSymbol.Bool)
            {
                writer.Write((bool)expression.value ? "true" : "false");
            }
            else if (expression.type == TypeSymbol.Int)
            {
                writer.Write(expression.value);
            }
            else if (expression.type == TypeSymbol.String)
            {
                writer.Write("\"");
                writer.Write(((string)expression.value).Replace("\"", "\\\""));
                writer.Write("\"");
            }
            else
            {
                throw new Exception($"Undefined type {expression.type}");
            }
        }

        protected override void WriteVariableExpression(BoundVariableExpression expression)
        {
            writer.Write("$");
            writer.Write(expression.variableSymbol.name);
        }

        protected override void WriteCallExpression(BoundCallExpression expression)
        {
            writer.Write(expression.function.name);
            writer.Write("(");

            bool first = true;
            foreach (BoundExpression argument in expression.arguments)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(", ");
                }

                WriteExpression(argument);
            }

            writer.Write(")");
        }

        protected override void WriteConversionExpression(BoundConversionExpression expression)
        {
            writer.Write("(");
            writer.Write(expression.type.name.ToLower());
            writer.Write(")");
            WriteExpression(expression.expression);
        }

        private void WriteFunctionHead(FunctionSymbol function)
        {
            writer.Write("function ");
            writer.Write(function.name);
            writer.Write("(");

            bool first = true;

            foreach (ParameterSymbol parameter in function.parameters)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    writer.Write(", ");
                }

                writer.Write("$");
                writer.Write(parameter.name);
            }

            writer.Write(")");
        }
    }
}