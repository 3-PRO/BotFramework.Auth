﻿using TRIPRO.BotFramework.Auth.AspNetCore.Constants;
using TRIPRO.BotFramework.Auth.AspNetCore.Models;
using TRIPRO.BotFramework.Auth.AspNetCore.Providers;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TRIPRO.BotFramework.Auth.AspNetCore.Extensions
{
    public static class Extensions
    {
        public static void StoreAuthResult(this IBotContext context, IAuthResult authResult, IAuthProvider authProvider)
        {
            context.UserData.SetValue($"{authProvider.Name}{ContextConstants.AuthResultKey}", authResult);
        }

        public static async Task<JObject> GetWithAuthAsync(this HttpClient client, string accessToken, string endpoint)
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            using (HttpResponseMessage response = await client.GetAsync(endpoint))
            {
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(json);
                }
                else
                    return null;
            }
        }

        public static async Task<byte[]> GetStreamWithAuthAsync(this HttpClient client, string accessToken, string endpoint)
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            using (HttpResponseMessage response = await client.GetAsync(endpoint))
            {
                if (response.IsSuccessStatusCode)
                {
                    System.IO.Stream stream = await response.Content.ReadAsStreamAsync();
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, (int)stream.Length);
                    return bytes;
                }
                else
                    return null;
            }
        }

        public static async Task<JObject> PostWithAuthAsync<T>(this HttpClient client, string accessToken, string endpoint, T data)
        {
            string json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            using (HttpResponseMessage response = await client.PostAsync(endpoint, content))
            {
                if (response.IsSuccessStatusCode)
                {
                    string resp = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(resp);
                }
                else
                    return null;
            }
        }

        public static async Task<JObject> DeleteWithAuthAsync<T>(this HttpClient client, string accessToken, string endpoint, T data)
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            using (HttpResponseMessage response = await client.DeleteAsync(endpoint))
            {
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(json);
                }
                else
                    return null;
            }
        }

        public static async Task<JObject> PatchWithAuthAsync<T>(this HttpClient client, string accessToken, string endpoint, T data)
        {
            string json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), endpoint)
            {
                Content = content
            };
            using (HttpResponseMessage response = await client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    string resp = await response.Content.ReadAsStringAsync();
                    return JObject.Parse(resp);
                }
                else
                    return null;
            }
        }
    }
}
