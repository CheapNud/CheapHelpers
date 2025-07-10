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


    }
}
