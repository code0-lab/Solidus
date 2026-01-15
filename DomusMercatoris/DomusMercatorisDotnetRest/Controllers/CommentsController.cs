using Microsoft.AspNetCore.Mvc;
using DomusMercatoris.Service.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using DomusMercatoris.Service.Services;

namespace DomusMercatorisDotnetRest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommentsController : ControllerBase
    {
        private readonly ICommentService _commentService;

        public CommentsController(ICommentService commentService)
        {
            _commentService = commentService;
        }

        private long? GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) 
                           ?? User.FindFirst("sub")
                           ?? User.FindFirst(JwtRegisteredClaimNames.Sub)
                           ?? User.FindFirst("id");
                           
            if (userIdClaim != null && long.TryParse(userIdClaim.Value, out long userId))
            {
                return userId;
            }
            return null;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetAll()
        {
            var comments = await _commentService.GetAllAsync();
            return Ok(comments);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CommentDto>> GetById(int id)
        {
            var comment = await _commentService.GetByIdAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            return Ok(comment);
        }

        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<CommentDto>>> GetByProductId(long productId)
        {
            var userId = GetUserId();
            var comments = await _commentService.GetByProductIdAsync(productId, userId);
            return Ok(comments);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<CommentDto>> Create(CreateCommentDto createDto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var createdComment = await _commentService.CreateAsync(createDto, userId.Value);
            return CreatedAtAction(nameof(GetById), new { id = createdComment.Id }, createdComment);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, UpdateCommentDto updateDto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole("admin");
            await _commentService.UpdateAsync(id, updateDto, userId.Value, isAdmin);
            
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var isAdmin = User.IsInRole("admin");
            await _commentService.DeleteAsync(id, userId.Value, isAdmin);

            return NoContent();
        }
    }
}
