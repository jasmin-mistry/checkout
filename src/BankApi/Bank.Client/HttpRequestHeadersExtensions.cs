using System.Net.Http.Headers;

namespace Bank.Client
{
    internal static class HttpRequestHeadersExtensions
    {
        public static void Set(this HttpRequestHeaders headers, string name, string value)
        {
            if (headers.Contains(name)) headers.Remove(name);
            headers.Add(name, value);
        }

        public static void SetWithoutValidation(this HttpRequestHeaders headers, string name, string value)
        {
            if (headers.Contains(name)) headers.Remove(name);
            headers.TryAddWithoutValidation(name, value);
        }
    }
}