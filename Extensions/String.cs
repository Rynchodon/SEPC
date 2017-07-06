using System.Globalization;

namespace SEPC.Extensions
{
    public static class StringExtensions
    {
        public static bool LooseContains(this string self, string other)
        {
            // https://stackoverflow.com/questions/444798/case-insensitive-containsstring
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(self, other, CompareOptions.IgnoreCase) >= 0;
        }
        
    }
}
