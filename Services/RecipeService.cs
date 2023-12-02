using Retetar.Interfaces;
using Retetar.Models;
using Retetar.Repository;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Services
{
    public class RecipeService
    {

        private readonly RecipeDbContext _dbContext;

        public RecipeService(RecipeDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Retrieves a paginated list of Recipes based on the provided search and pagination options.
        /// </summary>
        /// <param name="options">The pagination and search options.</param>
        /// <returns>
        /// Returns a paginated list of Recipes if successful.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public List<Recipe> GetAllRecipesPaginated(IPaginationAndSearchOptions options)
        {
            try
            {
                IQueryable<Recipe> query = _dbContext.Recipe.AsQueryable();

                // Apply search filters
                if (!string.IsNullOrEmpty(options.SearchTerm) && options.SearchFields != null)
                {
                    string searchTermLower = options.SearchTerm.ToLower();
                    query = query.Where(g =>
                        options.SearchFields.Any(f => f != null && g.Name != null && g.Name.ToLower().Contains(searchTermLower))
                    );
                }

                // Sorting
                if (!string.IsNullOrEmpty(options.SortField))
                {
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
        private IQueryable<Recipe> SortQuery(IQueryable<Recipe> query, string sortField, bool isAscending)
        {
            switch (sortField.ToLower())
            {
                case "name":
                    return isAscending ? query.OrderBy(g => g.Name) : query.OrderByDescending(g => g.Name);
                default:
                    return query; // If the sorting field does not exist or is not specified, return the unchanged query
            }
        }

        /// <summary>
        /// Retrieves Recipe details by its unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the Recipe.</param>
        /// <returns>
        /// Returns the Recipe object if found.
        /// If the Recipe with the specified ID is not found, throws an exception with an appropriate error message.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public RecipeDetails GetRecipeDetails(int id)
        {
            try
            {
                var recipe = _dbContext.Recipe.FirstOrDefault(r => r.Id == id);

                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, id));
                }

                var RecipeCategories = _dbContext.RecipeCategories.Where(rc => rc.RecipeId == id).ToList();
                var RecipeIngredients = _dbContext.RecipeIngredients.Where(ri => ri.RecipeId == id).ToList();

                var Categories = _dbContext.RecipeCategories.Where(i => i.RecipeId == id).ToList();

                foreach (var Category in Categories)
                {
                    var categoryEntity = _dbContext.Category.FirstOrDefault(c => c.Id == Category.CategoryId);
                    if (categoryEntity != null)
                    {
                        Category.Category= categoryEntity;
                    }
                }

                var RecipeIngredientsList = _dbContext.RecipeIngredients.Where(i => i.RecipeId == id).ToList();

                foreach (var RecipeIngredient in RecipeIngredientsList)
                {
                    var ingredientEntity = _dbContext.Ingredient.FirstOrDefault(i => i.Id == RecipeIngredient.IngredientId);
                    if (ingredientEntity != null)
                    {
                        RecipeIngredient.Ingredient = ingredientEntity;
                    }
                }

                var recipeDetails = new RecipeDetails
                {
                    Recipe = Recipe,
                    Ingredients = RecipeIngredientsList,
                    Categories = Categories
                };

                return recipeDetails;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.NOT_FOUND, id), ex);
            }
        }

        /// <summary>
        /// Adds a new Recipe to the database.
        /// </summary>
        /// <param name="recipe">The RecipeEditor object to be added.</param>
        /// <exception cref="ArgumentNullException">Thrown when the Recipe object is null.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during saving the Recipe to the database.</exception>
        public void AddRecipe(RecipeEditor recipe)
        {
            try
            {
                if (recipe == null)
                {
                    throw new ArgumentNullException(nameof(recipe));
                }

                _dbContext.Recipe.Add(recipe.Recipe);
                _dbContext.SaveChanges();

                if (recipe.Ingredients != null && recipe.Ingredients.Any())
                {
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        var recipeIngredient = new RecipeIngredients
                        {
                            RecipeId = recipe.Recipe.Id,
                            IngredientId = ingredient.IngredientId,
                            Quantity = ingredient.Quantity,
                        };

                        _dbContext.RecipeIngredients.Add(recipeIngredient);
                    }

                    _dbContext.SaveChanges();
                }

                if (recipe.Categories != null && recipe.Categories.Any())
                {
                    foreach (var category in recipe.Categories)
                    {
                        var recipeCategory = new RecipeCategory
                        {
                            RecipeId = recipe.Recipe.Id,
                            CategoryId = category,
                        };

                        _dbContext.RecipeCategories.Add(recipeCategory);
                    }

                    _dbContext.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(RECIPE.NOT_SAVED, ex);
            }
        }

        /// <summary>
        /// Updates an existing Recipe in the database.
        /// </summary>
        /// <param name="id">The ID of the Recipe to be updated.</param>
        /// <param name="updatedRecipe">The Recipe object containing the updated data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the Recipe object is null.</exception>
        /// <exception cref="Exception">Thrown when the Recipe with the specified ID is not found or an error occurs during updating.</exception>
        public void UpdateRecipe(int id, RecipeEditor updatedRecipe)
        {
            try
            {
                if (updatedRecipe == null)
                {
                    throw new ArgumentNullException(nameof(updatedRecipe));
                }

                var existingRecipe = _dbContext.Recipe.FirstOrDefault(r => r.Id == id);

                if (existingRecipe == null)
                {
                    throw new ArgumentNullException(nameof(existingRecipe));
                }

                existingRecipe.Name = updatedRecipe.Recipe.Name;
                existingRecipe.Description = updatedRecipe.Recipe.Description;
                existingRecipe.CookingInstructions = updatedRecipe.Recipe.CookingInstructions;

                if (updatedRecipe.Ingredients != null)
                {
                    var existingRecipeIngredients = _dbContext.RecipeIngredients
                        .Where(ri => ri.RecipeId == id)
                        .ToList();

                    foreach (var existingIngredient in existingRecipeIngredients)
                    {
                        var updatedIngredient = updatedRecipe.Ingredients.FirstOrDefault(ui => ui.Id == existingIngredient.Id);

                        if (updatedIngredient == null)
                        {
                            _dbContext.RecipeIngredients.Remove(existingIngredient);
                        }
                        else
                        {
                            existingIngredient.Quantity = updatedIngredient.Quantity;
                        }
                    }

                    foreach (var ingredient in updatedRecipe.Ingredients.Where(ui => !existingRecipeIngredients.Any(ei => ei.Id == ui.Id)))
                    {
                        _dbContext.RecipeIngredients.Add(new RecipeIngredients
                        {
                            RecipeId = existingRecipe.Id,
                            IngredientId = ingredient.IngredientId,
                            Quantity = ingredient.Quantity
                        });
                    }
                }

                if (updatedRecipe.Categories != null)
                {
                    var existingRecipeCategories = _dbContext.RecipeCategories
                        .Where(rc => rc.RecipeId == id)
                        .ToList();

                    foreach (var existingCategory in existingRecipeCategories)
                    {
                        if (!updatedRecipe.Categories.Contains(existingCategory.CategoryId))
                        {
                            _dbContext.RecipeCategories.Remove(existingCategory);
                        }
                    }

                    foreach (var categoryId in updatedRecipe.Categories.Where(uc => !existingRecipeCategories.Any(ec => ec.CategoryId == uc)))
                    {
                        _dbContext.RecipeCategories.Add(new RecipeCategory
                        {
                            RecipeId = existingRecipe.Id,
                            CategoryId = categoryId
                        });
                    }
                }

                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.ERROR_UPDATING, id), ex);
            }
        }

        /// <summary>
        /// Deletes a Recipe from the database based on its unique identifier.
        /// </summary>
        /// <param name="recipeId">The unique identifier of the Recipe to be deleted.</param>
        /// <exception cref="Exception">Thrown when the Recipe is not found or an error occurs during deletion.</exception>
        public void DeleteRecipe(int recipeId)
        {
            try
            {
                var existingRecipe = _dbContext.Recipe.FirstOrDefault(r => r.Id == recipeId);

                if (existingRecipe == null)
                {
                    throw new ArgumentNullException(nameof(existingRecipe));
                }

                var recipeCategories = _dbContext.RecipeCategories.Where(rc => rc.RecipeId == recipeId).ToList();
                _dbContext.RecipeCategories.RemoveRange(recipeCategories);

                var recipeIngredients = _dbContext.RecipeIngredients.Where(ri => ri.RecipeId == recipeId).ToList();
                _dbContext.RecipeIngredients.RemoveRange(recipeIngredients);

                _dbContext.Recipe.Remove(existingRecipe);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.ERROR_DELETING, recipeId), ex);
            }
        }
    }
}
