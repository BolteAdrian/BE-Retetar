namespace Retetar.Utils.Constants
{
    public class ResponseConstants
    {
        public const string UNKNOWN = "Unknown Error!";
        public const string UNREACHABLE = "Resource Unavailable!";
        public const string INVALID_ID = "Invalid ID received!";
        public const string INVALID_DATA = "Invalid data received!";

        public static class INGREDIENT
        {
            public const string NOT_FOUND = "Ingredient not found!";
            public const string NOT_SAVED = "Could not save the Ingredient!";
            public const string ERROR_UPDATING = "Could not update the Ingredient!";
            public const string ERROR_DELETING = "Could not delete the Ingredient!";
            public const string SUCCESS_UPDATING = "Ingredient updated successfully!";
            public const string SUCCESS_DELETING = "Ingredient deleted successfully!";
        }

        public static class RECIPE
        {
            public const string NOT_FOUND = "Recipe not found!";
            public const string NOT_SAVED = "Could not save the Recipe!";
            public const string ERROR_UPDATING = "Could not update the Recipe!";
            public const string ERROR_DELETING = "Could not delete the Recipe!";
            public const string SUCCESS_UPDATING = "Recipe updated successfully!";
            public const string SUCCESS_DELETING = "Recipe deleted successfully!";
        }

        public static class CATEGORY
        {
            public const string NOT_FOUND = "Category not found!";
            public const string NOT_SAVED = "Could not save the Category!";
            public const string ERROR_UPDATING = "Could not update the Category!";
            public const string ERROR_DELETING = "Could not delete the Category!";
            public const string SUCCESS_UPDATING = "Category updated successfully!";
            public const string SUCCESS_DELETING = "Category deleted successfully!";
        }

        public static class USER
        {
            public const string NOT_SAVED = "Could Not Register User!";
            public const string EMAIL_TAKEN = "That Email Is Taken!";
            public const string EMAIL_ERROR = "Could Not Change User email!";
            public const string PASSWORD_ERROR = "Could Not Change User password!";
            public const string NEW_PASSWORD_ERROR = "The password must have:\nAt least 8 characters (required for your Muhlenberg password) — the more characters, the better.\nA mixture of both uppercase and lowercase letters.\nA mixture of letters and numbers.\nInclusion of at least one special character, e.g., ! @ # ?";
            public const string NOT_FOUND = "User not found!";
            public const string SUCCESS_UPDATING = "User updated successfully!";
            public const string SUCCESS_REGISTRATION = "Registration successfully!!";
            public const string SUCCESS_LOGIN = "Login successfully!!";
            public const string ERROR_LOGIN = "Invalid login attempt!";
            public const string ERROR_REGISTER = "Invalid register attempt!";
            public const string ERROR_UPDATING_EMAIL = "Could not update the email!";
            public const string ERROR_DELETING = "Could not delete user!";
            public const string SUCCESS_DELETING = "User deleted successfully!";
            public const string SUCCESS_UPDATING_EMAIL = "The email was updated!";
            public const string SUCCESS_UPDATING_PASSWORD = "The password was changed!";
            public const string INVALID_EMAIL = "Invalid email received!";
            public const string INVALID_TOKEN = "Invalid token!";
            public const string RESET_EMAIL_SEND = "Password reset email sent!";
            public const string CHANGE_ROLE = " User role changed successfully!";
            public const string ALREADY_HAS_THE_ROLE = "User already has the requested role!";
            public const string ERROR_CHANGE_ROLE = "Error changing user role!";
        }

        public static class EMAIL
        {
            public const string CONFIG_NULL = "Email configuration is null!";
            public const string EMAIL_NULL_OR_EMPTY = "Email address cannot be null or empty!";
            public const string ERROR_SENDING = "Could Not Send Email!";
        }
    }
}
