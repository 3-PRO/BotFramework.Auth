using System;
using System.Collections.Generic;

namespace TRIPRO.BotFramework.Auth.AspNetCore.Models
{
    [Serializable]
    public class AuthResult : IAuthResult
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string UserName { get; set; }
        public string UserUniqueId { get; set; }
        public long ExpiresOnUtcTicks { get; set; }
        public byte[] TokenCache { get; set; }
        public string IdentityProvider { get; set; }
        public Dictionary<string, string> Claims { get; set; }
    }
}
