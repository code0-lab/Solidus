using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace DomusMercatorisDotnetMVC.Dto.ProductDto
{
    public class ProductUpdateDto : IValidatableObject
    {
        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string Sku { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public int? SubCategoryId { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } = 0;

        public List<IFormFile?> ImageFiles { get; set; } = new List<IFormFile?>();
        public List<string> RemoveImages { get; set; } = new List<string>();
        public List<string> ImageOrder { get; set; } = new List<string>();
        public List<int> CategoryIds { get; set; } = new List<int>();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Price <= 0)
            {
                yield return new ValidationResult("Price must be greater than 0.", new[] { nameof(Price) });
                yield break;
            }
            var files = (ImageFiles ?? new List<IFormFile?>()).Where(f => f != null && f.Length > 0).Select(f => f!).ToList();
            if (files.Count > 4)
            {
                yield return new ValidationResult("You can upload up to 4 images.", new[] { nameof(ImageFiles) });
                yield break;
            }

            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
            foreach (var f in files)
            {
                var ext = System.IO.Path.GetExtension(f.FileName);
                if (string.IsNullOrEmpty(ext) || !allowed.Contains(ext))
                {
                    yield return new ValidationResult("Unsupported image type. Allowed: .jpg, .jpeg, .png, .webp", new[] { nameof(ImageFiles) });
                    yield break;
                }
            }
        }
    }
}
