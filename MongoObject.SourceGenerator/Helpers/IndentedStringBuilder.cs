using System;
using System.Text;

namespace MongoObject.SourceGenerator.Helpers
{
    public class IndentedStringBuilder
    {
        private readonly StringBuilder _sb = new();
        private int _indentLevel = 0;
        private const string IndentString = "    "; // 4 spaces

        public void AppendLine(string text)
        {
            // Only apply indentation if the line isn't empty
            if (!string.IsNullOrWhiteSpace(text))
            {
                for (int i = 0; i < _indentLevel; i++)
                {
                    _sb.Append(IndentString);
                }
            }
            _sb.AppendLine(text);
        }

        public void AppendLine() => _sb.AppendLine();

        // Opens a block, writes the opening character, and bumps indentation
        public IndentScope Block(string opener = "{")
        {
            AppendLine(opener);
            _indentLevel++;
            return new IndentScope(this);
        }

        public override string ToString() => _sb.ToString();

        // High-performance struct to handle the closing brace and dedent
        public readonly struct IndentScope : IDisposable
        {
            private readonly IndentedStringBuilder _builder;

            public IndentScope(IndentedStringBuilder builder)
            {
                _builder = builder;
            }

            public void Dispose()
            {
                if (_builder != null)
                {
                    _builder._indentLevel--;
                    _builder.AppendLine("}");
                }
            }
        }
    }
}