using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Retetar.Interfaces;
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
        /// Retrieves all Ingredients from the database.
        /// </summary>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a list of all Ingredients if successful.
        /// If no Ingredients are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult GetAllIngredients()
        {
            try
            {
                var Ingredients = _IngredientService.GetAllIngredients();

                if (Ingredients == null)
                {
                    return NotFound(INGREDIENT.NOT_FOUND);
                }

                return Ok(Ingredients);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
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
        [HttpGet("paginated")]
        public IActionResult GetAllIngredientsPaginated([FromQuery] IPaginationAndSearchOptions options)
        {
            try
            {
                var Ingredients = _IngredientService.GetAllIngredientsPaginated(options);

                if (Ingredients == null)
                {
                    return NotFound(INGREDIENT.NOT_FOUND);
                }
                return Ok(Ingredients);
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
        public IActionResult GetIngredientById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(INVALID_ID);
                }
                var Ingredient = _IngredientService.GetIngredientById(id);

                if (Ingredient == null)
                {
                    return NotFound(INGREDIENT.NOT_FOUND);
                }

                return Ok(Ingredient);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a new Ingredient to the database.
        /// </summary>
        /// <param name="Ingredient">The Ingredient information to be added.</param>
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
        public IActionResult AddIngredient([FromBody] Ingredient Ingredient)
        {
            try
            {
                if (Ingredient == null)
                {
                    return BadRequest(INVALID_DATA);
                }

                _IngredientService.AddIngredient(Ingredient);

                return CreatedAtAction(nameof(GetIngredientById), new { id = Ingredient.Id }, Ingredient);
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
        /// <param name="Ingredient">The updated Ingredient information.</param>
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
        public IActionResult UpdateIngredient(int id, [FromBody] Ingredient Ingredient)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(INVALID_ID);
                }

                if (Ingredient == null)
                {
                    return BadRequest(INVALID_DATA);
                }

                _IngredientService.UpdateIngredient(id, Ingredient);

                return Ok(new { message = INGREDIENT.SUCCES_UPDATING });
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
                    return BadRequest(INVALID_ID);
                }

                _IngredientService.DeleteIngredient(id);
                return Ok(new { message = INGREDIENT.SUCCES_DELETING });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.ERROR_DELETING, error = ex.Message });
            }
        }
    }
}
