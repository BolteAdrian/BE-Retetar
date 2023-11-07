using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Retetar.Interfaces;
using Retetar.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static Retetar.Utils.Constants.ResponseConstants;

namespace Retetar.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {

        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserService _userService;
        private readonly IConfiguration _configuration;

        public UserController(SignInManager<IdentityUser> signInManager, UserService userService, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _userService = userService;
            _configuration = configuration;
        }

        /// <summary>
        /// Retrieves all users from the database.
        /// </summary>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a list of all users if successful.
        /// If no users are found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet]
        [Authorize(Policy = "ManagerOnly")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                var users = await _userService.GetAllUsers();

                if (users == null)
                {
                    return NotFound(USER.NOT_FOUND);
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a user by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the user.</param>
        /// <returns>
        /// Returns the user's information if found.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If no user is found with the specified ID, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetUserById(string id)
        {
            try
            {
                if (id == null)
                {
                    return BadRequest(INVALID_ID);
                }

                var user = await _userService.GetUserById(id);

                if (user == null)
                {
                    return NotFound(USER.NOT_FOUND);
                }

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.NOT_FOUND, error = ex.Message });
            }
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        /// <param name="model">The registration information provided by the user.</param>
        /// <returns>
        /// Returns a response indicating the result of the registration process.
        /// If successful, returns a registration success message.
        /// If the provided registration data is invalid or null, returns a NotFound response.
        /// If the registration process fails, returns a BadRequest response with the error details.
        /// If an error occurs during processing, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] IUserAuth model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(INVALID_DATA);
                }

                var user = new User
                {
                    UserName = model.UserName,
                    Email = model.Email,
                };

                var password = model.Password;

                var result = await _userService.RegisterUserAsync(user, password);

                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    return Ok(USER.SUCCES_REGISTRATION);
                }

                return BadRequest(result.Errors);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.ERROR_REGISTER, error = ex.Message });
            }
        }

        /// <summary>
        /// Logs in a user.
        /// </summary>
        /// <param name="model">The login information provided by the user.</param>
        /// <returns>
        /// Returns a response indicating the result of the login process.
        /// If successful, returns a success login message.
        /// If the provided login data is invalid or null, returns a NotFound response.
        /// If the login process fails, returns a BadRequest response with the error details.
        /// If an error occurs during processing, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] IUserAuth model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(INVALID_DATA);
                }

                var result = await _userService.LoginUserAsync(model.Email, model.Password);

                if (result.Succeeded)
                {
                    return Ok(USER.SUCCES_LOGIN);
                }

                return BadRequest(USER.ERROR_LOGIN);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.ERROR_LOGIN, error = ex.Message });
            }
        }

        [HttpPost("change-role/{userId}")]
        public async Task<IActionResult> ChangeUserRole(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(INVALID_DATA);
                }

                var result = await _userService.ChangeUserRoleAsync(userId);

                if (result.Succeeded)
                {
                    return Ok(USER.CHANGE_ROLE);
                }
                else
                {
                    return BadRequest(result.Errors);
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.ERROR_CHANGE_ROLE, error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a user from the database based on its unique identifier.
        /// </summary>
        /// <param name="userId">The unique identifier of the user to be deleted.</param>
        /// <remarks>
        /// This endpoint requires the user to have the "ManagerOnly" authorization policy.
        /// </remarks>
        /// <returns>
        /// Returns a status code indicating the result of the update operation.
        /// If the provided ID is invalid, returns a BadRequest response with an appropriate message.
        /// If the user with the specified ID is not found, returns a NotFound response with an appropriate message.
        /// If an error occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpDelete("delete/{userId}")]
        [Authorize]
        public async Task<IActionResult> DeleteAccount(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(INVALID_DATA);
                }

                var result = await _userService.DeleteAccount(userId);

                if (!result)
                {
                    return Ok(USER.ERROR_DELETING);
                }

                return Ok(new { message = USER.SUCCES_DELETING });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.ERROR_DELETING, error = ex.Message });
            }
        }

        /// <summary>
        /// Updates the email of a user.
        /// </summary>
        /// <param name="userId">The ID of the user whose email is being updated.</param>
        /// <param name="newEmail">The new email to be set for the user.</param>
        /// <remarks>
        /// This endpoint requires the user to be authenticated.
        /// </remarks>
        /// <returns>
        /// Returns a response indicating the result of the email update process.
        /// If successful, returns a success message.
        /// If the provided user ID is invalid, returns a BadRequest response.
        /// If the email update process fails, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPut("update-email/{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdateEmail(string userId, [FromBody] string newEmail)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(INVALID_DATA);
                }

                if (newEmail == null)
                {
                    return BadRequest(USER.INVALID_EMAIL);
                }

                var result = await _userService.UpdateEmail(userId, newEmail);

                if (!result)
                {
                    return Ok(USER.ERROR_UPDATING_EMAIL);
                }

                return Ok(new { message = USER.SUCCES_UPDATING_EMAIL });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.ERROR_UPDATING_EMAIL, error = ex.Message });
            }
        }

        /// <summary>
        /// Updates the email of a user.
        /// </summary>
        /// <param name="userId">The ID of the user whose email is being updated.</param>
        /// <param name="newEmail">The new email to be set for the user.</param>
        /// <remarks>
        /// This endpoint requires the user to be authenticated.
        /// </remarks>
        /// <returns>
        /// Returns a response indicating the result of the email update process.
        /// If successful, returns a success message.
        /// If the provided user ID is invalid, returns a BadRequest response.
        /// If the email update process fails, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPut("update-password/{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdatePassword(string userId, [FromBody] IUpdatePassword model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(INVALID_DATA);
                }

                if (model == null)
                {
                    return BadRequest(INVALID_DATA);
                }

                var result = await _userService.UpdatePassword(userId, model.CurrentPassword, model.NewPassword);

                if (!result)
                {
                    return Ok(USER.PASSWORD_ERROR);
                }

                return Ok(new { message = USER.SUCCES_UPDATING_PASSWORD });

            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.PASSWORD_ERROR, error = ex.Message });
            }
        }

        /// <summary>
        /// Initiates the process of resetting a user's password by sending a password reset email.
        /// </summary>
        /// <param name="email">The email address of the user requesting a password reset.</param>
        /// <returns>
        /// Returns a response indicating the result of the password reset process.
        /// If successful, returns a success message indicating that the password reset email has been sent.
        /// If the provided email address is invalid, returns a BadRequest response.
        /// If the user with the provided email is not found, returns a NotFound response.
        /// If the password reset token generation fails, returns a BadRequest response with an error message.
        /// If the password reset email sending process fails, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            try
            {
                if (email == null)
                {
                    return BadRequest(USER.INVALID_EMAIL);
                }

                var user = await _userService.GetUserByEmail(email);

                if (user == null)
                {
                    return NotFound(USER.NOT_FOUND);
                }

                var resetPasswordToken = _userService.GenerateJwtToken(user);

                if (resetPasswordToken == null)
                {
                    return BadRequest(USER.INVALID_TOKEN);
                }

                await _userService.SendPasswordResetEmail(email, resetPasswordToken);

                return Ok(USER.RESET_EMAIL_SEND);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Resets the password for a user using a valid password reset token.
        /// </summary>
        /// <param name="email">The email address of the user whose password is being reset.</param>
        /// <param name="token">The JWT token used to verify the validity of the password reset request.</param>
        /// <returns>
        /// Returns a response indicating the result of the password reset process.
        /// If successful, returns a success message indicating that the password has been reset.
        /// If the provided email address is invalid, returns a BadRequest response.
        /// If the user with the provided email is not found, returns a NotFound response.
        /// If the token validation fails, returns a BadRequest response with an error message.
        /// If the email in the token does not match the user's email, returns a BadRequest response.
        /// If the password reset process is successful, returns a success message.
        /// If an error occurs during the password reset process, returns a success message.
        /// </returns>
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] IForgotPassword dataForReset)
        {
            try
            {
                if (dataForReset.email == null)
                {
                    return BadRequest(USER.INVALID_EMAIL);
                }

                // Check the validity of the JWT token
                var user = await _userService.GetUserByEmail(dataForReset.email);

                if (user == null)
                {
                    return NotFound(USER.INVALID_EMAIL);
                }

                // Validate the received JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidateAudience = false, // We don't validate the audience because it's not required in this case
                    ValidateLifetime = true
                };

                // Try to validate the JWT token
                ClaimsPrincipal claimsPrincipal;
                try
                {
                    claimsPrincipal = tokenHandler.ValidateToken(dataForReset.token, tokenValidationParameters, out _);
                }
                catch (Exception ex)
                {
                    return BadRequest(USER.INVALID_TOKEN + ex.Message);
                }

                // Check if the email in the token matches the user's email
                var emailClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email);
                if (emailClaim == null || emailClaim.Value != dataForReset.email)
                    return BadRequest(USER.INVALID_TOKEN);

                // Implement the logic to reset the password and update it in the database
                // For example, you can use UserManager to update the user's password:
                var newPassword = "YourNewPasswordHere"; // You can receive the new password from a form or an action parameter
                var tokenValidTo = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
                var validTo = DateTimeOffset.FromUnixTimeSeconds(long.Parse(tokenValidTo)).UtcDateTime;
                var resetPasswordResult = await _userService.ResetPasswordAsync(user, dataForReset.token, newPassword);
                if (resetPasswordResult.Succeeded)
                {
                    // Password reset was successful
                    return Ok(new { message = USER.SUCCES_UPDATING_PASSWORD });
                }
                else
                {
                    // An error occurred during password reset
                    // You can access the error details from resetPasswordResult.Errors
                    return Ok(new { message = USER.PASSWORD_ERROR });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}
