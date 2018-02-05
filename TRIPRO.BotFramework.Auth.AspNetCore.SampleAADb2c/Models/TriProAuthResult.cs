using System;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;

namespace TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Models
{
    [Serializable]
    public class TriProAuthResult : AuthResult
    {
        public string Email { get; set; }
        public string CustomB2CProperty { get; set; }
    }
}
