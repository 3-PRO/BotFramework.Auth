using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Models;

namespace TRIPRO.BotFramework.Auth.AspNetCore.SampleAADb2c.Extensions
{
    public static class JObjectExtension
    {
        //TriProAuthResult
        public static TriProAuthResult ToAuthResult(this JObject json)
        {
            string idTokenInfo = json.Value<string>("id_token").Split('.')[1];
            idTokenInfo = Base64UrlEncoder.Decode(idTokenInfo);
            JObject idTokenJson = JObject.Parse(idTokenInfo);

            var result = new TriProAuthResult
            {
                AccessToken = json.Value<string>("access_token"),
                UserName = idTokenJson.Value<string>("name"),
                UserUniqueId = idTokenJson.Value<string>("oid"),
                ExpiresOnUtcTicks = DateTime.UtcNow.AddSeconds(3600).Ticks, //HACK???
                RefreshToken = json.Value<string>("refresh_token"),
                IdentityProvider = idTokenJson.Value<string>("idp"),
                Email = idTokenJson.Value<string>("email"),
                CustomB2CProperty = idTokenJson.Value<string>("extension_customProperty"), // this must be configured in your custom policy on your b2c
                Claims = new Dictionary<string, string>()
            };
            //converting keyvaluepairs of Jtokens to claims (strings)
            foreach (KeyValuePair<string, JToken> item in idTokenJson) { result.Claims.Add(item.Key, (string)item.Value); }
            return result;
        }
    }
}
