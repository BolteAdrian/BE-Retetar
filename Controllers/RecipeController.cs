using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Retetar.Interfaces;
using Retetar.Services;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipeController : ControllerBase
    {
        private readonly RecipeService _RecipeService;

        public RecipeController(RecipeService RecipeService)
        {
            _RecipeService = RecipeService;
        }

        /// <summary>
        /// Retrieves all Recipes from the database.
        /// </summary>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a list of all Recipes if successful.
        /// If no Recipes are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult GetAllRecipes()
        {
            try
            {
                var Recipes = _RecipeService.GetAllRecipes();

                if (Recipes == null)
                {
                    return NotFound(RECIPE.NOT_FOUND);
                }

                return Ok(Recipes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a paginated list of Recipes based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of Recipes if successful.
        /// If no Recipes are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("paginated")]
        public IActionResult GetAllRecipesPaginated([FromQuery] IPaginationAndSearchOptions options)
        {
            try
            {
                var Recipes = _RecipeService.GetAllRecipesPaginated(options);

                if (Recipes == null)
                {
                    return NotFound(RECIPE.NOT_FOUND);
                }
                return Ok(Recipes);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a Recipe by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Recipe.</param>
        /// <returns>
        /// Returns the Recipe's information if found.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If no Recipe is found with the specified ID, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("{id}")]
        public IActionResult GetRecipeById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(INVALID_ID);
                }
                var Recipe = _RecipeService.GetRecipeById(id);

                if (Recipe == null)
                {
                    return NotFound(RECIPE.NOT_FOUND);
                }

                return Ok(Recipe);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a new Recipe to the database.
        /// </summary>
        /// <param name="Recipe">The Recipe information to be added.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a CreatedAtAction response with the URL of the newly created Recipe if successful.
        /// If the provided Recipe data is invalid, returns a BadRequest response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpPost]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult AddRecipe([FromBody] Recipe Recipe)
        {
            try
            {
                if (Recipe == null)
                {
                    return BadRequest(INVALID_DATA);
                }

                _RecipeService.AddRecipe(Recipe);

                return CreatedAtAction(nameof(GetRecipeById), new { id = Recipe.Id }, Recipe);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_SAVED, error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing Recipe's information in the database.
        /// </summary>
        /// <param name="id">The ID of the Recipe to be updated.</param>
        /// <param name="Recipe">The updated Recipe information.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If successful, returns a success message.
        /// If the provided ID is invalid, returns a BadRequest response.
        /// If the provided Recipe data is invalid, returns a BadRequest response.
        /// If the Recipe with the specified ID is not found, returns a NotFound response.
        /// If an error occurs during the update operation, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult UpdateRecipe(int id, [FromBody] Recipe Recipe)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(INVALID_ID);
                }

                if (Recipe == null)
                {
                    return BadRequest(INVALID_DATA);
                }

                _RecipeService.UpdateRecipe(id, Recipe);

                return Ok(new { message = RECIPE.SUCCES_UPDATING });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.ERROR_UPDATING, error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a Recipe from the database based on its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Recipe to be deleted.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If the Recipe with the specified ID is not found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult DeleteRecipe(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(INVALID_ID);
                }

                _RecipeService.DeleteRecipe(id);
                return Ok(new { message = RECIPE.SUCCES_DELETING });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.ERROR_DELETING, error = ex.Message });
            }
        }
    }
}