using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using DomusMercatoris.Data;
using DomusMercatorisDotnetMVC.Services;
using DomusMercatoris.Service.Services;
using DomusMercatoris.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using DomusMercatoris.Core.Entities;

namespace DomusMercatorisDotnetMVC.Pages.Moderator
{
    [Authorize(Roles = "Moderator,Rex")]
    public class BannersModel : PageModel
    {
        private readonly DomusDbContext _db;
        private readonly UserService _userService;
        private readonly GeminiService _geminiService;
        private readonly BannerService _bannerService;

        public BannersModel(
            DomusDbContext db,
            UserService userService,
            GeminiService geminiService,
            BannerService bannerService)
        {
            _db = db;
            _userService = userService;
            _geminiService = geminiService;
            _bannerService = bannerService;
        }

        public List<BannerDto> Banners { get; set; } = new();
        public List<Company> Companies { get; set; } = new();

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            public int CompanyId { get; set; }

            [Required]
            [StringLength(200)]
            public string Topic { get; set; } = string.Empty;
        }

        public async Task OnGetAsync()
        {
            Companies = await _db.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();

            if (Input.CompanyId == 0 && Companies.Count > 0)
            {
                Input.CompanyId = Companies[0].CompanyId;
            }

            await LoadBannersAsync(Input.CompanyId);
        }

        public async Task<IActionResult> OnPostGenerateAsync()
        {
            Companies = await _db.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                await LoadBannersAsync(Input.CompanyId);
                return Page();
            }

            var apiKey = _userService.GetCompanyGeminiKey(Input.CompanyId);
            if (string.IsNullOrEmpty(apiKey))
            {
                ModelState.AddModelError(string.Empty, "Selected company does not have a Gemini API key configured.");
                await LoadBannersAsync(Input.CompanyId);
                return Page();
            }

            var prompt = BuildBannerPrompt(Input.Topic);
            var html = await _geminiService.GenerateBannerHtml(apiKey, prompt);
            var cleanedHtml = CleanGeminiHtml(html);

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
            Companies = await _db.Companies
                .OrderBy(c => c.Name)
                .ToListAsync();

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

            if (cleaned.StartsWith("```"))
            {
                var firstNewLine = cleaned.IndexOf('\n');
                if (firstNewLine >= 0 && firstNewLine + 1 < cleaned.Length)
                {
                    cleaned = cleaned.Substring(firstNewLine + 1);
                }
                else
                {
                    cleaned = cleaned.Substring(3);
                }
            }

            var lastFenceIndex = cleaned.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFenceIndex >= 0)
            {
                cleaned = cleaned.Substring(0, lastFenceIndex);
            }

            return cleaned.Trim();
        }

        private string BuildBannerPrompt(string userTopic)
        {
            return
                "DESIGN CONTEXT (Sitenin Tasarım Kuralları): <br/> " +
                "Aşağıdaki CSS değişkenlerini ve kurallarını KESİNLİKLE kullanmalısın: " +
                "- Ana Renk (Primary): #000000 " +
                "- İkincil Renk: #ffffff " +
                "- Font Ailesi: \"Geneva\", \"Chicago\", \"Monaco\", \"Courier New\", monospace " +
                "- Köşe Yuvarlaklığı (Border Radius): 2px " +
                "- Genel Stil: Retro Macintosh / 90’lar işletim sistemi arayüzü, siyah-beyaz ağırlıklı, piksel gölgeli pencereler, brutalist kutular, düşük radius’lu butonlar, aralarda neon/glitch efektleri ve oyun referansları (GTA, cyberpunk 404 yağmurlu sahne vb.) " +
                "CONTENT (İçerik): <br/> " +
                "Banner Konusu: \"" + userTopic + "\" (Kullanıcıdan gelen konu) <br/> " +
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
    }
}
