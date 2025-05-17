using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace CheapHelpers
{
    //Use networkcredential securestring implementation
    public class Secret
    {
        /// <summary>
        /// Safe in-memory storage for strings (SecureString is not Secure, difference is an entropy)
        /// </summary>
        /// <param name="stringToHide"></param>
        public Secret(string stringToHide)
        {
            ProtectedValue = Protect(stringToHide);
        }

        /// <summary>
        /// Gets the encrypted string
        /// </summary>
        public string ProtectedValue { get; }
        /// <summary>
        /// gets the unprotected plain string password. Be carefull with this, dispose the plain string the moment you are done.
        /// </summary>
        /// <returns></returns>
        public string GetPlainString()
        {
            return Unprotect(ProtectedValue);
        }
        /// <summary>
        /// Encrypts a string and stores it in memory
        /// Xamarin Cannot use this function. Throws operation not supported exception.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string Protect(string str)
        {
            byte[] entropy = Encoding.ASCII.GetBytes(Assembly.GetExecutingAssembly().FullName);
            byte[] data = Encoding.ASCII.GetBytes(str);
            string protectedData = Convert.ToBase64String(ProtectedData.Protect(data, entropy, DataProtectionScope.CurrentUser));
            return protectedData;
        }
        /// <summary>
        /// gets the plain string from the encrypted string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string Unprotect(string str)
        {
            byte[] protectedData = Convert.FromBase64String(str);
            byte[] entropy = Encoding.ASCII.GetBytes(Assembly.GetExecutingAssembly().FullName);
            string data = Encoding.ASCII.GetString(ProtectedData.Unprotect(protectedData, entropy, DataProtectionScope.CurrentUser));
            return data;
        }
    }
}
