﻿using Microsoft.EntityFrameworkCore;
using Retetar.DataModels;
using Retetar.Models;
using Retetar.Repository;
using static Retetar.MLModel;
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
        public async Task<List<Recipe>> GetAllRecipesPaginated(PaginationAndSearchOptionsDto options)
        {
            try
            {
                IQueryable<Recipe> query = _dbContext.Recipe
                    .Select(r => new Recipe
                    {
                        Id = r.Id,
                        Name = r.Name,
                        ShortDescription = r.ShortDescription,
                        Picture = r.Picture,
                        RecipeCategories = r.RecipeCategories,
                        RecipeIngredients = r.RecipeIngredients
                    });

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
                query = query.Include(r => r.RecipeCategories!)
                             .ThenInclude(rc => rc.Category)
                             .Include(r => r.RecipeIngredients!)
                             .ThenInclude(ri => ri.Ingredient);

                // Calculate the total number of records
                int totalItems = query.Count();

                // Pagination - calculate the start index and retrieve the desired number of elements
                int startIndex = (options.PageNumber - 1) * options.PageSize;
                query = query.Skip(startIndex).Take(options.PageSize);

                // Return the results and pagination information
                var recipes = await query.ToListAsync();

                // Process each recipe to complete the categories and ingredients
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
        /// Retrieves a paginated list of Recipes based on the provided category.
        /// </summary>
        /// <param name="id">The unique identifier of the category.</param>
        /// <returns>
        /// Returns a list of Recipes if successful.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public async Task<List<Recipe>> GetAllRecipesByCategory(int id)
        {
            try
            {
                var recipes = await _dbContext.Recipe
                    .Include(r => r.RecipeCategories!)
                        .ThenInclude(rc => rc.Category)
                    .Where(r => r.RecipeCategories!.Any(rc => rc.CategoryId == id))
                    .ToListAsync();

                // Process each recipe to populate categories and ingredients
                foreach (var recipe in recipes)
                {
                    recipe.RecipeCategories = _dbContext.RecipeCategories
                        .Where(rc => rc.RecipeId == recipe.Id)
                        .Select(rc => new RecipeCategory
                        {
                            Category = _dbContext.Category.FirstOrDefault(c => c.Id == rc.CategoryId)
                        })
                        .ToList()!;
                }

                return recipes;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
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
                    .Include(r => r.RecipeCategories!)
                        .ThenInclude(rc => rc.Category)
                    .Include(r => r.RecipeIngredients!)
                        .ThenInclude(ri => ri.Ingredient)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, id));
                }

                // Follow the same steps as in Get AllRecipes Paginated to fill in the categories and ingredients
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
        /// Retrieves Recipe details by its unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the Recipe.</param>
        /// <returns>
        /// Returns the Recipe object if found.
        /// If the Recipe with the specified ID is not found, throws an exception with an appropriate error message.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public async Task<List<PreparedRecipeDto>> GetPreparedRecipeHistory()
        {
            try
            {
                var preparedRecipes = await _dbContext.PreparedRecipeHistory
                    .Select(r => new PreparedRecipeDto
                    {
                        RecipeName = r.Recipe.Name,
                        PreparedRecipe = r
                    })
                    .ToListAsync();

                return preparedRecipes;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Retrieves the historical data of prepared recipes and generates predictions for the next year.
        /// </summary>
        /// <remarks>
        /// This method fetches the historical data of prepared recipes from the database,
        /// including details such as the amount prepared and the date of preparation.
        /// It then uses an ML.NET model to predict the amount that will be prepared in the next year based on historical data.
        /// </remarks>
        /// <returns>
        /// Returns a list of prepared recipes along with their predicted amounts for the next year.
        /// If there is no historical data available or any error occurs during processing, appropriate exceptions are thrown.
        /// </returns>
        public async Task<IEnumerable<object>> GetPreparedRecipesWithPredictions()
        {
            try
            {
                // Obțineți anul curent
                int currentYear = DateTime.Now.Year;

                // Obține datele istorice din baza de date
                var preparedRecipes = await _dbContext.PreparedRecipeHistory
                    .Include(r => r.Recipe)
                    .Select(r => new
                    {
                        Id = r.Id,
                        Amount = r.Amount,
                        RecipeId = r.RecipeId,
                        RecipeName = r.Recipe.Name,
                        PreparationDate = new DateTime(currentYear, r.PreparationDate.Month, r.PreparationDate.Day)
                    })
                    .ToListAsync();

                // Generați predicții pentru anul următor
                var predictedData = preparedRecipes.Select(data => new
                {
                    data.Id,
                    data.Amount,
                    data.RecipeId,
                    RecipeName = data.RecipeName,
                    data.PreparationDate,
                    PredictedAmount = MLModel.Predict(new ModelInput
                    {
                        Id = data.Id,
                        Amount = data.Amount,
                        RecipeId = data.RecipeId,
                        PreparationDate = data.PreparationDate
                    }).Score
                }).ToList();

                return predictedData;
            }
            catch (Exception ex)
            {
                throw new Exception("Error occurred while generating predictions.", ex);
            }
        }

        /// <summary>
        /// Calculates the maximum number of times a Recipe can be prepared based on available ingredients and their quantities.
        /// </summary>
        /// <param name="recipeId">The identifier of the Recipe to calculate the amount for.</param>
        /// <remarks>
        /// This method computes the maximum number of times the specified Recipe can be prepared
        /// considering the availability and quantity of required ingredients in the inventory.
        /// </remarks>
        /// <returns>
        /// Returns an object containing information about the Recipe amount including:
        /// - MaximumPossibleRecipes: The maximum number of times the Recipe can be prepared with available ingredients.
        /// - PriceAllRecipes: The total price of all recipes that can be prepared with available ingredients.
        /// - PriceUnitRecipe: The average price per recipe considering the available ingredients.
        /// Returns 0 if any ingredient is unavailable or expired.
        /// </returns>
        public async Task<RecipeAmountDto> GetRecipeAmount(int recipeId)
        {
            try
            {
                var recipe = await _dbContext.Recipe.FirstOrDefaultAsync(r => r.Id == recipeId);
                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, recipeId));
                }

                var recipeIngredients = await _dbContext.RecipeIngredients
                    .Where(ri => ri.RecipeId == recipeId)
                    .ToListAsync();

                var ingredientIds = recipeIngredients.Select(ri => ri.IngredientId).ToList();

                var ingredientQuantities = await _dbContext.IngredientQuantities
                    .Where(iq => ingredientIds.Contains(iq.IngredientId) && iq.UsedDate == null && iq.ExpiringDate > DateTime.Now)
                    .ToListAsync();

                int maximumPossibleRecipes = int.MaxValue;
                double totalIngredientsPrice = 0;

                var currencyConverter = new CurrencyConverter();

                foreach (var recipeIngredient in recipeIngredients)
                {
                    var relevantQuantities = ingredientQuantities.Where(iq => iq.IngredientId == recipeIngredient.IngredientId).ToList();

                    double totalIngredientQuantity = 0;
                    double totalIngredientPrice = 0;

                    foreach (var ingredientQuantity in relevantQuantities)
                    {
                        double quantityInStandardUnit = ingredientQuantity.Unit == "g" || ingredientQuantity.Unit == "ml"
                            ? ingredientQuantity.Amount / 1000
                            : ingredientQuantity.Amount;

                        totalIngredientQuantity += quantityInStandardUnit;

                        double ingredientPriceInCurrency = ingredientQuantity.Currency == "RON"
                            ? ingredientQuantity.Price
                            : await currencyConverter.ConvertCurrency(ingredientQuantity.Price, ingredientQuantity.Currency);

                        totalIngredientPrice += ingredientPriceInCurrency;
                    }

                    double recipeIngredientQuantityInStandardUnit = recipeIngredient.Unit == "g" || recipeIngredient.Unit == "ml"
                        ? recipeIngredient.Quantity / 1000
                        : recipeIngredient.Quantity;

                    int possibleRecipesWithIngredient = (int)(totalIngredientQuantity / recipeIngredientQuantityInStandardUnit);
                    maximumPossibleRecipes = Math.Min(maximumPossibleRecipes, possibleRecipesWithIngredient);

                    if (maximumPossibleRecipes != 0)
                    {
                        totalIngredientsPrice += totalIngredientPrice;
                    }
                }

                return new RecipeAmountDto
                {
                    MaximumPossibleRecipes = recipeIngredients.Count() == 0 ? 0: maximumPossibleRecipes,
                    PriceAllRecipes = maximumPossibleRecipes != 0 ? Math.Round(totalIngredientsPrice, 2) : 0,
                    PriceUnitRecipe = maximumPossibleRecipes != 0 ? Math.Round(totalIngredientsPrice / maximumPossibleRecipes, 2) : 0
                };
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
        public (bool, List<MissingIngredientsDto>) SubmitRecipeQuantity(int recipeId, int cantitateDorita)
        {
            try
            {
                var recipe = _dbContext.Recipe.FirstOrDefault(r => r.Id == recipeId);

                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, recipeId));
                }

                var recipeIngredients = _dbContext.RecipeIngredients.Where(ri => ri.RecipeId == recipeId).ToList();

                var missingIngredients = new List<MissingIngredientsDto>();

                foreach (var recipeIngredient in recipeIngredients)
                {
                    // We convert the desired quantity into kilograms or liters, depending on the unit of measure specified for the ingredient
                    double cantitateDeRedus;
                    switch (recipeIngredient.Unit.ToLower())
                    {
                        case "g":
                        case "ml":
                            cantitateDeRedus = (recipeIngredient.Quantity / 1000) * cantitateDorita; // Converting quantity to standard unit if necessary
                            break;
                        default:
                            cantitateDeRedus = recipeIngredient.Quantity * cantitateDorita;
                            break;
                    }

                    var ingredientQuantities = _dbContext.IngredientQuantities
                        .Where(iq => iq.IngredientId == recipeIngredient.IngredientId && iq.ExpiringDate > DateTime.Now && iq.UsedDate == null)
                        .OrderBy(iq => iq.Id) // Sort by ID to reduce oldest available
                        .ToList();

                    // We calculate the total amount of available quantities, taking into account the unit of measure
                    double totalIngredientQuantity = 0;
                    foreach (var ingredientQuantity in ingredientQuantities)
                    {
                        // We convert the available quantity into the same unit as the desired quantity
                        double availableAmountInDefaultUnit = ingredientQuantity.Amount;
                        switch (ingredientQuantity.Unit.ToLower())
                        {
                            case "g":
                                availableAmountInDefaultUnit /= 1000; // We convert grams to kilograms
                                break;
                            case "ml":
                                availableAmountInDefaultUnit /= 1000; // Convert milliliters to liters
                                break;
                        }
                        totalIngredientQuantity += availableAmountInDefaultUnit;
                    }

                    var unit = ingredientQuantities.FirstOrDefault()?.Unit ?? "";
                    var cantitateMaxima = (int)(totalIngredientQuantity / cantitateDeRedus);

                    if(unit == "g")
                    {
                        unit = "Kg";
                    }

                    if (unit == "ml")
                    {
                        unit = "L";
                    }

                    if (totalIngredientQuantity == 0)
                    {
                        var ingredient = _dbContext.Ingredient.FirstOrDefault(r => r.Id == recipeIngredient.IngredientId);
                        if (ingredient != null)
                        {
                            missingIngredients.Add(new MissingIngredientsDto
                            {
                                Unit = unit,
                                Quantity = Math.Round(cantitateDeRedus, 2),
                                Name = ingredient.Name
                            });
                        }
                    }
                    else if (cantitateMaxima < cantitateDorita)
                    {
                        var ingredient = _dbContext.Ingredient.FirstOrDefault(r => r.Id == recipeIngredient.IngredientId);
                        if (ingredient != null)
                        {
                            missingIngredients.Add(new MissingIngredientsDto
                            {
                                Unit = unit,
                                Quantity = Math.Round(cantitateDeRedus - totalIngredientQuantity, 2),
                                Name = ingredient.Name
                            });
                        }
                    }
                    else
                    {

                        // Updates the available quantities of ingredients in the database
                        foreach (var ingredientQuantity in ingredientQuantities)
                        {
                            if (cantitateDeRedus > 0 && ingredientQuantity.Amount > 0)
                            {

                                if (cantitateDeRedus > ingredientQuantity.Amount)
                                {
                                    cantitateDeRedus -= ingredientQuantity.Amount;
                                    ingredientQuantity.UsedDate = DateTime.Now;
                                    _dbContext.IngredientQuantities.Update(ingredientQuantity);
                                }
                                else
                                {
                                    ingredientQuantity.Amount -= cantitateDeRedus;
                                    cantitateDeRedus = 0;
                                    if (ingredientQuantity.Amount == 0)
                                    {
                                        ingredientQuantity.UsedDate = DateTime.Now;
                                        _dbContext.IngredientQuantities.Update(ingredientQuantity);
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

                if (missingIngredients.Count() == 0 )
                {
                    PreparedRecipeHistory preparedRecipe = new PreparedRecipeHistory
                    {
                        Amount = cantitateDorita,
                        RecipeId = recipeId,
                        PreparationDate = DateTime.Now
                    };
                    _dbContext.PreparedRecipeHistory.AddAsync(preparedRecipe);
                    _dbContext.SaveChanges();
                }

                return ((missingIngredients.Count() == 0 ? true : false), missingIngredients);
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
        public void AddRecipe(RecipeEditorDto recipe)
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
        public RecipeEditorDto UpdateRecipe(int id, RecipeEditorDto updatedRecipe)
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
                existingRecipe.Picture = updatedRecipe.Recipe.Picture != null ? updatedRecipe.Recipe.Picture : existingRecipe.Picture;

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
