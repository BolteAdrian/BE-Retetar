using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Retetar.Interfaces;
using Retetar.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Services
{
    public class UserService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public UserService(UserManager<User> userManager, IConfiguration configuration, IEmailSender emailSender)
        {
            _userManager = userManager;
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

                if (users == null)
                {
                    throw new Exception(string.Format(USER.NOT_FOUND));
                }

                return users;
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
                var passwordHasher = new PasswordHasher<User>();
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
        /// Authenticates a user asynchronously based on provided credentials.
        /// </summary>
        /// <param name="email">The email of the user.</param>
        /// <param name="password">The password of the user.</param>
        /// <returns>The result of the user authentication process.</returns>
        /// <exception cref="Exception">Thrown when there is an error during user authentication.</exception>
        public async Task<IJwtAutResponse> LoginUserAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
                throw new Exception(string.Format(USER.NOT_FOUND));
            if (!await _userManager.CheckPasswordAsync(user, password))
                throw new Exception(string.Format(USER.NOT_FOUND));

            var userRoles = await _userManager.GetRolesAsync(user);
            var authClaims = new List<Claim>
            {
               new Claim(ClaimTypes.Name, user.UserName),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }
            string token = GenerateToken(authClaims);

            return new IJwtAutResponse
            {
                Token = token,
                UserName = user.UserName,
                UserId = user.Id
            };
        }

        private string GenerateToken(IEnumerable<Claim> claims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWTKey:Secret"]));
            var _TokenExpiryTimeInHour = Convert.ToInt64(_configuration["JWTKey:TokenExpiryTimeInHour"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Issuer = _configuration["JWTKey:ValidIssuer"],
                Audience = _configuration["JWTKey:ValidAudience"],
                Expires = DateTime.UtcNow.AddHours(_TokenExpiryTimeInHour),
                SigningCredentials = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256),
                Subject = new ClaimsIdentity(claims)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
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
        /// <exception cref = "Exception" > Thrown when there is an error generating the JWT token.</exception>
        public string GenerateJwtToken(User user)
        {
            try
            {
                if (user == null || string.IsNullOrEmpty(user.Email))
                {
                    throw new ArgumentException("User or user email is null.");
                }

                var jwtKey = _configuration["JWTKey:Secret"];
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? throw new InvalidOperationException("Jwt key is null")));
                var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

               //Fetch user roles
               var userRoles = _userManager.GetRolesAsync(user).Result; // Fetch roles synchronously for simplicity

                var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

                //Add user roles as claims
                claims.AddRange(userRoles.Select(role => new Claim("role", role)));

                var token = new JwtSecurityToken(
                    issuer: _configuration["JWTKey:ValidIssuer"],
                    audience: _configuration["JWTKey:ValidAudience"],
                    claims: claims,
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
                var emailMessage = $"Please click the link below to reset your password: <br>{resetUrl}";
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
                return $"<a href=\"https://{_configuration["Frontend:HostName"]}/api/User/reset-password/{email}/{resetPasswordToken}\">Reset Password</a>";
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
                await _userManager.RemovePasswordAsync(user);

                var result = await _userManager.AddPasswordAsync(user, newPassword);
                
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error resetting password.", ex);
            }
        }
    }
}