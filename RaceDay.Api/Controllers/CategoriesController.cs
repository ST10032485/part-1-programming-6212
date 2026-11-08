using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CategoriesController(AppDbContext db)
    {
        _db = db;
    }

    private static CategoryDto ToDto(Category c) =>
        new(c.CategoryId, c.CategoryName, c.DistanceKm, c.Description);

    /// <summary>Lists all race categories (age or distance based).</summary>
    /// <response code="200">List of categories.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var categories = await _db.Categories.OrderBy(c => c.CategoryName).ToListAsync();
        return categories.Select(ToDto).ToList();
    }

    /// <summary>Returns one category.</summary>
    /// <response code="200">The category.</response>
    /// <response code="404">Category not found.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> GetById(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();
        return ToDto(category);
    }

    /// <summary>Creates a new category. Organisers only.</summary>
    /// <remarks>Send categoryName and optionally distanceKm and description. Leave distanceKm empty for age categories.</remarks>
    /// <response code="201">Category created.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    /// <response code="409">A category with that name already exists.</response>
    [HttpPost]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Create(CategoryRequest request)
    {
        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryName == name))
            return Conflict("A category with that name already exists.");

        var category = new Category
        {
            CategoryName = name,
            DistanceKm = request.DistanceKm,
            Description = request.Description
        };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = category.CategoryId }, ToDto(category));
    }

    /// <summary>Updates a category. Organisers only.</summary>
    /// <response code="200">The updated category.</response>
    /// <response code="400">Invalid data.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">Another category already uses that name.</response>
    [HttpPut("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDto>> Update(int id, CategoryRequest request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        var name = request.CategoryName.Trim();
        if (await _db.Categories.AnyAsync(c => c.CategoryName == name && c.CategoryId != id))
            return Conflict("Another category already uses that name.");

        category.CategoryName = name;
        category.DistanceKm = request.DistanceKm;
        category.Description = request.Description;
        await _db.SaveChangesAsync();

        return ToDto(category);
    }

    /// <summary>Deletes a category that is not used by any event. Organisers only.</summary>
    /// <response code="204">Category deleted.</response>
    /// <response code="401">Not logged in.</response>
    /// <response code="403">Not an Organiser.</response>
    /// <response code="404">Category not found.</response>
    /// <response code="409">The category is used by an event.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Organiser)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null) return NotFound();

        if (await _db.EventCategories.AnyAsync(ec => ec.CategoryId == id))
            return Conflict("This category is used by an event.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
