using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Retetar.Interfaces;
using Retetar.Models;
using Retetar.Services;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class CategoryController : ControllerBase
    {
        private readonly CategoryService _CategoryService;

        public CategoryController(CategoryService CategoryService)
        {
            _CategoryService = CategoryService;
        }

        /// <summary>
        /// Retrieves a paginated list of Categories based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of Categories if successful.
        /// If no Categories are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet]
        public IActionResult GetAllCategorysPaginated([FromQuery] IPaginationAndSearchOptions options)
        {
            try
            {
                var categories = _CategoryService.GetAllCategorysPaginated(options);

                if (categories == null || !categories.Any())
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = CATEGORY.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, categories = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = CATEGORY.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a Category by its unique identifier.
        /// </summary>
        /// <param name="IsRecipe">The identifier of the type of the Category.</param>
        /// <returns>
        /// Returns the all the Category of that type.
        /// If no Category is found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("{IsRecipe}")]
        public IActionResult GetCategoryByType(bool IsRecipe)
        {
            try
            {
                var category = _CategoryService.GetCategoryByType(IsRecipe);

                if (category == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = CATEGORY.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, data = category });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = CATEGORY.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a Category by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Category.</param>
        /// <returns>
        /// Returns the Category's information if found.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If no Category is found with the specified ID, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("{id}")]
        public IActionResult GetCategoryById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var category = _CategoryService.GetCategoryById(id);

                if (category == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = CATEGORY.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, data = category });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = CATEGORY.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a new Category to the database.
        /// </summary>
        /// <param name="category">The Category information to be added.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a CreatedAtAction response with the URL of the newly created Category if successful.
        /// If the provided Category data is invalid, returns a BadRequest response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpPost]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult AddCategory([FromBody] Category category)
        {
            try
            {
                if (category == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                _CategoryService.AddCategory(category);

                return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, new { status = StatusCodes.Status201Created, data = category });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = CATEGORY.NOT_SAVED, error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing Category's information in the database.
        /// </summary>
        /// <param name="id">The ID of the Category to be updated.</param>
        /// <param name="category">The updated Category information.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If successful, returns a success message.
        /// If the provided ID is invalid, returns a BadRequest response.
        /// If the provided Category data is invalid, returns a BadRequest response.
        /// If the Category with the specified ID is not found, returns a NotFound response.
        /// If an error occurs during the update operation, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult UpdateCategory(int id, [FromBody] Category category)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                if (category == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                _CategoryService.UpdateCategory(id, category);

                return Ok(new { status = StatusCodes.Status200OK, message = CATEGORY.SUCCESS_UPDATING });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = CATEGORY.ERROR_UPDATING, error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a Category from the database based on its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Category to be deleted.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If the Category with the specified ID is not found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult DeleteCategory(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                _CategoryService.DeleteCategory(id);
                return Ok(new { status = StatusCodes.Status200OK, message = CATEGORY.SUCCESS_DELETING });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = CATEGORY.ERROR_DELETING, error = ex.Message });
            }
        }
    }
}
