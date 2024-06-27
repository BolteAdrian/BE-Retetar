using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Retetar.DataModels;
using Retetar.Models;
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

        private readonly SignInManager<User> _signInManager;
        private readonly UserService _userService;
        private readonly IEmailSender _emailService;
        private readonly IConfiguration _configuration;

        public UserController(SignInManager<User> signInManager, UserService userService, IConfiguration configuration, IEmailSender emailService)
        {
            _signInManager = signInManager;
            _userService = userService;
            _configuration = configuration;
            _emailService = emailService;
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

                if (users == null || !users.Any())
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = USER.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, users });
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
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_ID });
                }

                var user = await _userService.GetUserById(id);

                if (user == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = USER.NOT_FOUND });
                }

                return Ok(new { status = StatusCodes.Status200OK, user });
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
        public async Task<IActionResult> Register([FromBody] UserAuthDto model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
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

                    return Ok(new {status = StatusCodes.Status200OK, message = USER.SUCCESS_REGISTRATION });
                }

                return BadRequest(new { status = StatusCodes.Status400BadRequest, errors = result.Errors });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = USER.ERROR_REGISTER, error = ex.Message });
            }
        }

        /// <summary>
        /// Sets the application settings.
        /// </summary>
        /// <param name="settings">The new settings to be applied.</param>
        /// <returns>
        /// Returns a response indicating the result of the settings update process.
        /// If successful, returns a 200 OK response with a success message.
        /// If the provided settings data is invalid or null, returns a 400 Bad Request response with the error details.
        /// If the settings update process fails, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPut("settings")]
        public Task<IActionResult> SetSettings([FromBody] Settings settings)
        {
            try
            {
                if (settings == null)
                {
                    return Task.FromResult<IActionResult>(BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA }));
                }

                 _userService.SetSettings(settings);

                    return Task.FromResult<IActionResult>(Ok(new { status = StatusCodes.Status200OK, message = USER.SETTINGS_UPDATED }));
            }
            catch (Exception ex)
            {
                return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message, error = ex }));
            }
        }

        /// <summary>
        /// Gets the application settings.
        /// </summary>
        /// <returns>
        /// Returns a response containing the application settings.
        /// If the settings are found, returns a 200 OK response with the settings data.
        /// If the settings are not found, returns a 400 Bad Request response with an error message.
        /// If an error occurs during processing, returns a 500 Internal Server Error response with an error message.
        [HttpGet("settings")]
        public Task<IActionResult> GetSettings()
        {
            try
            {
                var result = _userService.GetSettings();

                if (result != null)
                {
                    return Task.FromResult<IActionResult>(Ok(new { status = StatusCodes.Status200OK, result }));
                }

                return Task.FromResult<IActionResult>(BadRequest(new { status = StatusCodes.Status400BadRequest, errors = USER.ERROR_GETTING_SETTINGS }));
            }
            catch (Exception ex)
            {
                return Task.FromResult<IActionResult>(StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message, error = ex }));
            }
        }

        /// <summary>
        /// Sends an email.
        /// </summary>
        /// <param name="emailData">The email data including recipient email, subject, and message body.</param>
        /// <returns>
        /// Returns an HTTP response indicating the result of the email sending process.
        /// If successful, returns a 200 OK response with a success message.
        /// If the provided email data is invalid (null email) or missing, returns a 400 Bad Request response with an error message.
        /// If the email sending process fails, returns a 400 Bad Request response with the error details.
        /// If an unexpected error occurs during processing, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPost("email")]
        public async Task<IActionResult> SendEmail()
        {
            try
            {
                string email = Request.Form["Email"];
                string subject = Request.Form["Subject"];
                string message = Request.Form["Message"];
                var attachment = Request.Form.Files["Attachment"]; // Assuming attachment is a file

                if (email == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                if (subject == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                // Validate email, subject, and other parameters if necessary

                var result = await _emailService.SendEmailAsync(email, subject, message, attachment);


                if (result != null)
                {
                    return Ok(new { status = StatusCodes.Status200OK, message = USER.EMAIL_SEND_SUCCESSFFULY });
                }

                return BadRequest(new { status = StatusCodes.Status400BadRequest, errors = result });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message, error = ex });
            }
        }

        /// <summary>
        /// Logs in a user.
        /// </summary>
        /// <param name="input">The login information provided by the user.</param>
        /// <returns>
        /// Returns a response indicating the result of the login process.
        /// If successful, returns a success login message.
        /// If the provided login data is invalid or null, returns a NotFound response.
        /// If the login process fails, returns a BadRequest response with the error details.
        /// If an error occurs during processing, returns a 500 Internal Server Error response with an error message.
        /// </returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserAuthDto input)
        {
            try
            {
                if (input == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                var result = await _userService.LoginUserAsync(input.Email, input.Password);

                if (result != null)
                {
                    return Ok(new { status = StatusCodes.Status200OK, message = USER.SUCCESS_LOGIN, token = result.Token, userName = result.UserName, id = result.UserId });
                }

                return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.ERROR_LOGIN });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = StatusCodes.Status500InternalServerError, message = USER.ERROR_LOGIN, error = ex.Message });
            }
        }

        /// <summary>
        /// Changes the role of a user in the system.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <remarks>
        /// This endpoint allows changing the role of a user and requires appropriate authorization.
        /// </remarks>
        /// <returns>
        /// Returns an IActionResult indicating the result of the role change operation.
        /// If the provided user ID is invalid, returns a BadRequest response with an appropriate message.
        /// If the role change is successful, returns an Ok response with a success message.
        /// If the role change fails, returns a BadRequest response with the error details.
        /// If an exception occurs during processing, returns a StatusCode 500 response with an error message.
        /// </returns>
        [HttpPost("change-role/{userId}")]
        public async Task<IActionResult> ChangeUserRole(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                var result = await _userService.ChangeUserRoleAsync(userId);

                if (result.Succeeded)
                {
                    return Ok(new { status = StatusCodes.Status200OK, message = USER.CHANGE_ROLE });
                }
                else
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = result.Errors });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = StatusCodes.Status500InternalServerError, message = USER.ERROR_CHANGE_ROLE, error = ex.Message });
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
        [Authorize(Policy = "ManagerOnly")]
        public async Task<IActionResult> DeleteAccount(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                var result = await _userService.DeleteAccount(userId);

                if (!result)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.ERROR_DELETING });
                }

                return Ok(new { status = StatusCodes.Status200OK, message = USER.SUCCESS_DELETING });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = StatusCodes.Status500InternalServerError, message = USER.ERROR_DELETING, error = ex.Message });
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
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                if (string.IsNullOrEmpty(newEmail))
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.INVALID_EMAIL });
                }

                var result = await _userService.UpdateEmail(userId, newEmail);

                if (!result)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.ERROR_UPDATING_EMAIL });
                }

                return Ok(new { status = StatusCodes.Status200OK, message = USER.SUCCESS_UPDATING_EMAIL });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = StatusCodes.Status500InternalServerError, message = USER.ERROR_UPDATING_EMAIL, error = ex.Message });
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
        public async Task<IActionResult> UpdatePassword(string userId, [FromBody] UpdatePasswordDto model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId) || model == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = INVALID_DATA });
                }

                var result = await _userService.UpdatePassword(userId, model.CurrentPassword, model.NewPassword);

                if (!result)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.PASSWORD_ERROR });
                }

                return Ok(new { status = StatusCodes.Status200OK, message = USER.SUCCESS_UPDATING_PASSWORD });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = StatusCodes.Status500InternalServerError, message = USER.PASSWORD_ERROR, error = ex.Message });
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
        public async Task<IActionResult> ForgotPassword([FromBody] ResetPasswordDto resetData)
        {
            try
            {
                if (resetData.Email == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.INVALID_EMAIL });
                }

                var user = await _userService.GetUserByEmail(resetData.Email);

                if (user == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = USER.NOT_FOUND });
                }

                var resetPasswordToken = _userService.GenerateJwtToken(user);

                if (resetPasswordToken == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.INVALID_TOKEN });
                }

                await _userService.SendPasswordResetEmail(resetData.Email, resetPasswordToken);

                return Ok(new { status = StatusCodes.Status200OK, message = USER.RESET_EMAIL_SEND });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { status = StatusCodes.Status500InternalServerError, error = ex.Message });
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
        [HttpPost("reset-password/{token}")]
        public async Task<IActionResult> ResetPassword(string token, [FromBody] ResetPasswordDto resetData)
        {
            try
            {
                if (resetData.Email == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.INVALID_EMAIL });
                }

                // Check the validity of the JWT token
                var user = await _userService.GetUserByEmail(resetData.Email);

                if (user == null)
                {
                    return NotFound(new { status = StatusCodes.Status404NotFound, message = USER.INVALID_EMAIL });
                }

                // Validate the received JWT token
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtKey = _configuration["JWTKey:Secret"];
                var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey ?? throw new InvalidOperationException("Jwt key is null")));
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = securityKey,
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JWTKey:ValidIssuer"],
                    ValidateAudience = false,
                    ValidateLifetime = true
                };

                // Try to validate the JWT token
                ClaimsPrincipal claimsPrincipal;
                try
                {
                    claimsPrincipal = tokenHandler.ValidateToken(token, tokenValidationParameters, out _);
                }
                catch (Exception ex)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.INVALID_TOKEN, error = ex.Message });
                }

                // Check if the email in the token matches the user's email
                var emailClaim = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");

                if (emailClaim == null || emailClaim.Value != resetData.Email)
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.INVALID_TOKEN });

                var tokenValidTo = claimsPrincipal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp)?.Value;
                if (tokenValidTo == null)
                {
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.INVALID_TOKEN });
                }

                var resetPasswordResult = await _userService.ResetPasswordAsync(user, token, resetData.NewPassword);
                if (resetPasswordResult.Succeeded)
                {
                    // Password reset was successful
                    return Ok(new { status = StatusCodes.Status200OK, message = USER.SUCCESS_UPDATING_PASSWORD });
                }
                else
                {
                    // An error occurred during password reset
                    // You can access the error details from resetPasswordResult.Errors
                    return BadRequest(new { status = StatusCodes.Status400BadRequest, message = USER.PASSWORD_ERROR });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
            }
        }
    }
}
