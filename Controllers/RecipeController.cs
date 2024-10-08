﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Retetar.DataModels;
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
        /// Retrieves a paginated list of Recipes based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of Recipes if successful.
        /// If no Recipes are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet]
        [Authorize]
        public IActionResult GetAllRecipesPaginated([FromBody] PaginationAndSearchOptionsDto options)
        {
            try
            {
                var recipes = _RecipeService.GetAllRecipesPaginated(options);

                if (recipes == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = RECIPE.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, recipes});
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a paginated list of Recipes by category.
        /// </summary>
        /// <param name="id">The unique identifier of the category.</param>
        /// <returns>
        /// Returns a list of Recipes by category if successful.
        /// If no Recipes are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("category/{id}")]
        [Authorize]
        public IActionResult GetAllRecipesByCategory(int id)
        {
            try
            {
                var recipes = _RecipeService.GetAllRecipesByCategory(id);

                if (recipes == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = RECIPE.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, recipes });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves Recipe details by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Recipe.</param>
        /// <returns>
        /// Returns the Recipe's information if found.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If no Recipe is found with the specified ID, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("{id}")]
        [Authorize]
        public IActionResult GetRecipeDetails(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var recipe = _RecipeService.GetRecipeDetails(id);

                if (recipe == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = RECIPE.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, recipe });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the history of prepared recipes.
        /// </summary>
        /// <remarks>
        /// This endpoint retrieves the history of prepared recipes, including details such as when they were prepared and by whom.
        /// </remarks>
        /// <returns>
        /// Returns the history of prepared recipes.
        /// If there is no history available or any error occurs during processing, appropriate HTTP responses are returned.
        /// </returns>
        [HttpGet("prepared-recipes")]
        [Authorize]
        public IActionResult GetPreparedRecipeHistory()
        {
            try
            {
                var result = _RecipeService.GetPreparedRecipeHistory();

                if (result == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = RECIPE.ERROR_GETTING_HISTORY });
                }

                return Ok(new { status = StatusCodes.Status200OK, result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves predictions of prepared recipes for the next year.
        /// </summary>
        /// <remarks>
        /// This endpoint retrieves the history of prepared recipes, including details such as the amount prepared and the date of preparation.
        /// Additionally, it uses an ML.NET model to predict the amount that will be prepared in the next year based on historical data.
        /// </remarks>
        /// <returns>
        /// Returns a list of prepared recipes along with their predicted amounts for the next year.
        /// If there is no history available or any error occurs during processing, appropriate HTTP responses are returned.
        /// </returns>
        /// <summary>
        /// Retrieves the history of prepared recipes and makes predictions for the next year.
        /// </summary>
        /// <remarks>
        /// This endpoint retrieves the history of prepared recipes, including details such as the amount prepared and the date of preparation.
        /// Additionally, it uses an ML.NET model to predict the amount that will be prepared in the next year based on historical data.
        /// </remarks>
        /// <returns>
        /// Returns a list of prepared recipes along with their predicted amounts for the next year.
        /// If there is no history available or any error occurs during processing, appropriate HTTP responses are returned.
        /// </returns>
        [HttpGet("prediction-consume")]
        [Authorize]
        public async Task<ActionResult> GetPredictionConsumeAsync()
        {
            try
            {

                var preparedRecipesWithPredictions = await _RecipeService.GetPreparedRecipesWithPredictions();
                return Ok(preparedRecipesWithPredictions);

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves the maximum number of times a Recipe can be prepared based on available ingredients.
        /// </summary>
        /// <param name="id">The identifier of the Recipe to calculate the amount for.</param>
        /// <remarks>
        /// This endpoint calculates the maximum number of times the specified Recipe can be prepared
        /// considering the availability of required ingredients and their quantities in the inventory.
        /// </remarks>
        /// <returns>
        /// Returns the maximum number of times the Recipe can be prepared with available ingredients.
        /// If the Recipe does not exist or any error occurs during processing, appropriate HTTP responses are returned.
        /// </returns>
        [HttpGet("{id}/amount")]
        [Authorize]
        public IActionResult GetRecipeAmount(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var recipeAmount = _RecipeService.GetRecipeAmount(id);

                return Ok(new { status = StatusCodes.Status200OK, recipeAmount });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Checks if the given quantity is available for the specified Recipe.
        /// </summary>
        /// <param name="id">The identifier of the Recipe to check.</param>
        /// <param name="quantity">The quantity to be checked for availability.</param>
        /// <remarks>
        /// This endpoint checks if the provided quantity can be prepared for the specified Recipe
        /// based on the availability of ingredients and their quantities.
        /// </remarks>
        /// <returns>
        /// Returns an OK response with a message indicating the availability of the specified quantity
        /// for the Recipe. If the quantity is available, it confirms the possibility of preparing the Recipe.
        /// If there's not enough quantity available, it specifies the missing amount and ingredient.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpPost("{id}/submit-amount")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult SubmitRecipeQuantity(int id, [FromBody] int quantity)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var (available, missingIngredients) = _RecipeService.SubmitRecipeQuantity(id, quantity);

                if (available)
                {
                    return Ok(new { status = StatusCodes.Status200OK, message = RECIPE.QUANTITY_AVAILABLE });
                }
                else
                {
                    var missingIngredientsMessage = RECIPE.QUANTITY_UNAVAILABLE;

                    return Ok(new
                    {
                        status = StatusCodes.Status400BadRequest,
                        message = missingIngredientsMessage,
                        missingIngredients
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a new Recipe to the database.
        /// </summary>
        /// <param name="recipe">The Recipe information to be added.</param>
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
        public IActionResult AddRecipe([FromBody] RecipeEditorDto recipe)
        {
            try
            {
                if (recipe == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                _RecipeService.AddRecipe(recipe);

                // Return the created recipe's details
                return CreatedAtAction(nameof(GetRecipeDetails), new { id = recipe.Recipe.Id }, new { status = StatusCodes.Status201Created });
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
        /// <param name="recipe">The updated Recipe information.</param>
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
        public IActionResult UpdateRecipe(int id, [FromBody] RecipeEditorDto recipe)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                if (recipe == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                var updateResult = _RecipeService.UpdateRecipe(id, recipe);

                if (updateResult != null)
                {
                    return Ok(new { status = StatusCodes.Status200OK, message = RECIPE.SUCCESS_UPDATING });
                }
                else
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = RECIPE.NOT_FOUND });
                }
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
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var deleteResult = _RecipeService.DeleteRecipe(id);

                if (deleteResult)
                {
                    return Ok(new { status = StatusCodes.Status200OK, message = RECIPE.SUCCESS_DELETING });
                }
                else
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = RECIPE.NOT_FOUND });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = RECIPE.ERROR_DELETING, error = ex.Message });
            }
        }
    }
}