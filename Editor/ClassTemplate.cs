using Ambratolm.ScriptGenerator.Extensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ambratolm.ScriptGenerator
{
    /// <summary>
    /// Represents a class template that can be used to generate C# code.
    /// </summary>
    public class ClassTemplate
    {
        private string _code;

        //----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets or sets the mark that indicates the start of the class template code in input text.
        /// </summary>
        public static string CodeStartMark { get; set; } = "#";

        /// <summary>
        /// Gets or sets the mark that indicates the end of the class template code in input text.
        /// </summary>
        public static string CodeEndMark { get; set; } = "#end";

        /// <summary>
        /// Gets the dictionary that maps tokens to their values.
        /// <para>Tokens represent placeholders inside the class template code.</para>
        /// <para>
        /// Token keys found in the class template code will be replaced by their corresponding
        /// token values in the generated class.
        /// </para>
        /// </summary>
        public Dictionary<string, StringBuilder> Tokens { get; } = new();

        /// <summary>
        /// Gets a flag that indicates whether the class template has any code or not.
        /// </summary>
        public bool HasCode { get; private set; }

        /// <summary>
        /// Gets a flag that indicates whether the class template has a class defined or not.
        /// </summary>
        public bool HasClass { get; private set; }

        /// <summary>
        /// Gets a flag that indicates whether the class template has a namespace defined or not.
        /// </summary>
        public bool HasNamespace { get; private set; }

        /// <summary>
        /// Gets the name of the class to be generated using the class template.
        /// <para>Extracted from the parsed class template code.</para>
        /// </summary>
        public string ClassName { get; private set; }

        /// <summary>
        /// Gets the name of the namespace that contains the class to be generated using the class template.
        /// <para>Extracted from the parsed class template code.</para>
        /// </summary>
        public string NamespaceName { get; private set; }

        /// <summary>
        /// Gets or sets the template class code to be used to generate the class code.
        /// <para>
        /// This property accepts any input string, and will try to parse it and extract the
        /// template class code from it by looking for the start and end marks. It will also extract
        /// the class name and the namespace name.
        /// </para>
        /// <para>
        /// The class code (output code) is generated by replacing the tokens contained in this
        /// template class code (input code) by their corresponding values that are mapped in the
        /// tokens dictionary.
        /// </para>
        /// </summary>
        public string Code
        {
            get => _code;
            set
            {
                value = ExtractClassTemplateCode(value);
                HasCode = !string.IsNullOrWhiteSpace(value);
                if (!HasCode) return;
                _code = value;
                ClassName = ExtractClassName(_code);
                NamespaceName = ExtractNamespaceName(_code);
                HasClass = !string.IsNullOrWhiteSpace(ClassName);
                HasNamespace = !string.IsNullOrWhiteSpace(NamespaceName); ;
            }
        }

        //----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="text">The text input to be parsed for extracting the class template code.</param>
        public ClassTemplate(string text) => Code = text;

        /// <summary>
        /// Appends a text to a token value in the tokens dictionary.
        /// <para>A new token entry is added if the specified key doesn't exist.</para>
        /// </summary>
        /// <param name="tokenKey">The key of the token to append to.</param>
        /// <param name="text">The line of text to append.</param>
        public void Append(string tokenKey, string text)
        {
            if (Tokens.ContainsKey(tokenKey)) Tokens[tokenKey].Append(text);
            else Tokens.Add(tokenKey, new StringBuilder(text));
        }

        /// <summary>
        /// Appends a line of text to a token value in the tokens dictionary.
        /// <para>A new token entry is added if the specified key doesn't exist.</para>
        /// </summary>
        /// <param name="tokenKey">The key of the token to append to.</param>
        /// <param name="line">The line of text to append.</param>
        public void AppendLine(string tokenKey, string line) => Append(tokenKey, $"{line}{Environment.NewLine}");

        /// <summary>
        /// Generates the class code by replacing the tokens with their values in the class template code.
        /// </summary>
        /// <returns>The generated class code.</returns>
        public string GenerateClassCode()
        {
            string[] codeLines = Code.Split(Environment.NewLine);
            for (int i = 0; i < codeLines.Length; i++)
            {
                foreach (KeyValuePair<string, StringBuilder> token in Tokens)
                {
                    int paddingWidth = codeLines[i].IndexOf(token.Key);
                    if (paddingWidth < 0) continue;
                    string padding = string.Empty.PadRight(paddingWidth);
                    string[] tokenValueLines = token.Value.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < tokenValueLines.Length; j++)
                        tokenValueLines[j] = $"{padding}{tokenValueLines[j]}";
                    codeLines[i] = string.Join(Environment.NewLine, tokenValueLines);
                }
            }
            return string.Join(Environment.NewLine, codeLines);
        }

        //----------------------------------------------------------------------------------------------------

        /// <summary>
        /// Extracts the class template code from a string that contains it between the start and
        /// end marks.
        /// </summary>
        /// <param name="text">The string that contains a class template code.</param>
        /// <returns>The extracted class template code.</returns>
        public static string ExtractClassTemplateCode(string text)
        {
            if (!Validate(text, out Exception exception)) throw exception;
            string[] inputCodeLines = text.Split(Environment.NewLine);
            StringBuilder outputCodeBuilder = new();
            bool codeStartMarkFound = false;
            foreach (string line in inputCodeLines)
            {
                if (codeStartMarkFound)
                {
                    if (line.StartsWith(CodeEndMark)) break;
                    else outputCodeBuilder.AppendLine(line);
                }
                else if (line.StartsWith(CodeStartMark))
                {
                    codeStartMarkFound = true;
                    continue;
                }
            }
            return codeStartMarkFound ? outputCodeBuilder.ToString() : string.Empty;
        }

        /// <summary>
        /// Extracts the name of the namespace defined in the given code.
        /// </summary>
        /// <param name="code">The code to be parsed.</param>
        /// <returns>The string of the namespace name or an empty string if no namespace is found.</returns>
        public static string ExtractNamespaceName(string code) => code.SubstringBetween("namespace ", "{").Trim();

        /// <summary>
        /// Extracts the name of the class defined in the given code.
        /// </summary>
        /// <param name="code">The code to be parsed.</param>
        /// <returns>The string of the class name or an empty string if no class is found.</returns>
        public static string ExtractClassName(string code)
            => code.SubstringBetween("class ", "{").Trim().SubstringBetween("", ":").Trim();

        /// <summary>
        /// Validates the input text for code generation.
        /// </summary>
        /// <param name="text">The input text containing the class template code.</param>
        /// <param name="exception">
        /// An output exception that can be thrown when the validation fails.
        /// </param>
        /// <returns>True if the validation succeeds, false otherwise.</returns>
        public static bool Validate(string text, out Exception exception)
        {
            exception = null;
            if (string.IsNullOrEmpty(text))
                exception = new ArgumentNullException("Empty input");
            else if (!text.Contains(CodeStartMark))
                exception = new FormatException($"Invalid input. No code start mark found. " +
                    $"Input should contain class template code put between two lines: " +
                    $"Start line (Prefixed with start mark \"{CodeStartMark}\") and " +
                    $"End line (Prefixed with end mark \"{CodeEndMark}\"). ");
            else
                return true;
            return false;
        }

        /// <summary>
        /// Validates the input text for code generation.
        /// </summary>
        /// <param name="text">The input text containing the class template code.</param>
        /// <returns>True if the validation succeeds, false otherwise.</returns>
        public static bool Validate(string text) => Validate(text, out _);
    }
}