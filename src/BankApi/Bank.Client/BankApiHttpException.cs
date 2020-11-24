using System;
using System.Net.Http;
using System.Text;

namespace Bank.Client
{
    public class BankApiHttpException : HttpRequestException
    {
        public readonly HttpRequestMessage Request;
        public readonly HttpResponseMessage Response;

        public BankApiHttpException(string message, Exception exception) : base(message, exception)
        {
        }

        public BankApiHttpException(string message) : base(message)
        {
        }

        public BankApiHttpException(HttpRequestMessage request, HttpResponseMessage response) : base(
            BuildMessage(request, response))
        {
            Request = request;
            Response = response;
        }

        public BankApiHttpException(HttpRequestMessage request, HttpResponseMessage response,
            Exception exception) : base(BuildMessage(request, response), exception)
        {
            Request = request;
            Response = response;
        }

        private static string BuildMessage(HttpRequestMessage request, HttpResponseMessage response)
        {
            var builder = new StringBuilder();

            if (response != null)
            {
                builder.AppendLine(
                    $"Response status code does not indicate success: {(int) response.StatusCode} ({response.ReasonPhrase}) when sending '{request.Method} {request.RequestUri}'");

                var responseBody = response.Content?.ReadAsStringAsync()?.Result;
                if (!string.IsNullOrEmpty(responseBody))
                {
                    builder.AppendLine("Response received: ");
                    builder.AppendLine(responseBody);
                }
            }
            else
            {
                builder.AppendLine(
                    $"Request exception when sending {request.Method} {request.RequestUri}");
            }

            return builder.ToString();
        }
    }
}