using Google.Cloud.AIPlatform.V1;
using Google.Protobuf;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace DomusMercatorisDotnetMVC.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;

        public GeminiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string?> GenerateProductDescription(string apiKey, List<IFormFile> images)
        {
            if (string.IsNullOrEmpty(apiKey) || images == null || images.Count == 0)
            {
                return null;
            }

            var parts = new List<object>();
            parts.Add(new { text = "Bu resimdeki ürün için bir isim ve açıklama oluştur. Çıktı şu JSON formatında olsun: { \"name\": \"Ürün Adı\", \"description\": \"Ürün Açıklaması\" }. Açıklama detaylı ve satış odaklı olsun." });

            foreach (var image in images)
            {
                using var ms = new MemoryStream();
                await image.CopyToAsync(ms);
                var base64 = Convert.ToBase64String(ms.ToArray());
                parts.Add(new 
                { 
                    inline_data = new 
                    { 
                        mime_type = image.ContentType, 
                        data = base64 
                    } 
                });
            }

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = parts
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-lite:generateContent?key={apiKey}";

            try
            {
                var response = await _httpClient.PostAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini API Error: {error}");
                    return null;
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                dynamic? result = JsonConvert.DeserializeObject(resultJson);
                
                if (result == null || result!.candidates == null || result!.candidates.Count == 0)
                {
                    return null;
                }

                string text = result!.candidates[0].content.parts[0].text;
                
                // JSON bloğunu temizle (```json ... ``` gibi markdown işaretlerini kaldır)
                text = text.Replace("```json", "").Replace("```", "").Trim();
                
                return text;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini Exception: {ex.Message}");
                return null;
            }
        }
    }
}
