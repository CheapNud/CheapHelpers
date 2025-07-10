using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace CheapHelpers.Extensions
{
    public static class AuthExtensions
    {
        public static string ToAuthorizationCredentials(string usr, string pwd)
        {
            return $@"Basic {Convert.ToBase64String(Encoding.ASCII.GetBytes($@"{usr}:{pwd}"))}";
        }
        public static (string username, string password) GetAuthorizationCredentials(this string encodedstring)
        {
            var token = encodedstring.Substring("Basic ".Length).Trim();
            var credentialstring = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var credentials = credentialstring.Split(':');
            return (credentials[0], credentials[1]);
        }
        public static SecureString ToSecureString(this string input)
        {
            SecureString output = new SecureString();
            int l = input.Length;
            char[] s = input.ToCharArray(0, l);
            for (int i = 0; i < s.Length; i++)
            {
                output.AppendChar(s[i]);
            }
            return output;
        }
        public static string ToString(this SecureString ss)
        {
            nint unmanagedString = nint.Zero;
            try
            {
                unmanagedString = Marshal.SecureStringToGlobalAllocUnicode(ss);
                return Marshal.PtrToStringUni(unmanagedString);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(unmanagedString);
            }
        }
    }
}
