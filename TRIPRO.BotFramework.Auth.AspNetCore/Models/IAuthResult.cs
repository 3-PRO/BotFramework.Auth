using System.Collections.Generic;

namespace TRIPRO.BotFramework.Auth.AspNetCore.Models
{
    public interface IAuthResult
    {
        string AccessToken { get; set; }
        string RefreshToken { get; set; }
        string UserName { get; set; }
        string UserUniqueId { get; set; }
        long ExpiresOnUtcTicks { get; set; }
        byte[] TokenCache { get; set; }
        string IdentityProvider { get; set; }
        Dictionary<string, string> Claims { get; set; }
    }
}
