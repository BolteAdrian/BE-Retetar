using Retetar.Interfaces;
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
        /// Retrieves all Recipes from the database.
        /// </summary>
        /// <returns>
        /// Returns a list of all Recipes if successful.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public List<Recipe> GetAllRecipes()
        {
            try
            {
                return _dbContext.Recipe.ToList();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
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
                if (!string.IsNullOrEmpty(options.SearchTerm))
                {
                    string searchTermLower = options.SearchTerm.ToLower();
                    query = query.Where(g =>
                        options.SearchFields.Any(f => g.Name.ToLower().Contains(searchTermLower)) ||
                        options.SearchFields.Any(f => g.Category.Name.ToLower().Contains(searchTermLower))
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
        private IQueryable<Recipe> SortQuery(IQueryable<Recipe> query, string sortField, bool isAscending)
        {
            switch (sortField.ToLower())
            {
                case "name":
                    return isAscending ? query.OrderBy(g => g.Name) : query.OrderByDescending(g => g.Name);
                case "category":
                    return isAscending ? query.OrderBy(g => g.Category.Name) : query.OrderByDescending(g => g.Category.Name);
                default:
                    return query; // If the sorting field does not exist or is not specified, return the unchanged query
            }
        }

        /// <summary>
        /// Retrieves a Recipe by its unique identifier from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the Recipe.</param>
        /// <returns>
        /// Returns the Recipe object if found.
        /// If the Recipe with the specified ID is not found, throws an exception with an appropriate error message.
        /// If an error occurs during processing, throws an exception with an error message.
        /// </returns>
        public Recipe GetRecipeById(int id)
        {
            try
            {
                var Recipe = _dbContext.Recipe.FirstOrDefault(g => g.Id == id);
                if (Recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, id));
                }
                return Recipe;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.NOT_FOUND, id), ex);
            }
        }

        /// <summary>
        /// Adds a new Recipe to the database.
        /// </summary>
        /// <param name="Recipe">The Recipe object to be added.</param>
        /// <exception cref="ArgumentNullException">Thrown when the Recipe object is null.</exception>
        /// <exception cref="Exception">Thrown when an error occurs during saving the Recipe to the database.</exception>
        public void AddRecipe(Recipe Recipe)
        {
            try
            {
                _dbContext.Recipe.Add(Recipe);
                _dbContext.SaveChanges();
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
        /// <param name="Recipe">The Recipe object containing the updated data.</param>
        /// <exception cref="ArgumentNullException">Thrown when the Recipe object is null.</exception>
        /// <exception cref="Exception">Thrown when the Recipe with the specified ID is not found or an error occurs during updating.</exception>
        public void UpdateRecipe(int id, Recipe Recipe)
        {
            try
            {
                var existingRecipe = _dbContext.Recipe.Find(id);

                if (existingRecipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, id));
                }

                existingRecipe.Name = Recipe.Name;
                existingRecipe.Description = Recipe.Description;
                existingRecipe.CookingInstructions = Recipe.CookingInstructions;
                existingRecipe.CategoryId = Recipe.CategoryId;
                existingRecipe.UserId = Recipe.UserId;

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
        /// <param name="id">The unique identifier of the Recipe to be deleted.</param>
        /// <exception cref="Exception">Thrown when the Recipe is not found or an error occurs during deletion.</exception>
        public void DeleteRecipe(int id)
        {
            try
            {
                var Recipe = _dbContext.Recipe.Find(id);

                if (Recipe == null)
                {
                    throw new Exception(string.Format(RECIPE.NOT_FOUND, id));
                }

                _dbContext.Recipe.Remove(Recipe);
                _dbContext.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(RECIPE.ERROR_DELETING, id), ex);
            }
        }
    }
}
