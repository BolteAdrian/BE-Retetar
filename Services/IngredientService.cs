using Microsoft.EntityFrameworkCore;
using Retetar.DataModels;
using Retetar.Models;
using Retetar.Repository;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Services
{
    public class IngredientService
    {
        private readonly RecipeDbContext _dbContext;

        public IngredientService(RecipeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves a paginated list of Ingredients based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of Ingredients if successful.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public List<Ingredient> GetAllIngredientsPaginated(PaginationAndSearchOptionsDto options)
        {
            try
            {
                IQueryable<Ingredient> query = _dbContext.Ingredient
               .Include(i => i.Category); // Include Category navigation property

                // Apply search filters
                if (!string.IsNullOrEmpty(options.SearchTerm) && options.SearchFields != null)
                {
                    string searchTermLower = options.SearchTerm.ToLower();
                    query = query.Where(g =>
                        options.SearchFields.Any(f => g.Name.ToLower().Contains(searchTermLower) ||
                                                      g.Category!.Name.ToLower().Contains(searchTermLower))
                    );
                }

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
        private IQueryable<Ingredient> SortQuery(IQueryable<Ingredient> query, string sortField, bool isAscending)
        {
            switch (sortField.ToLower())
            {
                case "name":
                    return isAscending ? query.OrderBy(g => g.Name) : query.OrderByDescending(g => g.Name);
                case "category":
                    return isAscending ? query.OrderBy(g => g.Category!.Name) : query.OrderByDescending(g => g.Name);
                default:
                    return query; // If the sorting field does not exist or is not specified, return the unchanged query
            }
        }

        /// <summary>
        /// Retrieves a list of all Ingredients.
        /// </summary>
        /// <returns>
        /// Returns a list of Ingredients if successful.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public IQueryable<Ingredient> GetAllIngredients()
        {
            try
            {
                IQueryable<Ingredient> ingredients = _dbContext.Ingredient
                    .Include(i => i.Category);

                if (ingredients == null)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND));
                }
                return ingredients;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.NOT_FOUND), ex);
            }
        }

        /// <summary>
        /// Retrieves a list of all Ingredients by category.
        /// </summary>
        /// <param name="id">The unique identifier of the Category.</param>
        /// <returns>
        /// Returns a list of Ingredients if successful.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public IQueryable<Ingredient> GetAllIngredientsByCategory(int id)
        {
            try
            {
                IQueryable<Ingredient> ingredients = _dbContext.Ingredient.Where(i => i.CategoryId == id);

                if (ingredients == null)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND));
                }
                return ingredients;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.NOT_FOUND), ex);
            }
        }

        /// <summary>
        /// Retrieves a Ingredient by its unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the Ingredient.</param>
        /// <returns>
        /// Returns the Ingredient object if found.
        /// If the Ingredient with the specified ID is not found, throws an exception with an appropriate error message.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public Ingredient GetIngredientById(int id)
        {
            try
            {
                var ingredient = _dbContext.Ingredient.FirstOrDefault(g => g.Id == id);
                if (ingredient == null)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id));
                }
                return ingredient;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id), ex);
            }
        }

        /// <summary>
        /// Adds a new Ingredient to the database.
        /// </summary>
        /// <param name="ingredient">The Ingredient object to be added.</param>
        /// <exception cref="ArgumentNullException">Thrown when the Ingredient object is null.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during saving the Ingredient to the database.</exception>
        public void AddIngredient(Ingredient ingredient)
        {
            try
            {
                _dbContext.Ingredient.Add(ingredient);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(INGREDIENT.NOT_SAVED, ex);
            }
        }

        /// <summary>
        /// Updates an existing Ingredient in the database.
        /// </summary>
        /// <param name="id">The ID of the Ingredient to be updated.</param>
        /// <param name="ingredient">The Ingredient object containing the updated data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the Ingredient object is null.</exception>
        /// <exception cref="Exception">Thrown when the Ingredient with the specified ID is not found or an error occurs during updating.</exception>
        public void UpdateIngredient(int id, Ingredient ingredient)
        {
            try
            {
                var existingIngredient = _dbContext.Ingredient.FirstOrDefault(g => g.Id == id);

                if (existingIngredient == null)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id));
                }

                existingIngredient.Name = ingredient.Name;
                existingIngredient.Description = ingredient.Description;
                existingIngredient.Picture = ingredient.Picture;
                existingIngredient.CategoryId = ingredient.CategoryId;

                _dbContext.SaveChanges();

            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.ERROR_UPDATING, id), ex);
            }
        }

        /// <summary>
        /// Deletes a Ingredient from the database based on its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the Ingredient to be deleted.</param>
        /// <exception cref="Exception">Thrown when the Ingredient is not found or an error occurs during deletion.</exception>
        public void DeleteIngredient(int id)
        {
            try
            {
                var ingredient = _dbContext.Ingredient.FirstOrDefault(g => g.Id == id);

                if (ingredient == null)
                {
                    throw new Exception(string.Format(INGREDIENT.NOT_FOUND, id));
                }

                var IngredientQuantities = _dbContext.IngredientQuantities.Where(g => g.IngredientId == id).ToList();
                _dbContext.IngredientQuantities.RemoveRange(IngredientQuantities);

                var recipeIngredients = _dbContext.RecipeIngredients.Where(rc => rc.IngredientId == id).ToList();
                _dbContext.RecipeIngredients.RemoveRange(recipeIngredients);

                _dbContext.Ingredient.Remove(ingredient);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(INGREDIENT.ERROR_DELETING, id), ex);
            }
        }
    }
}
