using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Retetar.DataModels;
using Retetar.Models;
using Retetar.Services;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class IngredientQuantitiesController : ControllerBase
    {
        private readonly IngredientQuantitiesService _IngredientQuantitiesService;

        public IngredientQuantitiesController(IngredientQuantitiesService IngredientQuantitiesService)
        {
            _IngredientQuantitiesService = IngredientQuantitiesService;
        }

        /// <summary>
        /// Retrieves a paginated list of IngredientQuantitiess based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of IngredientQuantitiess if successful.
        /// If no IngredientQuantitiess are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet]
        public IActionResult GetAllIngredientQuantitiesPaginated([FromBody] PaginationAndSearchOptionsDto options)
        {
            try
            {
                var ingredientQuantities = _IngredientQuantitiesService.GetAllIngredientQuantitiesPaginated(options);

                if (ingredientQuantities != null && ingredientQuantities.Any())
                {
                    return Ok(new { status = StatusCodes.Status200OK, ingredientQuantities });
                }
                else
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = INGREDIENT.NOT_FOUND });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a all IngredientQuantities by the ingredient unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Ingredient.</param>
        /// <returns>
        /// Returns all the IngredientQuantities's informations if found.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If no IngredientQuantities is found with the ingredient ID, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("all/{id}")]
        public IActionResult GetAllIngredientQuantitiesById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var ingredientQuantities = _IngredientQuantitiesService.GetAllIngredientQuantitiesById(id);

                if (ingredientQuantities != null)
                {
                    return Ok(new { status = StatusCodes.Status200OK, ingredientQuantities });
                }
                else
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = INGREDIENT.NOT_FOUND });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a IngredientQuantities by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the IngredientQuantities.</param>
        /// <returns>
        /// Returns the IngredientQuantities's information if found.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If no IngredientQuantities is found with the specified ID, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("{id}")]
        public IActionResult GetIngredientQuantitiesById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var ingredientQuantities = _IngredientQuantitiesService.GetIngredientQuantitiesById(id);

                if (ingredientQuantities != null)
                {
                    return Ok(new { status = StatusCodes.Status200OK, ingredientQuantities });
                }
                else
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = INGREDIENT.NOT_FOUND });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Adds a new IngredientQuantities to the database.
        /// </summary>
        /// <param name="ingredientQuantities">The IngredientQuantities information to be added.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a CreatedAtAction response with the URL of the newly created IngredientQuantities if successful.
        /// If the provided IngredientQuantities data is invalid, returns a BadRequest response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpPost]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult AddIngredientQuantities([FromBody] IngredientQuantities ingredientQuantities)
        {
            try
            {
                if (ingredientQuantities == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                _IngredientQuantitiesService.AddIngredientQuantities(ingredientQuantities);

                return CreatedAtAction(nameof(GetIngredientQuantitiesById), new { id = ingredientQuantities.Id }, ingredientQuantities);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.NOT_SAVED, error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing IngredientQuantities's information in the database.
        /// </summary>
        /// <param name="id">The ID of the IngredientQuantities to be updated.</param>
        /// <param name="ingredientQuantities">The updated IngredientQuantities information.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If successful, returns a success message.
        /// If the provided ID is invalid, returns a BadRequest response.
        /// If the provided IngredientQuantities data is invalid, returns a BadRequest response.
        /// If the IngredientQuantities with the specified ID is not found, returns a NotFound response.
        /// If an error occurs during the update operation, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPut("{id}")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult UpdateIngredientQuantities(int id, [FromBody] IngredientQuantities ingredientQuantities)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                if (ingredientQuantities == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                _IngredientQuantitiesService.UpdateIngredientQuantities(id, ingredientQuantities);

                return Ok(new { status = StatusCodes.Status200OK, message = INGREDIENT.SUCCESS_UPDATING });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.ERROR_UPDATING, error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a IngredientQuantities from the database based on its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the IngredientQuantities to be deleted.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If the IngredientQuantities with the specified ID is not found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "ManagerOnly")]
        public IActionResult DeleteIngredientQuantities(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                _IngredientQuantitiesService.DeleteIngredientQuantities(id);
                return Ok(new { status = StatusCodes.Status200OK, message = INGREDIENT.SUCCESS_DELETING });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = INGREDIENT.ERROR_DELETING, error = ex.Message });
            }
        }
    }
}
