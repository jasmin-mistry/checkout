using System.Text.RegularExpressions;

namespace SharedKernel
{
    public static class StringExtensions
    {
        public static string MaskCardNumber(this string cardNumber, char withLetter = '*')
        {
            var reg = new Regex(@"(?<=\d{4})\d{4}\d{4}(?=\d{4})|(?<=\d{4}( |-))\d{4}\1\d{4}(?=\1\d{4})");
            return reg.Replace(cardNumber, m => new string(withLetter, m.Length));
        }
    }
}