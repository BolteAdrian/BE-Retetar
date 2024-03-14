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
    public class IngredientController : ControllerBase
    {
        private readonly IngredientService _IngredientService;

        public IngredientController(IngredientService IngredientService)
        {
            _IngredientService = IngredientService;
        }

        /// <summary>
        /// Retrieves a paginated list of Ingredients based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of Ingredients if successful.
        /// If no Ingredients are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpPost("search")]
        [Authorize]
        public IActionResult GetAllIngredientsPaginated([FromBody] IPaginationAndSearchOptions options)
        {
            try
            {
                var ingredients = _IngredientService.GetAllIngredientsPaginated(options);

                if (ingredients == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = INGREDIENT.NOT_FOUND });
                }
                return Ok(new { status = StatusCodes.Status200OK, ingredients });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a list of all Ingredients.
        /// </summary>
        /// <returns>
        /// Returns a list of Ingredients if successful.
        /// If no Ingredients are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet]
        [Authorize]
        public IActionResult GetAllIngredients()
        {
            try
            {
                var ingredients = _IngredientService.GetAllIngredients();

                if (ingredients == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = INGREDIENT.NOT_FOUND });
                }
                return Ok(new { status = StatusCodes.Status200OK, ingredients });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a list of all Ingredients by category.
        /// </summary>
        /// <param name="id">The unique identifier of the category.</param>
        /// <returns>
        /// Returns a list of Ingredients if successful.
        /// If no Ingredients are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("category/{id}")]
        [Authorize]
        public IActionResult GetAllIngredientsByCategory(int id)
        {
            try
            {
                var ingredients = _IngredientService.GetAllIngredientsByCategory(id);

                if (ingredients == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = INGREDIENT.NOT_FOUND });
                }
                return Ok(new { status = StatusCodes.Status200OK, ingredients });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a Ingredient by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Ingredient.</param>
        /// <returns>
        /// Returns the Ingredient's information if found.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If no Ingredient is found with the specified ID, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("{id}")]
        [Authorize]
        public IActionResult GetIngredientById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var ingredient = _IngredientService.GetIngredientById(id);

                if (ingredient == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = INGREDIENT.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, ingredient });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a new Ingredient to the database.
        /// </summary>
        /// <param name="ingredient">The Ingredient information to be added.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a CreatedAtAction response with the URL of the newly created Ingredient if successful.
        /// If the provided Ingredient data is invalid, returns a BadRequest response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpPost]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult AddIngredient([FromBody] Ingredient ingredient)
        {
            try
            {
                if (ingredient == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                _IngredientService.AddIngredient(ingredient);

                return CreatedAtAction(nameof(GetIngredientById), new { id = ingredient.Id }, new { status = StatusCodes.Status201Created, ingredient });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_SAVED, error = ex.Message });
            }
        }


        /// <summary>
        /// Updates an existing Ingredient's information in the database.
        /// </summary>
        /// <param name="id">The ID of the Ingredient to be updated.</param>
        /// <param name="ingredient">The updated Ingredient information.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If successful, returns a success message.
        /// If the provided ID is invalid, returns a BadRequest response.
        /// If the provided Ingredient data is invalid, returns a BadRequest response.
        /// If the Ingredient with the specified ID is not found, returns a NotFound response.
        /// If an error occurs during the update operation, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult UpdateIngredient(int id, [FromBody] Ingredient ingredient)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                if (ingredient == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                _IngredientService.UpdateIngredient(id, ingredient);

                return Ok(new { status = StatusCodes.Status200OK, message = INGREDIENT.SUCCESS_UPDATING });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.ERROR_UPDATING, error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a Ingredient from the database based on its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Ingredient to be deleted.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If the Ingredient with the specified ID is not found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult DeleteIngredient(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                _IngredientService.DeleteIngredient(id);
                return Ok(new { status = StatusCodes.Status200OK, message = INGREDIENT.SUCCESS_DELETING });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.ERROR_DELETING, error = ex.Message });
            }
        }
    }
}
