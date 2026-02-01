using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.DTOs;
using DomusMercatorisDotnetRest.Services;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly CategoryService _categoryService;

        public CategoriesController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Retrieves all categories.
        /// </summary>
        /// <returns>A list of all categories.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            var categories = await _categoryService.GetAllAsync();
            return Ok(categories);
        }

        /// <summary>
        /// Retrieves all auto categories (cached).
        /// </summary>
        /// <returns>A list of root auto categories with their children.</returns>
        [HttpGet("auto")]
        [ProducesResponseType(typeof(IEnumerable<AutoCategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<AutoCategoryDto>>> GetAutoCategories()
        {
            var autoCategories = await _categoryService.GetAutoCategoriesAsync();
            return Ok(autoCategories);
        }

        /// <summary>
        /// Invalidates the auto categories cache.
        /// Should be called when auto categories are modified.
        /// </summary>
        [HttpPost("auto/invalidate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult InvalidateAutoCategoriesCache()
        {
            _categoryService.InvalidateAutoCategoriesCache();
            return NoContent();
        }

        /// <summary>
        /// Retrieves a specific category by ID.
        /// </summary>
        /// <param name="id">The ID of the category.</param>
        /// <returns>The requested category.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CategoryDto>> GetById(int id)
        {
            var category = await _categoryService.GetByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }

        // Manual mapping removed in favor of AutoMapper profiles
    }
}
