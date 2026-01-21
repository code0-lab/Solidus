using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using DomusMercatoris.Core.Entities;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using System.Linq;
using Newtonsoft.Json;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    [Authorize(Roles = "Moderator,Rex")]
    public class BannersModel : PageModel
    {
        private readonly UserService _userService;
        private readonly GeminiService _geminiService;
        private readonly BannerService _bannerService;
        private readonly IConfiguration _configuration;

        public BannersModel(
            UserService userService,
            GeminiService geminiService,
            BannerService bannerService,
            IConfiguration configuration)
        {
            _userService = userService;
            _geminiService = geminiService;
            _bannerService = bannerService;
            _configuration = configuration;
        }

        public List<BannerDto> Banners { get; set; } = new();
        public List<Company> Companies { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public EditModel Edit { get; set; } = new();

        public class InputModel
        {
            [Required]
            public int CompanyId { get; set; }

            [Required]
            [StringLength(200)]
            public string Topic { get; set; } = string.Empty;

            public string HtmlContent { get; set; } = string.Empty;
        }

        public class EditModel
        {
            public int Id { get; set; }
            public int CompanyId { get; set; }
            public string HtmlContent { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(int? companyId)
        {
            await LoadSharedDataAsync();

            if (companyId.HasValue && companyId.Value > 0)
            {
                Input.CompanyId = companyId.Value;
            }
            else if (Input.CompanyId == 0 && Companies.Count > 0)
            {
                Input.CompanyId = Companies[0].CompanyId;
            }

            await LoadBannersAsync(Input.CompanyId);
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            await LoadSharedDataAsync();

            if (!ModelState.IsValid)
            {
                await LoadBannersAsync(Input.CompanyId);
                return Page();
            }

            var apiKey = await _userService.GetCompanyGeminiKeyAsync(Input.CompanyId);
            if (string.IsNullOrEmpty(apiKey))
            {
                ModelState.AddModelError(string.Empty, "Selected company does not have a Gemini API key configured.");
                await LoadBannersAsync(Input.CompanyId);
                return Page();
            }

            var prompt = BuildBannerPrompt(Input.Topic);
            var html = await _geminiService.GenerateBannerHtml(apiKey, prompt);
            var cleanedHtml = CleanGeminiHtml(html ?? string.Empty);

            if (string.IsNullOrWhiteSpace(cleanedHtml))
            {
                ModelState.AddModelError(string.Empty, "Gemini could not generate a banner. Please try again.");
                await LoadBannersAsync(Input.CompanyId);
                return Page();
            }

            var dto = new CreateBannerDto
            {
                CompanyId = Input.CompanyId,
                Topic = Input.Topic,
                HtmlContent = cleanedHtml
            };

            await _bannerService.CreateAsync(dto);

            Input.Topic = string.Empty;
            await LoadBannersAsync(Input.CompanyId);

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id, int companyId, bool isApproved, bool isActive)
        {
            await LoadSharedDataAsync();

            var dto = new UpdateBannerStatusDto
            {
                IsApproved = isApproved,
                IsActive = isActive
            };

            await _bannerService.UpdateStatusAsync(id, companyId, dto);

            Input.CompanyId = companyId;
            await LoadBannersAsync(companyId);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id, int companyId)
        {
            await LoadSharedDataAsync();
            await _bannerService.DeleteAsync(id, companyId);
            Input.CompanyId = companyId;
            await LoadBannersAsync(companyId);
            return Page();
        }

        public async Task<IActionResult> OnPostManualCreateAsync()
        {
            await LoadSharedDataAsync();

            if (Input.CompanyId == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select a company.");
                await LoadBannersAsync(Input.CompanyId);
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Input.HtmlContent))
            {
                ModelState.AddModelError(string.Empty, "Please enter HTML content.");
                await LoadBannersAsync(Input.CompanyId);
                return Page();
            }
            
            // Security: Sanitize user input and wrap in safe template
            var sanitizedContent = _geminiService.SanitizeHtml(Input.HtmlContent);
            var json = JsonConvert.SerializeObject(new[] { sanitizedContent });
            var finalHtml = GeminiService.WrapInRetroTemplate(json);
            
            var dto = new CreateBannerDto
            {
                CompanyId = Input.CompanyId,
                Topic = !string.IsNullOrWhiteSpace(Input.Topic) ? Input.Topic : "Manual Entry",
                HtmlContent = finalHtml
            };

            await _bannerService.CreateAsync(dto);

            Input.Topic = string.Empty;
            Input.HtmlContent = string.Empty;
            await LoadBannersAsync(Input.CompanyId);

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateContentAsync()
        {
            await LoadSharedDataAsync();

            // 1. Sanitize the input to remove malicious scripts/tags
            var rawContent = Edit.HtmlContent ?? string.Empty;
            var sanitizedContent = _geminiService.SanitizeHtml(rawContent);

            string finalHtml;

            // 2. Smart Wrapping Logic
            // If the content already contains the main container class "os-window", 
            // it means the user is editing the FULL banner structure (manual edit mode).
            // We should NOT wrap it in another RetroSliderTemplate (which would create nested windows).
            // Instead, we just wrap it in a basic HTML shell with the necessary CSS.
            if (sanitizedContent.Contains("os-window"))
            {
                finalHtml = GeminiService.WrapRawHtmlInRetroTemplate(sanitizedContent);
            }
            else
            {
                // If it doesn't contain "os-window", assume it's just the content for slides.
                // We wrap it in the full slider template.
                var json = JsonConvert.SerializeObject(new[] { sanitizedContent });
                finalHtml = GeminiService.WrapInRetroTemplate(json);
            }

            await _bannerService.UpdateContentAsync(Edit.Id, Edit.CompanyId, finalHtml);
            Input.CompanyId = Edit.CompanyId;
            await LoadBannersAsync(Edit.CompanyId);
            return Page();
        }

        public async Task<IActionResult> OnGetContentAsync(int id, int companyId)
        {
            var banners = await _bannerService.GetAllAsync(companyId);
            var banner = banners.FirstOrDefault(b => b.Id == id);
            if (banner == null)
            {
                return NotFound();
            }

            var html = banner.HtmlContent ?? string.Empty;
            return Content(html, "text/html");
        }

        private async Task LoadSharedDataAsync()
        {
            Companies = await _userService.GetAllCompaniesAsync();
        }

        private async Task LoadBannersAsync(int companyId)
        {
            if (companyId > 0)
            {
                Banners = await _bannerService.GetAllAsync(companyId);
            }
            else
            {
                Banners = new List<BannerDto>();
            }
        }

        private string CleanGeminiHtml(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return string.Empty;

            var cleaned = html.Trim();

            cleaned = Regex.Replace(cleaned, @"^```[^\r\n]*\r?\n", string.Empty);
            cleaned = Regex.Replace(cleaned, @"```[\s]*$", string.Empty);

            return cleaned.Trim();
        }

        private string BuildBannerPrompt(string userTopic)
        {
            var template = _configuration["BannerGeneration:PromptTemplate"];

            if (string.IsNullOrWhiteSpace(template))
            {
                template =
                    "DESIGN CONTEXT (Sitenin Tasarım Kuralları): <br/> " +
                    "Aşağıdaki CSS değişkenlerini ve kurallarını KESİNLİKLE kullanmalısın: " +
                    "- Ana Renk (Primary): #000000 " +
                    "- İkincil Renk: #ffffff " +
                    "- Font Ailesi: \"Geneva\", \"Chicago\", \"Monaco\", \"Courier New\", monospace " +
                    "- Köşe Yuvarlaklığı (Border Radius): 2px " +
                    "- Genel Stil: Retro Macintosh / 90’lar işletim sistemi arayüzü, siyah-beyaz ağırlıklı, piksel gölgeli pencereler, brutalist kutular, düşük radius’lu butonlar, aralarda neon/glitch efektleri ve oyun referansları (GTA, cyberpunk 404 yağmurlu sahne vb.) " +
                    "CONTENT (İçerik): <br/> " +
                    "Banner Konusu: \"{UserTopic}\" (Kullanıcıdan gelen konu) <br/> " +
                    "Bu konuya uygun yaratıcı bir başlık, alt metin ve bir \"Call to Action\" butonu yaz. " +
                    "TECHNICAL CONSTRAINTS (Teknik Kısıtlamalar): " +
                    "1. Çıktı formatı: SADECE ham HTML kodu. (Markdown, ```html, açıklama metni YOK). " +
                    "2. Stil: CSS'i HTML elementlerinin içine \"inline style\" olarak yaz (style=\"...\"). Harici CSS dosyası kullanma. " +
                    "3. Layout: Flexbox kullanarak içeriği ortala veya split layout yap. " +
                    "4. Görsel: <img> etiketi için `https://picsum.photos/800/400` gibi placeholder servisleri kullan ama üzerine bir linear-gradient overlay ekle ki yazılar okunsun. " +
                    "5. Uyumluluk: Buton rengi ve yazı tipleri yukarıdaki 'DESIGN CONTEXT' ile birebir aynı olmalı. " +
                    "OUTPUT: <br/> " +
                    "Sadece HTML string'i döndür.";
            }

            return template.Replace("{UserTopic}", userTopic);
        }
    }
}
