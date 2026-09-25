using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xrm.Sdk;

namespace EduCore.Plugins
{
    /// <summary>
    /// Builds the value of the generated <c>edu_uniquekey</c> column that backs each table's alternate key.
    /// Parts are joined with '|'. Callers (plug-ins and integrations) must use this exact format so that
    /// upserts by alternate key match the key the plug-in would generate.
    /// </summary>
    public static class EduKey
    {
        public const char Separator = '|';

        /// <summary>Marks an optional key part that is legitimately empty, so it never collides with a real value.</summary>
        public static object Or(object value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(fallback)) throw new ArgumentException("A fallback token is required.", "fallback");
            var text = value as string;
            return (value == null || (text != null && text.Trim().Length == 0)) ? fallback : value;
        }

        public static string Build(params object[] parts)
        {
            if (parts == null || parts.Length == 0) throw new ArgumentException("At least one key part is required.", "parts");
            var tokens = new List<string>(parts.Length);
            for (int i = 0; i < parts.Length; i++) tokens.Add(Token(parts[i], i));
            return string.Join(Separator.ToString(), tokens);
        }

        private static string Token(object part, int index)
        {
            if (part == null) throw new ArgumentException("Key part " + index + " is null. Use EduKey.Or for an optional part.");
            if (part is Guid) return ((Guid)part).ToString("D");
            var reference = part as EntityReference;
            if (reference != null) return reference.Id.ToString("D");
            var option = part as OptionSetValue;
            if (option != null) return option.Value.ToString(CultureInfo.InvariantCulture);
            if (part is DateTime) return ((DateTime)part).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            if (part is int) return ((int)part).ToString(CultureInfo.InvariantCulture);
            var text = part as string;
            if (text != null)
            {
                text = text.Trim().ToLowerInvariant();
                if (text.Length == 0) throw new ArgumentException("Key part " + index + " is empty. Use EduKey.Or for an optional part.");
                if (text.IndexOf(Separator) >= 0) throw new ArgumentException("Key part " + index + " contains the separator '" + Separator + "'.");
                return text;
            }
            throw new ArgumentException("Key part " + index + " has unsupported type " + part.GetType().Name + ".");
        }
    }
}