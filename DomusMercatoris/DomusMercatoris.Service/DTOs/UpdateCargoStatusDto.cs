using DomusMercatoris.Core.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace DomusMercatoris.Service.DTOs
{
    public class UpdateCargoStatusDto
    {
        [Required]
        public string TrackingNumber { get; set; } = string.Empty;

        [Required]
        public CargoStatus NewStatus { get; set; }

        public string? Description { get; set; }
    }
}