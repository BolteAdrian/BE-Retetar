using Microsoft.EntityFrameworkCore;
using Retetar.Interfaces;
using Retetar.Models;
using Retetar.Repository;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Services
{
    public class IngredientQuantitiesService
    {
        private readonly RecipeDbContext _dbContext;

        public IngredientQuantitiesService(RecipeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves a paginated list of IngredientQuantitiess based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of IngredientQuantitiess if successful.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public List<IngredientQuantities> GetAllIngredientQuantitiesPaginated(IPaginationAndSearchOptions options)
        {
            try
            {
                IQueryable<IngredientQuantities> query = (IQueryable<IngredientQuantities>)_dbContext.IngredientQuantities.AsQueryable();

                // Sorting
                if (!string.IsNullOrEmpty(options.SortField))
                {
                    // In this example, we'll use the SortOrder enum to decide whether sorting is done in ascending or descending order
                    bool isAscending = options.SortOrder == SortOrder.Ascending;
                    query = SortQuery(query, options.SortField, isAscending);
                }

                // Calculate the total number of records
                int totalItems = query.Count();

                // Pagination - calculate the start index and retrieve the desired number of elements
                int startIndex = (options.PageNumber - 1) * options.PageSize;
                query = query.Skip(startIndex).Take(options.PageSize);

                // Return the results and pagination information
                return query.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Sorts the given query based on the provided sort field and sort order.
        /// </summary>
        /// <param name="query">The query to be sorted.</param>
        /// <param name="sortField">The field to be used for sorting.</param>
        /// <param name="isAscending">True for ascending sorting, false for descending sorting.</param>
        /// <returns>
        /// Returns the sorted query if successful.
        /// If the provided sort field is not recognized, returns the unchanged query.
        /// </returns>
        private IQueryable<IngredientQuantities> SortQuery(IQueryable<IngredientQuantities> query, string sortField, bool isAscending)
        {
            switch (sortField.ToLower())
            {
                case "amount":
                    return isAscending ? query.OrderBy(g => g.Amount) : query.OrderByDescending(g => g.Amount);
                case "purchase":
                    return isAscending ? query.OrderBy(g => g.DateOfPurchase) : query.OrderByDescending(g => g.DateOfPurchase);
                case "expiring":
                    return isAscending ? query.OrderBy(g => g.ExpiringDate) : query.OrderByDescending(g => g.ExpiringDate);
                default:
                    return query; // If the sorting field does not exist or is not specified, return the unchanged query
            }
        }

        /// <summary>
        /// Retrieves all IngredientQuantities by ingredient unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the Ingredient.</param>
        /// <returns>
        /// Returns all the IngredientQuantities objects if found.
        /// If the IngredientQuantities with the specified ingredient ID is not found, throws an exception with an appropriate error message.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public async Task<List<IngredientQuantities>> GetAllIngredientQuantitiesById(int id)
        {
            try
            {
                var ingredientQuantities = await _dbContext.IngredientQuantities
                    .Where(g => g.IngredientId == id)
                    .ToListAsync();

                if (ingredientQuantities == null || ingredientQuantities.Count == 0)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id));
                }

                return ingredientQuantities;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id), ex);
            }
        }

        /// <summary>
        /// Retrieves a IngredientQuantities by its unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the IngredientQuantities.</param>
        /// <returns>
        /// Returns the IngredientQuantities object if found.
        /// If the IngredientQuantities with the specified ID is not found, throws an exception with an appropriate error message.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public IngredientQuantities GetIngredientQuantitiesById(int id)
        {
            try
            {
                var ingredientQuantities = _dbContext.IngredientQuantities.FirstOrDefault(g => g.Id == id);
                if (ingredientQuantities == null)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id));
                }
                return ingredientQuantities;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id), ex);
            }
        }

        /// <summary>
        /// Adds a new IngredientQuantities to the database.
        /// </summary>
        /// <param name="ingredientQuantities">The IngredientQuantities object to be added.</param>
        /// <exception cref="ArgumentNullException">Thrown when the IngredientQuantities object is null.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during saving the IngredientQuantities to the database.</exception>
        public void AddIngredientQuantities(IngredientQuantities ingredientQuantities)
        {
            try
            {
                _dbContext.IngredientQuantities.Add(ingredientQuantities);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(INGREDIENT.NOT_SAVED, ex);
            }
        }

        /// <summary>
        /// Updates an existing IngredientQuantities in the database.
        /// </summary>
        /// <param name="id">The ID of the IngredientQuantities to be updated.</param>
        /// <param name="ingredientQuantities">The IngredientQuantities object containing the updated data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the IngredientQuantities object is null.</exception>
        /// <exception cref="Exception">Thrown when the IngredientQuantities with the specified ID is not found or an error occurs during updating.</exception>
        public void UpdateIngredientQuantities(int id, IngredientQuantities ingredientQuantities)
        {
            try
            {
                var existingIngredientQuantities = _dbContext.IngredientQuantities.FirstOrDefault(g => g.Id == id);

                if (existingIngredientQuantities == null)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id));
                }

                existingIngredientQuantities.Amount = ingredientQuantities.Amount;
                existingIngredientQuantities.ExpiringDate = ingredientQuantities.ExpiringDate;
                existingIngredientQuantities.Unit = ingredientQuantities.Unit;
                existingIngredientQuantities.DateOfPurchase = ingredientQuantities.DateOfPurchase;

                _dbContext.SaveChanges();

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.ERROR_UPDATING, id), ex);
            }
        }

        /// <summary>
        /// Deletes a IngredientQuantities from the database based on its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the IngredientQuantities to be deleted.</param>
        /// <exception cref="Exception">Thrown when the IngredientQuantities is not found or an error occurs during deletion.</exception>
        public void DeleteIngredientQuantities(int id)
        {
            try
            {
                var ingredientQuantities = _dbContext.IngredientQuantities.FirstOrDefault(g => g.Id == id);

                if (ingredientQuantities == null)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id));
                }

                _dbContext.IngredientQuantities.Remove(ingredientQuantities);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.ERROR_DELETING, id), ex);
            }
        }
    }
}
