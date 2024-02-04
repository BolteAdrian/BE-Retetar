using Microsoft.EntityFrameworkCore;
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
        public async Task<List<Recipe>> GetAllRecipesPaginated(IPaginationAndSearchOptions options)
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

                // Include RecipeCategories and RecipeIngredients
                query = query.Include(r => r.RecipeCategories)
                             .ThenInclude(rc => rc.Category)
                             .Include(r => r.RecipeIngredients)
                             .ThenInclude(ri => ri.Ingredient);

                // Calculate the total number of records
                int totalItems = query.Count();

                // Pagination - calculate the start index and retrieve the desired number of elements
                int startIndex = (options.PageNumber - 1) * options.PageSize;
                query = query.Skip(startIndex).Take(options.PageSize);

                // Return the results and pagination information
                var recipes = await query.ToListAsync();

                // Procesați fiecare rețetă pentru a umple categoriile și ingredientele
                foreach (var recipe in recipes)
                {
                    recipe.RecipeCategories = _dbContext.RecipeCategories
                        .Where(rc => rc.RecipeId == recipe.Id)
                        .Select(rc => new RecipeCategory
                        {
                            Category = _dbContext.Category.FirstOrDefault(c => c.Id == rc.CategoryId)
                        })
                        .ToList();

                    recipe.RecipeIngredients = _dbContext.RecipeIngredients
                        .Where(ri => ri.RecipeId == recipe.Id)
                        .Select(ri => new RecipeIngredients
                        {
                            Ingredient = _dbContext.Ingredient.FirstOrDefault(i => i.Id == ri.IngredientId)
                        })
                        .ToList();
                }

                return recipes;
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
        public async Task<Recipe> GetRecipeDetails(int id)
        {
            try
            {
                var recipe = await _dbContext.Recipe
                    .Include(r => r.RecipeCategories)
                        .ThenInclude(rc => rc.Category)
                    .Include(r => r.RecipeIngredients)
                        .ThenInclude(ri => ri.Ingredient)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, id));
                }

                // Umeaza aceleasi pasi ca in GetAllRecipesPaginated pentru a umple categoriile si ingredientele
                recipe.RecipeCategories = _dbContext.RecipeCategories
                    .Where(rc => rc.RecipeId == recipe.Id)
                    .Select(rc => new RecipeCategory
                    {
                        CategoryId = rc.CategoryId,
                        Category = _dbContext.Category.FirstOrDefault(c => c.Id == rc.CategoryId)
                    })
                    .ToList();

                recipe.RecipeIngredients = _dbContext.RecipeIngredients
                    .Where(ri => ri.RecipeId == recipe.Id)
                    .Select(ri => new RecipeIngredients
                    {
                        Quantity = ri.Quantity,
                        Unit = ri.Unit,
                        Ingredient = _dbContext.Ingredient.FirstOrDefault(i => i.Id == ri.IngredientId)
                    })
                    .ToList();

                return recipe;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.NOT_FOUND, id), ex);
            }
        }

        /// <summary>
        /// Calculates the maximum number of times a Recipe can be prepared based on available ingredients.
        /// </summary>
        /// <param name="recipeId">The identifier of the Recipe to calculate the amount for.</param>
        /// <remarks>
        /// This method computes the maximum number of times the specified Recipe can be prepared
        /// considering the availability of required ingredients and their quantities in the inventory.
        /// </remarks>
        /// <returns>
        /// Returns the maximum number of times the Recipe can be prepared with available ingredients.
        /// Returns 0 if any ingredient is unavailable or expired.
        /// </returns>
        public int GetRecipeAmount(int recipeId)
        {
            try
            {
                var recipe = _dbContext.Recipe.FirstOrDefault(r => r.Id == recipeId);

                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, recipeId));
                }

                var recipeIngredients = _dbContext.RecipeIngredients.Where(ri => ri.RecipeId == recipeId).ToList();

                int maximumPossibleRecipes = int.MaxValue;

                foreach (var recipeIngredient in recipeIngredients)
                {
                    var ingredientQuantities = _dbContext.IngredientQuantities.Where(iq => iq.IngredientId == recipeIngredient.IngredientId).ToList();
                    double totalIngredientQuantity = 0;

                    foreach (var ingredientQuantity in ingredientQuantities)
                    {
                        if (ingredientQuantity != null && ingredientQuantity.ExpiringDate > DateTime.Now)
                        {
                            totalIngredientQuantity += ingredientQuantity.Amount;
                        }
                    }

                    if (totalIngredientQuantity == 0)
                    {
                        return 0; // Return 0 if any ingredient is unavailable
                    }

                    int possibleRecipesWithIngredient = (int)(totalIngredientQuantity / recipeIngredient.Quantity);
                    maximumPossibleRecipes = Math.Min(maximumPossibleRecipes, possibleRecipesWithIngredient);
                }

                return maximumPossibleRecipes;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.NOT_FOUND, recipeId), ex);
            }
        }

        /// <summary>
        /// Checks if the requested quantity of a recipe can be prepared based on available ingredient quantities.
        /// If possible, reduces the ingredient quantities in the database accordingly.
        /// </summary>
        /// <param name="recipeId">The ID of the recipe to check.</param>
        /// <param name="desiredQuantity">The desired quantity of the recipe to be prepared.</param>
        /// <returns>
        /// A tuple containing information:
        ///   - First value (bool): Indicates if the requested quantity can be prepared.
        ///   - Second value (double): The actual available quantity of the ingredient required for the recipe.
        ///   - Third value (string): Name of the ingredient that is insufficient in quantity, if any.
        /// </returns>
        /// <exception cref="Exception">Thrown when an error occurs during the quantity check process.</exception>
        public (bool, List<double>, List<string>) SubmitRecipeQuantity(int recipeId, double cantitateDorita)
        {
            try
            {
                var recipe = _dbContext.Recipe.FirstOrDefault(r => r.Id == recipeId);

                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, recipeId));
                }

                var recipeIngredients = _dbContext.RecipeIngredients.Where(ri => ri.RecipeId == recipeId).ToList();

                var ingredientLipsa = new List<string>();
                var cantitateLipsa = new List<double>();

                foreach (var recipeIngredient in recipeIngredients)
                {
                    var cantitateDeRedus = recipeIngredient.Quantity* cantitateDorita;
                    var ingredientQuantities = _dbContext.IngredientQuantities
                        .Where(iq => iq.IngredientId == recipeIngredient.IngredientId && iq.ExpiringDate > DateTime.Now)
                        .OrderBy(iq => iq.Id) // Sortează după ID pentru a reduce cel mai vechi disponibil
                        .ToList();

                    double totalIngredientQuantity = ingredientQuantities.Sum(iq => iq.Amount);
                    var unit = ingredientQuantities.FirstOrDefault()?.Unit ?? "";
                    var cantitateMaxima = (int)(totalIngredientQuantity / cantitateDeRedus);

                    if (totalIngredientQuantity == 0)
                    {
                        var ingredient = _dbContext.Ingredient.FirstOrDefault(r => r.Id == recipeIngredient.IngredientId);
                        if (ingredient != null)
                        {
                            ingredientLipsa.Add( unit + " of " + ingredient.Name );
                            cantitateLipsa.Add(cantitateDeRedus);
                        }
                    }
                    else if (cantitateMaxima < cantitateDorita)
                    {
                        var ingredient = _dbContext.Ingredient.FirstOrDefault(r => r.Id == recipeIngredient.IngredientId);
                        if (ingredient != null)
                        {
                            ingredientLipsa.Add(unit + " of " + ingredient.Name);
                            cantitateLipsa.Add(cantitateDeRedus - totalIngredientQuantity);
                        }
                    }
                    else
                    {

                        // Actualizează cantitățile disponibile ale ingredientelor din baza de date
                        foreach (var ingredientQuantity in ingredientQuantities)
                        {
                            if (cantitateDeRedus > 0 && ingredientQuantity.Amount > 0)
                            {

                                if (cantitateDeRedus > ingredientQuantity.Amount)
                                {
                                    cantitateDeRedus -= ingredientQuantity.Amount;
                                    _dbContext.IngredientQuantities.Remove(ingredientQuantity);
                                }
                                else
                                {
                                    ingredientQuantity.Amount -= cantitateDeRedus;
                                    cantitateDeRedus = 0;
                                    if (ingredientQuantity.Amount == 0)
                                    {
                                        _dbContext.IngredientQuantities.Remove(ingredientQuantity);
                                    }
                                    else
                                    {
                                        _dbContext.IngredientQuantities.Update(ingredientQuantity);
                                    }
                                }

                            }
                        }
                    }
                }

                if (cantitateLipsa.Count() == 0 )
                {
                    _dbContext.SaveChanges();
                }

                return ((ingredientLipsa.Count()==0?true:false), cantitateLipsa, ingredientLipsa);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.NOT_FOUND, recipeId), ex);
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
                            Unit = ingredient.Unit,
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
        public RecipeEditor UpdateRecipe(int id, RecipeEditor updatedRecipe)
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
                existingRecipe.ShortDescription = updatedRecipe.Recipe.ShortDescription;
                existingRecipe.CookingInstructions = updatedRecipe.Recipe.CookingInstructions;
                existingRecipe.Picture = updatedRecipe.Recipe.Picture;

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
                            Quantity = ingredient.Quantity,
                            Unit = ingredient.Unit,
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
                        if (existingCategory.CategoryId != null && !updatedRecipe.Categories.Contains((int)existingCategory.CategoryId))
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

                return updatedRecipe;
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
        public bool DeleteRecipe(int recipeId)
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
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.ERROR_DELETING, recipeId), ex);
            }
        }
    }
}
