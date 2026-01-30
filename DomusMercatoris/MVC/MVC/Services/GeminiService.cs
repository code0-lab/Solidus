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
using Ganss.Xss;
using DomusMercatoris.Data;
using DomusMercatoris.Service.Services;
using Microsoft.EntityFrameworkCore;
using DomusMercatorisDotnetMVC.Dto.UserDto;
using DomusMercatoris.Core.Entities;

namespace DomusMercatorisDotnetMVC.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly HtmlSanitizer _sanitizer;
        private readonly DomusDbContext _dbContext;
        private readonly EncryptionService _encryptionService;

        public GeminiService(HttpClient httpClient, DomusDbContext dbContext, EncryptionService encryptionService)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _encryptionService = encryptionService;
            _sanitizer = new HtmlSanitizer();
            _sanitizer.AllowedAttributes.Add("style");
            _sanitizer.AllowedAttributes.Add("class");
            _sanitizer.AllowedAttributes.Add("id");
            _sanitizer.AllowedCssProperties.Add("display");
            _sanitizer.AllowedCssProperties.Add("flex-direction");
            _sanitizer.AllowedCssProperties.Add("justify-content");
            _sanitizer.AllowedCssProperties.Add("align-items");
            _sanitizer.AllowedCssProperties.Add("gap");
            _sanitizer.AllowedCssProperties.Add("width");
            _sanitizer.AllowedCssProperties.Add("height");
            _sanitizer.AllowedCssProperties.Add("background");
            _sanitizer.AllowedCssProperties.Add("color");
            _sanitizer.AllowedCssProperties.Add("font-size");
            _sanitizer.AllowedCssProperties.Add("font-weight");
            _sanitizer.AllowedCssProperties.Add("border");
            _sanitizer.AllowedCssProperties.Add("border-radius");
            _sanitizer.AllowedCssProperties.Add("padding");
            _sanitizer.AllowedCssProperties.Add("margin");
            _sanitizer.AllowedCssProperties.Add("text-align");
            _sanitizer.AllowedCssProperties.Add("text-decoration");
            _sanitizer.AllowedCssProperties.Add("box-shadow");
            _sanitizer.AllowedCssProperties.Add("transform");
            _sanitizer.AllowedCssProperties.Add("cursor");
            _sanitizer.AllowedCssProperties.Add("z-index");
            _sanitizer.AllowedCssProperties.Add("position");
            _sanitizer.AllowedCssProperties.Add("top");
            _sanitizer.AllowedCssProperties.Add("left");
            _sanitizer.AllowedCssProperties.Add("right");
            _sanitizer.AllowedCssProperties.Add("bottom");
            _sanitizer.AllowedCssProperties.Add("overflow");
            _sanitizer.AllowedCssProperties.Add("background-color");
            _sanitizer.AllowedCssProperties.Add("background-image");
            _sanitizer.AllowedCssProperties.Add("background-size");
            _sanitizer.AllowedCssProperties.Add("background-position");
            _sanitizer.AllowedCssProperties.Add("background-repeat");
            _sanitizer.AllowedCssProperties.Add("linear-gradient");

            // URL Filtreleme (Whitelist: Sadece kendi domainimiz ve picsum.photos)
            _sanitizer.FilterUrl += (sender, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.OriginalUrl)) return;

                // Relative URL'lere izin ver (Kendi domainimiz)
                if (e.OriginalUrl.StartsWith("/") || e.OriginalUrl.StartsWith("#"))
                {
                    return; 
                }

                // Absolute URL kontrolü
                if (Uri.TryCreate(e.OriginalUrl, UriKind.Absolute, out var uri))
                {
                    // Whitelist domains
                    var allowedDomains = new[] { "picsum.photos", "fastly.picsum.photos", "via.placeholder.com" };
                    
                    if (allowedDomains.Any(d => uri.Host.Equals(d, StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith("." + d, StringComparison.OrdinalIgnoreCase)))
                    {
                        return;
                    }
                }

                // Güvenli değilse URL'i kaldır
                e.SanitizedUrl = null; 
            };
        }

        public string SanitizeHtml(string html)
        {
            return _sanitizer.Sanitize(html);
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

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={apiKey}";

            var response = await SendRequestWithRetryAsync(url, content);
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

        public static string WrapInRetroTemplate(string jsonContent)
        {
            return RetroSliderTemplate.Replace("{{GEMINI_DATA_PLACEHOLDER}}", jsonContent);
        }

        public static string WrapRawHtmlInRetroTemplate(string rawHtml)
        {
            return $@"<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Retro Banner</title>
    <style>
{RetroCss}
    </style>
</head>
<body>
{rawHtml}
</body>
</html>";
        }

        private const string RetroCss = @"
        /* --- RESET & VARIABLES --- */
        :root {
            --bg-color: #e0e0e0;
            --window-bg: #ffffff;
            --main-black: #000000;
            --shadow-hard: 8px 8px 0px rgba(0, 0, 0, 0.3);
            --font-mono: 'Courier New', 'Monaco', monospace;
            --accent-glitch: #ff00ff;
        }

        * {
            box-sizing: border-box;
            margin: 0;
            padding: 0;
        }

        body {
            background-color: var(--bg-color);
            font-family: var(--font-mono);
            display: flex;
            justify-content: center;
            align-items: center;
            min-height: 100vh;
            overflow: hidden;
        }

        /* --- THE RETRO WINDOW CONTAINER --- */
        .os-window {
            width: 90%;
            max-width: 900px;
            background: var(--window-bg);
            border: 3px solid var(--main-black);
            box-shadow: var(--shadow-hard);
            position: relative;
            display: flex;
            flex-direction: column;
        }

        /* --- TITLE BAR --- */
        .title-bar {
            background: var(--main-black);
            color: var(--window-bg);
            padding: 8px 12px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 3px solid var(--main-black);
            user-select: none;
        }

        .title-text {
            font-weight: bold;
            letter-spacing: 1px;
            text-transform: uppercase;
        }

        .window-controls {
            display: flex;
            gap: 6px;
        }

        .control-dot {
            width: 12px;
            height: 12px;
            background: var(--window-bg);
            border: 1px solid var(--main-black);
        }

        /* --- VIEWPORT (MASK) --- */
        .viewport {
            position: relative;
            width: 100%;
            height: 320px; /* Desktop Default */
            overflow: hidden;
            background-color: #f4f4f4;
        }

        /* Scanline Overlay Effect */
        .scanlines {
            position: absolute;
            top: 0; left: 0; right: 0; bottom: 0;
            background: repeating-linear-gradient(
                0deg,
                rgba(0,0,0,0) 0px,
                rgba(0,0,0,0) 2px,
                rgba(0,0,0,0.05) 3px
            );
            pointer-events: none;
            z-index: 10;
        }

        /* --- SLIDER TRACK --- */
        .slider-track {
            display: flex;
            height: 100%;
            width: 100%;
            /* JS ile burayı manipüle edeceğiz */
            transition: transform 0.5s cubic-bezier(0.2, 0.8, 0.2, 1);
        }

        .slide {
            min-width: 100%;
            height: 100%;
            flex-shrink: 0;
            display: flex;
            justify-content: center;
            align-items: center;
            overflow: hidden;
        }

        /* --- CONTROL BAR --- */
        .status-bar {
            border-top: 3px solid var(--main-black);
            padding: 12px 20px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            background: #eeeeee;
        }

        .btn-retro {
            background: var(--window-bg);
            color: var(--main-black);
            border: 2px solid var(--main-black);
            padding: 10px 24px;
            font-family: inherit;
            font-weight: bold;
            font-size: 14px;
            cursor: pointer;
            box-shadow: 4px 4px 0px var(--main-black);
            transition: all 0.1s;
        }

        .btn-retro:hover:not(:disabled) {
            transform: translate(1px, 1px);
            box-shadow: 3px 3px 0px var(--main-black);
        }

        .btn-retro:active:not(:disabled) {
            transform: translate(4px, 4px);
            box-shadow: 0px 0px 0px var(--main-black);
        }

        .btn-retro:disabled {
            opacity: 0.4;
            cursor: not-allowed;
            box-shadow: none;
            transform: translate(4px, 4px);
        }

        /* --- LOADING STATE --- */
        .loading-overlay {
            position: absolute;
            top: 0; left: 0; width: 100%; height: 100%;
            background: white;
            display: flex;
            justify-content: center;
            align-items: center;
            z-index: 20;
            font-size: 20px;
            font-weight: bold;
        }

        /* --- RESPONSIVE --- */
        @media (max-width: 1024px) {
            .viewport { height: 260px; }
        }
        @media (max-width: 768px) {
            .viewport { height: 220px; }
            .os-window { width: 95%; margin: 10px; }
            .btn-retro { padding: 8px 16px; font-size: 12px; }
        }";

        private const string RetroSliderTemplate = @"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Retro AI Slider</title>
    <style>" + RetroCss + @"    </style>
</head>
<body>

    <div class=""os-window"">
        <!-- Header -->
        <div class=""title-bar"">
            <span class=""title-text"">Gemini_Gen_UI.exe</span>
            <div class=""window-controls"">
                <div class=""control-dot""></div>
                <div class=""control-dot""></div>
            </div>
        </div>

        <!-- Görsel Alanı -->
        <div class=""viewport"">
            <!-- Loading Ekranı -->
            <div class=""loading-overlay"" id=""loader"">
                > SYSTEM_BOOTING...
            </div>

            <!-- TV Efekti -->
            <div class=""scanlines""></div>
            
            <!-- Slaytların Dizildiği Ray -->
            <div class=""slider-track"" id=""track"">
                <!-- JS buraya slide'ları basacak -->
            </div>
        </div>

        <!-- Kontroller -->
        <div class=""status-bar"">
            <div id=""status-text"">INDEX: 0/0</div>
            <div style=""display: flex; gap: 10px;"">
                <button class=""btn-retro"" id=""btn-prev"" disabled>&lt; PREV</button>
                <button class=""btn-retro"" id=""btn-next"" disabled>NEXT &gt;</button>
            </div>
        </div>
    </div>

    <script>
        // --- 1. MOCK DATA (Gemini'den gelen veri buraya inject edilecek) ---
        const geminiResponses = {{GEMINI_DATA_PLACEHOLDER}};

        // --- 2. DEĞİŞKENLER ---
        const track = document.getElementById('track');
        const loader = document.getElementById('loader');
        const btnPrev = document.getElementById('btn-prev');
        const btnNext = document.getElementById('btn-next');
        const statusText = document.getElementById('status-text');

        let currentIndex = 0;
        const totalSlides = geminiResponses.length;

        // --- 3. BAŞLATMA FONKSİYONU ---
        function initSlider() {
            setTimeout(() => {
                renderSlides();
                loader.style.display = 'none';
                updateControls();
            }, 1000);
        }

        // --- 4. HTML RENDER ---
        function renderSlides() {
            track.innerHTML = ''; // Temizle
            
            geminiResponses.forEach((htmlContent) => {
                const slideDiv = document.createElement('div');
                slideDiv.className = 'slide';
                slideDiv.innerHTML = htmlContent; // Gemini HTML'ini göm
                track.appendChild(slideDiv);
            });
        }

        // --- 5. NAVİGASYON ---
        function updateSliderPosition() {
            const translateX = -(currentIndex * 100);
            track.style.transform = `translateX(${translateX}%)`;
            updateControls();
        }

        function updateControls() {
            // Status yazısını güncelle
            statusText.innerText = `INDEX: ${currentIndex + 1}/${totalSlides}`;

            // Butonları aktif/pasif yap
            btnPrev.disabled = currentIndex === 0;
            btnNext.disabled = currentIndex === totalSlides - 1;
        }

        // Event Listeners
        btnPrev.addEventListener('click', () => {
            if (currentIndex > 0) {
                currentIndex--;
                updateSliderPosition();
            }
        });

        btnNext.addEventListener('click', () => {
            if (currentIndex < totalSlides - 1) {
                currentIndex++;
                updateSliderPosition();
            }
        });

        // --- 6. LİNK YÖNLENDİRME ---
        function handleLinkClicks() {
            document.addEventListener('click', function (e) {
                var anchor = e.target.closest('a');
                if (!anchor) return;
                var href = anchor.getAttribute('href');
                if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;
                e.preventDefault();
                window.top.location.href = href;
            });
        }

        initSlider();
        handleLinkClicks();

    </script>
</body>
</html>";

        public async Task<string?> GenerateBannerHtml(string apiKey, string prompt)
        {
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrWhiteSpace(prompt))
            {
                return null;
            }

            // Prompt Gemini for a JSON array of 3 banner HTML snippets
            var systemPrompt = @"You are a creative frontend developer specializing in retro/brutalist design. 
Create 3 distinct, high-quality HTML banner contents based on the user's prompt.
The banners will be rendered in a full-width iframe with a viewport height of about 320px on desktop, 260px on medium screens, and 220px on mobile. 

CRITICAL MOBILE REQUIREMENTS:
1. Ensure all text wraps correctly on small screens (width < 350px).
2. Do NOT use fixed widths (px) that exceed 300px. Use percentages (%) or flexbox.
3. Images and containers must be responsive (max-width: 100%).
4. Font sizes should adjust or be small enough to fit mobile screens.

RETURN ONLY A JSON ARRAY OF STRINGS. No markdown formatting, no explanations.
Examples: 
[""<div style='...'>...</div>"", ""<div>...</div>"", ""<div>...</div>""]
Each HTML string must be self-contained with inline CSS, ready to be placed inside a flex container.";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = systemPrompt + "\n\nUser Prompt: " + prompt }
                        }
                    }
                }
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-pro:generateContent?key={apiKey}";

            try
            {
                var response = await SendRequestWithRetryAsync(url, content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Gemini Banner API Error: {error}");
                    return null;
                }

                var resultJson = await response.Content.ReadAsStringAsync();
                dynamic? result = JsonConvert.DeserializeObject(resultJson);

                if (result == null || result!.candidates == null || result!.candidates.Count == 0)
                {
                    return null;
                }

                string text = result!.candidates[0].content.parts[0].text;
                
                // Sanitization to ensure we get a valid JS array
                text = text.Trim();
                if (text.StartsWith("```json")) text = text.Replace("```json", "").Replace("```", "");
                if (text.StartsWith("```")) text = text.Replace("```", "");
                text = text.Trim();

                // Security: Parse and Sanitize each slide
                try
                {
                    var slides = JsonConvert.DeserializeObject<List<string>>(text);
                    if (slides != null)
                    {
                        for (int i = 0; i < slides.Count; i++)
                        {
                            slides[i] = SanitizeHtml(slides[i]);
                        }
                        text = JsonConvert.SerializeObject(slides);
                    }
                }
                catch
                {
                    // If parsing fails, we proceed with raw text (might be single string or invalid JSON)
                    // But in a strict security context, we might want to fail or sanitize the whole blob.
                    // For now, let's just sanitize the whole blob as a fallback if it looks like HTML
                    if (text.Contains("<"))
                    {
                         text = SanitizeHtml(text);
                         // If it was a single string, we need to wrap it in array for the slider to work
                         text = JsonConvert.SerializeObject(new[] { text });
                    }
                }

                string finalHtml = WrapInRetroTemplate(text);
                
                return finalHtml;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Gemini Banner Exception: {ex.Message}");
                return null;
            }
        }

        private async Task<HttpResponseMessage> SendRequestWithRetryAsync(string url, HttpContent content, int maxRetries = 3)
        {
            for (int i = 0; i <= maxRetries; i++)
            {
                // İlk istekten sonra içerik atıldığı veya tüketildiği için kopyalamamız gerekiyor.
                // String içeriği için daha basit bir yol: yeniden oluşturmak.
                // StringContent tipinde bir 'content' geçtiğimiz için, okumadan jenerik olarak kopyalamak kolay değil.
                // Bu özel servis için bunun StringContent(json, Encoding.UTF8, "application/json") olduğunu biliyoruz.
                // Çağıran tarafın JSON dizesini geçmesini sağlayalım VEYA burada okuyalım.

                // Aslında, kopyalama karmaşıklığından kaçınmak veya basit tutmak için yeniden deneme işlemini metotların içinde yapalım.
                // Ancak DRY prensibine uymak için, çağıranın yeni bir StringContent geçtiğini veya bizim onu yeniden yapılandırdığımızı varsayacağız.
                // İçeriğin imha edilmesi (disposal) konusundaki güvenlik için yeniden deneme döngüsünü metotların içinde yapacak şekilde biraz refaktör edelim VEYA içeriği doğrudan okuyalım.
                
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                string json = await content.ReadAsStringAsync();
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.StatusCode != System.Net.HttpStatusCode.TooManyRequests)
                {
                    return response;
                }

                if (i == maxRetries)
                {
                    return response;
                }

                int delay = 2000 * (int)Math.Pow(2, i); // 2s, 4s, 8s
                Console.WriteLine($"Token Limit Exceeded (429). Retrying in {delay}ms...");
                await Task.Delay(delay);
            }
            return null!; // Should not happen
        }

        public async Task<string?> GetCompanyGeminiKeyAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (string.IsNullOrEmpty(company?.GeminiApiKey)) return null;

            var decrypted = _encryptionService.Decrypt(company.GeminiApiKey);
            return !string.IsNullOrEmpty(decrypted) ? decrypted : null;
        }

        public async Task<string?> GetCompanyCommentPromptAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            return company?.GeminiPrompt;
        }

        public async Task<bool> IsAiModerationEnabledAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            return company?.IsAiModerationEnabled ?? false;
        }

        public async Task<bool> UpdateCompanyAiModerationAsync(int companyId, bool isEnabled)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null)
            {
                return false;
            }
            company.IsAiModerationEnabled = isEnabled;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyGeminiKeyAsync(int companyId, string apiKey)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null) return false;

            company.GeminiApiKey = !string.IsNullOrEmpty(apiKey) ? _encryptionService.Encrypt(apiKey) : null;
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateCompanyGeminiSettingsAsync(int companyId, string apiKey, string? commentPrompt)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null) return false;

            var existingKey = await GetCompanyGeminiKeyAsync(companyId) ?? string.Empty;
            var keyPart = apiKey;
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "*****")
            {
                keyPart = existingKey;
            }

            company.GeminiApiKey = !string.IsNullOrEmpty(keyPart) ? _encryptionService.Encrypt(keyPart) : null;
            company.GeminiPrompt = commentPrompt;
            
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<AiSettingsDto?> GetAiSettingsAsync(int companyId)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null) return null;

            var decryptedKey = !string.IsNullOrEmpty(company.GeminiApiKey) 
                ? _encryptionService.Decrypt(company.GeminiApiKey) 
                : string.Empty;

            return new AiSettingsDto
            {
                GeminiApiKey = decryptedKey ?? string.Empty,
                CommentModerationPrompt = company.GeminiPrompt ?? string.Empty,
                IsAiModerationEnabled = company.IsAiModerationEnabled
            };
        }

        public async Task<bool> UpdateAiSettingsAsync(int companyId, AiSettingsDto settings)
        {
            var company = await _dbContext.Companies.SingleOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null) return false;

            if (!string.IsNullOrWhiteSpace(settings.GeminiApiKey) && settings.GeminiApiKey != "*****")
            {
                company.GeminiApiKey = _encryptionService.Encrypt(settings.GeminiApiKey);
            }

            company.GeminiPrompt = settings.CommentModerationPrompt;
            company.IsAiModerationEnabled = settings.IsAiModerationEnabled;

            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
