using System.Collections.Generic;
using System.Threading.Tasks;
using Refit;

namespace Bank.Client.Auth
{
    public interface IAuthClient
    {
        [Post("/login/v1/oauth2/token")]
        Task<ApiResponse<Token>> GetToken([Body(BodySerializationMethod.UrlEncoded)]
            Dictionary<string, object> data);
    }
}