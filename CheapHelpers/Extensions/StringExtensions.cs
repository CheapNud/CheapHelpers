using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CheapHelpers.Extensions
{
    public static class StringExtensions
    {
        public static string Capitalize(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                ArgumentException.ThrowIfNullOrWhiteSpace($"'{nameof(str)}' cannot be null or whitespace.", nameof(str));
            }

            str = str.ToLower();
            return str.First().ToString().ToUpper() + string.Join("", str.Skip(1));
        }

        public static bool IsDigitsOnly(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                ArgumentException.ThrowIfNullOrWhiteSpace($"'{nameof(str)}' cannot be null or whitespace.", nameof(str));
            }

            return !str.Any(x => x < '0' || x > '9');
        }

        /// <summary>
        /// checks for j/n and converts to true false
        /// </summary>
        /// <param name="str"></param>
        /// <returns>true, false</returns>
        public static bool CheckBool(this string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                return false;
            }

            return str.Equals("j", StringComparison.CurrentCultureIgnoreCase);
        }

        public static string ToInternationalPhoneNumber(this string phonenumber)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(phonenumber);

            //regex?
            string output = phonenumber.Replace(@" ", string.Empty).Replace(@"\", string.Empty).Replace(@"/", string.Empty).Replace(@"-", string.Empty).Replace(@".", string.Empty);

            if (output.StartsWith('+'))
            {
                return output;
            }
            else if (output.StartsWith("00"))
            {
                return $@"+{output.Remove(0, 2)}";
            }
            else if (output.StartsWith("06"))
            {
                return $@"+31{output.Remove(0, 1)}";
            }
            else if (output.StartsWith('0'))
            {
                return $@"+32{output.Remove(0, 1)}";
            }

            Debug.WriteLine($@"cannot convert -- {output} -- to international phone number");
            return null;
        }

        public static string CharArrayToString(this IEnumerable<char> input)
        {
            return string.Concat(input);
        }

        public static string StringArrayToString(this IEnumerable<string> input)
        {
            return string.Concat(input);
        }

        public static string ToShortString(this string input, int chars = 20)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            if (input.Length <= chars)
            {
                return input;
            }

            return @$"{input[..chars]}...";
        }

        /// <summary>
        /// Trims the string to a specified maximum length and appends "..." if it exceeds that length.
        /// </summary>
        /// <param name="input">The string to trim</param>
        /// <param name="maxLength">The maximum text length (ellipsis will be added after this length)</param>
        /// <returns>Trimmed string with ellipsis if needed</returns>
        public static string TrimWithEllipsis(this string input, int maxLength)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= maxLength)
            {
                return input;
            }
            return string.Concat(input.AsSpan(0, maxLength), "...");
        }

        /// <summary>
        /// Removes all special characters from a string, keeping only alphanumeric characters.
        /// </summary>
        /// <param name="str">The string to process</param>
        /// <returns>String with only alphanumeric characters</returns>
        public static string RemoveSpecialCharacters(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            var sb = new System.Text.StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Removes special characters from a string, keeping alphanumeric characters and dashes.
        /// Consecutive dashes are collapsed to a single dash.
        /// </summary>
        /// <param name="str">The string to process</param>
        /// <returns>String with alphanumeric characters and single dashes</returns>
        public static string RemoveSpecialCharactersKeepDash(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            var sb = new System.Text.StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c == '-'))
                {
                    sb.Append(c);
                }
            }
            while (sb.ToString().Contains("--"))
            {
                sb = sb.Replace("--", "-");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Sanitizes a string for safe usage (spaces to underscores, slashes to dashes, keeps alphanumeric and common safe characters).
        /// </summary>
        /// <param name="str">The string to sanitize</param>
        /// <returns>Sanitized string safe for general use</returns>
        public static string Sanitize(this string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            var sb = new System.Text.StringBuilder(str.Length);

            foreach (char c in str)
            {
                switch (c)
                {
                    case ' ':
                        sb.Append('_');
                        break;
                    case '/':
                        sb.Append('-');
                        break;
                    default:
                        if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.')
                            sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
