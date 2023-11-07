using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Retetar.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Retetar.Utils.Constants.ResponseConstants;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Retetar.Services
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public UserService(UserManager<User> userManager, SignInManager<User> signInManager, IConfiguration configuration, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Get all users.
        /// </summary>
        /// <returns>A list of all users.</returns>
        /// <exception cref="Exception">Thrown when there is an error retrieving the users.</exception>
        public async Task<List<User>> GetAllUsers()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                return users.Select(u => new User
                {
                    UserName = u.UserName,
                    Email = u.Email,
                    Id = u.Id
                }).ToList();
            }
            catch (Exception ex)
            {
               throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// Get a user by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>The User object if found, otherwise throws an exception.</returns>
        /// <exception cref="Exception">Thrown when the user is not found or there is an error retrieving it.</exception>
        public async Task<User> GetUserById(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    throw new Exception(string.Format(USER.NOT_FOUND, id));
                }

                return new User
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email
                };
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(UNKNOWN, id), ex);
            }
        }

        /// <summary>
        /// Register a new user asynchronously.
        /// </summary>
        /// <param name="user">The user to be registered.</param>
        /// <param name="password">The password for the user.</param>
        /// <returns>The result of the user registration operation.</returns>
        /// <exception cref="Exception">Thrown when there is an error saving the user to the database.</exception>
        public async Task<IdentityResult> RegisterUserAsync(User user, string password)
        {
            try
            {
                // Manually hash the password
                var passwordHasher = new PasswordHasher<IdentityUser>();
                user.PasswordHash = passwordHasher.HashPassword(user, password);

                // Add the user to the database using the UserManager
                var result = await _userManager.CreateAsync(user);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "USER");
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(USER.NOT_SAVED, ex);
            }
        }

        /// <summary>
        /// Login a user asynchronously.
        /// </summary>
        /// <param name="email">The email of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>The result of the user login operation.</returns>
        /// <exception cref="Exception">Thrown when there is an error authenticating the user.</exception>
        public async Task<SignInResult> LoginUserAsync(string email, string password)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    // Ensure the provided password is hashed before using it for authentication
                    var passwordHasher = new PasswordHasher<IdentityUser>();
                    var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password);
                    if (result == PasswordVerificationResult.Success)
                    {
                        return await _signInManager.PasswordSignInAsync(user, password, false, lockoutOnFailure: false);
                    }
                }

                return SignInResult.Failed;
            }
            catch (Exception ex)
            {
                throw new Exception(UNKNOWN, ex);
            }
        }

        /// <summary>
        /// Changes the role of a user to "Manager" if they do not already have that role.
        /// </summary>
        /// <param name="user">The user whose role is being changed.</param>
        /// <returns>
        /// Returns an IdentityResult indicating the result of the role change process.
        /// If the user's current role is already "Manager", throws an exception indicating that the user already has the role.
        /// If successfully removed from the current role and added to the "Manager" role, returns a success result.
        /// If an error occurs during role removal or role addition, throws an exception with error details.
        /// </returns>
        public async Task<IdentityResult> ChangeUserRoleAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    throw new Exception(string.Format(USER.NOT_FOUND, userId));
                }
                var currentRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();
                if (currentRole == "Manager")
                {
                    throw new Exception(USER.ALREADY_HAS_THE_ROLE);
                }

                var removeResult = await _userManager.RemoveFromRoleAsync(user, currentRole);
                if (removeResult.Succeeded)
                {
                    var addResult = await _userManager.AddToRoleAsync(user, "Manager");
                    if (addResult.Succeeded)
                    {
                        return addResult;
                    }
                    else
                    {
                        // Rollback changes if adding new role fails
                        await _userManager.AddToRoleAsync(user, currentRole);
                        throw new Exception(string.Join(", ", addResult.Errors));
                    }
                }
                else
                {
                    throw new Exception(string.Join(", ", removeResult.Errors));
                }
            }
            catch (Exception ex)
            {
                throw new Exception(USER.ERROR_CHANGE_ROLE, ex);
            }
        }

        /// <summary>
        /// Asynchronously deletes a user account based on their unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to delete.</param>
        /// <returns>True if the user is successfully deleted, otherwise false.</returns>
        /// <exception cref="Exception">Thrown when the user is not found or there is an error removing them.</exception>
        public async Task<bool> DeleteAccount(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    throw new Exception(string.Format(USER.NOT_FOUND, userId));

                var result = await _userManager.DeleteAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(USER.ERROR_DELETING, userId), ex);
            }
        }

        /// <summary>
        /// Asynchronously updates a user's email address.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to update.</param>
        /// <param name="newEmail">The new email address for the user.</param>
        /// <returns>True if the email is successfully updated, otherwise false.</returns>
        /// <exception cref="Exception">Thrown when there is an error updating the email.</exception>
        public async Task<bool> UpdateEmail(string userId, string newEmail)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return false;

                user.Email = newEmail;
                var result = await _userManager.UpdateAsync(user);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(USER.EMAIL_ERROR, userId), ex);
            }
        }

        /// <summary>
        /// Asynchronously updates a user's password.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to update.</param>
        /// <param name="currentPassword">The user's current password.</param>
        /// <param name="newPassword">The new password for the user.</param>
        /// <returns>True if the password is successfully updated, otherwise false.</returns>
        /// <exception cref="Exception">Thrown when there is an error updating the password.</exception>
        public async Task<bool> UpdatePassword(string userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    throw new Exception(string.Format(USER.NOT_FOUND, userId));

                var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
                return result.Succeeded;
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(USER.PASSWORD_ERROR, userId), ex);
            }
        }

        /// <summary>
        /// Asynchronously retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The email address of the user.</param>
        /// <returns>The User object representing the user with the specified email, or null if not found.</returns>
        /// <exception cref="Exception">Thrown when there is an error retrieving the user.</exception>
        public async Task<User> GetUserByEmail(string email)
        {
            try
            {
                return await _userManager.FindByEmailAsync(email);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format(USER.EMAIL_ERROR, email), ex);
            }
        }

        /// <summary>
        /// Generates a JWT token for the given user.
        /// </summary>
        /// <param name="user">The User object for which to generate the token.</param>
        /// <returns>The generated JWT token as a string.</returns>
        /// <exception cref="Exception">Thrown when there is an error generating the JWT token.</exception>
        public string GenerateJwtToken(User user)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    throw new ArgumentException("User or user email is null.");
                }

                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

                var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
                    _configuration["Jwt:Issuer"],
                    claims,
                    expires: DateTime.Now.AddMinutes(120),
                    signingCredentials: credentials);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                throw new Exception("Unknown Error!", ex);
            }
        }


        /// <summary>
        /// Sends a password reset email containing a reset link to the user's email address.
        /// </summary>
        /// <param name="email">The email address of the user requesting the password reset.</param>
        /// <param name="resetPasswordToken">The reset password token associated with the user.</param>
        /// <returns>A boolean indicating whether the email sending was successful or not.</returns>
        /// <exception cref="Exception">Thrown when there is an error generating the URL or sending the email.</exception>
        public async Task SendPasswordResetEmail(string email, string resetPasswordToken)
        {
            try
            {
                var resetUrl = GetResetPasswordUrl(email, resetPasswordToken);
                var emailMessage = $"Please click the link below to reset your password: \n\n{resetUrl}";
                await _emailSender.SendEmailAsync(email, "Reset Password", emailMessage);
            }
            catch (Exception ex)
            {
                throw new Exception("Error sending email.", ex);
            }
        }

        /// <summary>
        /// Asynchronously sends a password reset email to the user.
        /// </summary>
        /// <param name="email">The email address of the user to whom the password reset email will be sent.</param>
        /// <param name="resetPasswordToken">The reset password token associated with the user.</param>
        /// <returns>The task representing the asynchronous operation.</returns>
        /// <exception cref="Exception">Thrown when there is an error sending the password reset email.</exception>
        public string GetResetPasswordUrl(string email, string resetPasswordToken)
        {
            try
            {
                return $"https://localhost:44362/api/User/reset-password?email={email}&token={resetPasswordToken}";
            }
            catch (Exception ex)
            {
                throw new Exception("Unknown Error!", ex);
            }
        }


        /// <summary>
        /// Asynchronously resets the password for the specified user using the provided token and new password.
        /// </summary>
        /// <param name="user">The User object for which to reset the password.</param>
        /// <param name="token">The password reset token.</param>
        /// <param name="newPassword">The new password to set for the user.</param>
        /// <returns>The result of the password reset operation.</returns>
        /// <exception cref="Exception">Thrown when there is an error resetting the password.</exception>
        public async Task<IdentityResult> ResetPasswordAsync(User user, string token, string newPassword)
        {
            try
            {
                // Validate the user and token
                if (user == null || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(newPassword))
                {
                    throw new ArgumentException("Invalid user, token, or newPassword.");
                }

                // Reset the user's password using the UserManager
                var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error resetting password.", ex);
            }
        }
    }
}