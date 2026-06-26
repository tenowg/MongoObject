using System.Text;

namespace MongoObject.CliTool.Helpers
{
    public class IndentedStringBuilder
    {
        private readonly StringBuilder _sb = new();
        private int _indentLevel = 1;
        private const string IndentString = "    "; // 4 spaces

        public void Append(string text)
        {
            _sb.Append(text);
        }

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
        public IndentScope Block(string opener = "{", string closer = "", int indentLevel = 0)
        {
            AppendLine(opener);
            _indentLevel += indentLevel;
            return new IndentScope(this, closer, _indentLevel);
        }

        public override string ToString() => _sb.ToString();

        // High-performance struct to handle the closing brace and dedent
        public readonly struct IndentScope : IDisposable
        {
            private readonly IndentedStringBuilder _builder;
            private readonly string _closer = "";
            private readonly int _indentLevel = 0;

            public IndentScope(IndentedStringBuilder builder, string closer = "", int indentLevel = 0)
            {
                _builder = builder;
                _closer = closer;
                _indentLevel = indentLevel;
            }

            public void Dispose()
            {
                if (_builder != null)
                {
                    _builder._indentLevel -= _indentLevel;
                    _builder.AppendLine($"}}{_closer}");
                }
            }
        }
    }
}