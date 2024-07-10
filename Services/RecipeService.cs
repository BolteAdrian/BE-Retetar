using Microsoft.EntityFrameworkCore;
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
                // Get the current year
                int currentYear = DateTime.Now.Year;

                // Get the historical data from the database
                var preparedRecipes = await _dbContext.PreparedRecipeHistory
                    .Include(r => r.Recipe)
                    .Select(r => new
                    {
                        r.Id,
                        r.Amount,
                        r.RecipeId,
                        RecipeName = r.Recipe.Name,
                        PreparationDate = new DateTime(currentYear, r.PreparationDate.Month, r.PreparationDate.Day)
                    })
                    .ToListAsync();

                // Generate predictions for the next year
                var predictedData = preparedRecipes.Select(data => new
                {
                    data.Id,
                    data.Amount,
                    data.RecipeId,
                    data.RecipeName,
                    data.PreparationDate,
                    PredictedAmount = Predict(new ModelInput
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
        /// Calculates the maximum number of recipes that can be prepared based on available ingredient quantities.
        /// Also calculates the total cost of ingredients required for the calculated maximum recipes.
        /// </summary>
        /// <param name="recipeId">The ID of the recipe for which to calculate the maximum possible recipes.</param>
        /// <returns>
        /// A DTO containing:
        ///   - MaximumPossibleRecipes (int): The maximum number of recipes that can be prepared.
        ///   - PriceAllRecipes (double): The total cost of all ingredients required for the maximum possible recipes.
        ///   - PriceUnitRecipe (double): The cost per unit recipe based on the calculated maximum possible recipes.
        /// </returns>
        /// <exception cref="Exception">Thrown when the specified recipe ID does not exist in the database or an error occurs during the calculation.</exception>
        public async Task<RecipeAmountDto> GetRecipeAmount(int recipeId)
        {
            try
            {
                // Fetch the recipe by ID
                var recipe = await _dbContext.Recipe.FirstOrDefaultAsync(r => r.Id == recipeId);
                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, recipeId));
                }

                // Fetch the ingredients associated with the recipe
                var recipeIngredients = await _dbContext.RecipeIngredients
                    .Where(ri => ri.RecipeId == recipeId)
                    .ToListAsync();

                // Extract ingredient IDs from the recipe ingredients
                var ingredientIds = recipeIngredients.Select(ri => ri.IngredientId).ToList();

                // Fetch the available quantities for the ingredients
                var availableIngredientQuantities = await _dbContext.IngredientQuantities
                    .Where(iq => ingredientIds.Contains(iq.IngredientId) && iq.UsedDate == null && iq.ExpiringDate > DateTime.Now)
                    .ToListAsync();

                double totalIngredientsCost = 0;
                double minTotalAmount = double.MaxValue;
                var currencyConverter = new CurrencyConverter();

                // Calculate the minimum total amount for each ingredient
                foreach (var recipeIngredient in recipeIngredients)
                {
                    var relevantIngredientQuantities = availableIngredientQuantities
                        .Where(iq => iq.IngredientId == recipeIngredient.IngredientId)
                        .ToList();

                    // Convert recipe ingredient quantity to standard unit if needed
                    double recipeIngredientQuantityStandardUnit =
                        recipeIngredient.Unit == "g" || recipeIngredient.Unit == "ml"
                        ? recipeIngredient.Quantity / 1000
                        : recipeIngredient.Quantity;

                    double totalQuantityForIngredient = 0;
                    foreach (var ingredientQuantity in relevantIngredientQuantities)
                    {
                        totalQuantityForIngredient += ingredientQuantity.Unit == "g" || ingredientQuantity.Unit == "ml"
                            ? ingredientQuantity.Amount / 1000
                            : ingredientQuantity.Amount;
                    }

                    totalQuantityForIngredient = (int)totalQuantityForIngredient / recipeIngredientQuantityStandardUnit;

                    // Update the minimum total amount based on the current ingredient
                    minTotalAmount = Math.Min(minTotalAmount, totalQuantityForIngredient);
                }

                foreach (var recipeIngredient in recipeIngredients)
                {
                    var relevantIngredientQuantities = availableIngredientQuantities
                        .Where(iq => iq.IngredientId == recipeIngredient.IngredientId)
                        .ToList();

                    double ingredientTotalCost = 0;
                    double remainingQuantity = 0;
                    double remainingCost = 0;
                    double possibleRecipesForIngredient = 0;

                    // Convert recipe ingredient quantity to standard unit if needed
                    double recipeIngredientQuantityStandardUnit =
                        recipeIngredient.Unit == "g" || recipeIngredient.Unit == "ml"
                        ? recipeIngredient.Quantity / 1000
                        : recipeIngredient.Quantity;

                    // Calculate the possible number of recipes that can be made with the available quantities
                    foreach (var ingredientQuantity in relevantIngredientQuantities)
                    {
                        double ingredientQuantityStandardUnit =
                            ingredientQuantity.Unit == "g" || ingredientQuantity.Unit == "ml"
                            ? ingredientQuantity.Amount / 1000
                            : ingredientQuantity.Amount;

                        double ingredientPriceInTargetCurrency = ingredientQuantity.Currency == "RON"
                            ? ingredientQuantity.Price
                            : await currencyConverter.ConvertCurrency(ingredientQuantity.Price, ingredientQuantity.Currency);


                        ingredientQuantityStandardUnit += remainingQuantity; // Add the remaining quantity from the previous iteration

                        // Check if the total quantity exceeds minTotalAmount
                        if (possibleRecipesForIngredient == minTotalAmount)
                        {
                            break;
                        }

                        if (recipeIngredientQuantityStandardUnit == ingredientQuantityStandardUnit)
                        {
                            possibleRecipesForIngredient += 1;
                            ingredientTotalCost += ingredientPriceInTargetCurrency * (ingredientQuantityStandardUnit - remainingQuantity);
                            ingredientTotalCost += remainingCost;
                            remainingQuantity = 0;
                            remainingCost = 0;
                        }
                        else if (recipeIngredientQuantityStandardUnit < ingredientQuantityStandardUnit)
                        {
                            int completeRecipes = (int)(ingredientQuantityStandardUnit / recipeIngredientQuantityStandardUnit);

                            // Calculate the total possible recipes if we add the complete recipes from this iteration
                            double newPossibleRecipesForIngredient = possibleRecipesForIngredient + completeRecipes;

                            if (newPossibleRecipesForIngredient >= minTotalAmount)
                            {
                                // Adjust completeRecipes so that possibleRecipesForIngredient doesn't exceed minTotalAmount
                                completeRecipes = (int)(minTotalAmount - possibleRecipesForIngredient);

                                // Update the number of possible recipes
                                possibleRecipesForIngredient += completeRecipes;

                                // Calculate the leftover quantity after making the adjusted number of complete recipes
                                double leftoverQuantity = ingredientQuantityStandardUnit - (recipeIngredientQuantityStandardUnit * completeRecipes);

                                // Calculate the cost for the adjusted number of complete recipes
                                ingredientTotalCost += ingredientPriceInTargetCurrency * (recipeIngredientQuantityStandardUnit * completeRecipes);

                                // Add the remaining cost from the previous iteration
                                ingredientTotalCost += remainingCost;
                                remainingCost = 0;

                                // Update the remaining quantity and cost
                                remainingQuantity = leftoverQuantity;
                                remainingCost = leftoverQuantity * ingredientPriceInTargetCurrency;

                                // Break out of the loop as we've reached the maximum possible recipes
                                break;
                            }
                            else
                            {
                                // Update the number of possible recipes
                                possibleRecipesForIngredient = newPossibleRecipesForIngredient;

                                // Calculate the leftover quantity after making the complete recipes
                                double leftoverQuantity = ingredientQuantityStandardUnit - (recipeIngredientQuantityStandardUnit * completeRecipes);

                                // Calculate the cost for the complete recipes
                                ingredientTotalCost += ingredientPriceInTargetCurrency * (recipeIngredientQuantityStandardUnit * completeRecipes);

                                // Add the remaining cost from the previous iteration
                                ingredientTotalCost += remainingCost;
                                remainingCost = 0;

                                // Update the remaining quantity and cost
                                remainingQuantity = leftoverQuantity;
                                remainingCost = leftoverQuantity * ingredientPriceInTargetCurrency;
                            }
                        }
                        else
                        {
                            remainingQuantity += ingredientQuantityStandardUnit;
                            remainingCost += ingredientQuantityStandardUnit * ingredientPriceInTargetCurrency;
                        }

                    }

                     totalIngredientsCost += ingredientTotalCost;
                    
                }

                // Return the calculated recipe amount DTO
                return new RecipeAmountDto
                {
                    MaximumPossibleRecipes = (int)(recipeIngredients.Count == 0 ? 0 : minTotalAmount),
                    PriceAllRecipes = minTotalAmount != 0 ? Math.Round(totalIngredientsCost, 2) : 0,
                    PriceUnitRecipe = minTotalAmount != 0 ? Math.Round(totalIngredientsCost / minTotalAmount, 2) : 0
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
        ///   - Second value (List<MissingIngredientsDto>): List of ingredients that are insufficient in quantity, if any.
        /// </returns>
        /// <exception cref="Exception">Thrown when an error occurs during the quantity check process.</exception>
        public (bool canPrepare, List<MissingIngredientsDto> missingIngredients) SubmitRecipeQuantity(int recipeId, int desiredQuantity)
        {
            try
            {
                // Fetch the recipe from the database using the recipeId
                var recipe = _dbContext.Recipe.FirstOrDefault(r => r.Id == recipeId);

                // Throw an exception if the recipe is not found
                if (recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, recipeId));
                }

                // Get all ingredients associated with the recipe
                var recipeIngredients = _dbContext.RecipeIngredients.Where(ri => ri.RecipeId == recipeId).ToList();

                // List to track ingredients that are insufficient
                var missingIngredients = new List<MissingIngredientsDto>();

                foreach (var recipeIngredient in recipeIngredients)
                {
                    // Convert the desired quantity into kilograms or liters based on the unit of measure
                    double quantityToReduce;
                    switch (recipeIngredient.Unit.ToLower())
                    {
                        case "g":
                        case "ml":
                            quantityToReduce = (recipeIngredient.Quantity / 1000) * desiredQuantity; // Convert to standard unit if necessary
                            break;
                        default:
                            quantityToReduce = recipeIngredient.Quantity * desiredQuantity;
                            break;
                    }

                    // Fetch available ingredient quantities from the database
                    var ingredientQuantities = _dbContext.IngredientQuantities
                        .Where(iq => iq.IngredientId == recipeIngredient.IngredientId && iq.ExpiringDate > DateTime.Now && iq.UsedDate == null)
                        .OrderBy(iq => iq.Id) // Sort by ID to use the oldest available quantities first
                        .ToList();

                    // Calculate the total available quantity of the ingredient
                    double totalAvailableQuantity = 0;
                    foreach (var ingredientQuantity in ingredientQuantities)
                    {
                        // Convert available quantity to the same unit as the desired quantity
                        double availableAmountInStandardUnit = ingredientQuantity.Amount;
                        switch (ingredientQuantity.Unit.ToLower())
                        {
                            case "g":
                                availableAmountInStandardUnit /= 1000; // Convert grams to kilograms
                                break;
                            case "ml":
                                availableAmountInStandardUnit /= 1000; // Convert milliliters to liters
                                break;
                        }
                        totalAvailableQuantity += availableAmountInStandardUnit;
                    }

                    // Determine the unit of measure
                    var unit = ingredientQuantities.FirstOrDefault()?.Unit ?? "";
                    var maxQuantity = (int)(totalAvailableQuantity / quantityToReduce);

                    if (unit == "g") unit = "Kg";
                    if (unit == "ml") unit = "L";

                    // Check if the ingredient is completely missing or insufficient
                    if (totalAvailableQuantity == 0)
                    {
                        var ingredient = _dbContext.Ingredient.FirstOrDefault(r => r.Id == recipeIngredient.IngredientId);
                        if (ingredient != null)
                        {
                            missingIngredients.Add(new MissingIngredientsDto
                            {
                                Unit = unit,
                                Quantity = Math.Round(quantityToReduce, 2),
                                Name = ingredient.Name
                            });
                        }
                    }
                    else if (maxQuantity < desiredQuantity)
                    {
                        var ingredient = _dbContext.Ingredient.FirstOrDefault(r => r.Id == recipeIngredient.IngredientId);
                        if (ingredient != null)
                        {
                            missingIngredients.Add(new MissingIngredientsDto
                            {
                                Unit = unit,
                                Quantity = Math.Round(quantityToReduce - totalAvailableQuantity, 2),
                                Name = ingredient.Name
                            });
                        }
                    }
                    else
                    {
                        // Update the ingredient quantities in the database
                        foreach (var ingredientQuantity in ingredientQuantities)
                        {
                            if (quantityToReduce > 0 && ingredientQuantity.Amount > 0)
                            {
                                double ingredientQuantityStandardUnit =
                                    ingredientQuantity.Unit == "g" || ingredientQuantity.Unit == "ml"
                                    ? ingredientQuantity.Amount / 1000
                                    : ingredientQuantity.Amount;

                                if (quantityToReduce > ingredientQuantityStandardUnit)
                                {
                                    quantityToReduce -= ingredientQuantityStandardUnit;
                                    ingredientQuantity.UsedDate = DateTime.Now;
                                    _dbContext.IngredientQuantities.Update(ingredientQuantity);
                                }
                                else
                                {
                                    ingredientQuantityStandardUnit -= quantityToReduce;

                                    if (ingredientQuantityStandardUnit == 0)
                                    {
                                        ingredientQuantity.UsedDate = DateTime.Now;
                                        _dbContext.IngredientQuantities.Update(ingredientQuantity);
                                    }
                                    else
                                    {
                                        ingredientQuantity.Amount = Math.Round(ingredientQuantityStandardUnit); // The amount remaining in stock
                                        _dbContext.IngredientQuantities.Update(ingredientQuantity);

                                        _dbContext.IngredientQuantities.Add(new IngredientQuantities // The amount used
                                        {
                                            Amount = Math.Round(quantityToReduce),
                                            ExpiringDate = ingredientQuantity.ExpiringDate,
                                            Unit = ingredientQuantity.Unit,
                                            DateOfPurchase = ingredientQuantity.DateOfPurchase,
                                            Price = ingredientQuantity.Price,
                                            Currency = ingredientQuantity.Currency,
                                            UsedDate = DateTime.Now,
                                            IngredientId = ingredientQuantity.IngredientId
                                        });
                                    }

                                    quantityToReduce = 0;
                                }
                            }
                        }
                    }
                }

                // If there are no missing ingredients, record the prepared recipe in the history
                if (missingIngredients.Count() == 0)
                {
                    PreparedRecipeHistory preparedRecipe = new PreparedRecipeHistory
                    {
                        Amount = desiredQuantity,
                        RecipeId = recipeId,
                        PreparationDate = DateTime.Now
                    };
                    _dbContext.PreparedRecipeHistory.AddAsync(preparedRecipe);
                    _dbContext.SaveChanges();
                }

                // Return the result indicating if the recipe can be prepared and the list of missing ingredients
                return (missingIngredients.Count() == 0, missingIngredients);
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
