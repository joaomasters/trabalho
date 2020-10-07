﻿using System.Globalization;
using System.Linq;

namespace StackExchange.Metrics
{
    /// <summary>
    /// A delegate signature for transforming a metric name or tag.
    /// </summary>
    public delegate string NameTransformerDelegate(string name);

    /// <summary>
    /// A delegate signature for modifying tag values. See <see cref="MetricSourceOptions.TagValueTransformer"/>.
    /// </summary>
    public delegate string TagValueTransformerDelegate(string tagName, object tagValue);

    /// <summary>
    /// Provides a set of commonly useful metric name and tag name/value converters.
    /// </summary>
    public static class NameTransformers
    {
        // http://stackoverflow.com/questions/18781027/regex-camel-case-to-underscore-ignore-first-occurrence
        /// <summary>
        /// Converts CamelCaseNames to Snake_Case_Names.
        /// </summary>
        public static string CamelToSnakeCase(string s) => string.Concat(s.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString(CultureInfo.InvariantCulture)));

        /// <summary>
        /// Converts CamelCaseNames to snake_case_names with all lowercase letters.
        /// </summary>
        public static string CamelToLowerSnakeCase(string s) =>
            string.Concat(s.Select((c, i) =>
            {
                if (char.IsUpper(c))
                    return i == 0 ? char.ToLowerInvariant(c).ToString(CultureInfo.InvariantCulture) : "_" + char.ToLowerInvariant(c);

                return c.ToString(CultureInfo.InvariantCulture);
            }));

        /// <summary>
        /// Sanitizes a metric name or tag name/value by replacing illegal characters with an underscore.
        /// </summary>
        public static string Sanitize(string s) =>
            MetricValidation.InvalidChars.Replace(s, m =>
            {
                if (m.Index == 0 || m.Index + m.Length == s.Length) // beginning and end of string
                    return "";

                return "_";
            });

        /// <summary>
        /// Combines two name transformers.
        /// </summary>
        public static NameTransformerDelegate Combine(this NameTransformerDelegate first, NameTransformerDelegate second) => v => first(second(v));
    }
}
