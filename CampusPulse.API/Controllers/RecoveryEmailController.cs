using Microsoft.AspNetCore.RateLimiting;
using CampusPulse.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace CampusPulse.API.Controllers
{
    [ApiController]
    [Authorize(Roles = "Student")]
    [EnableRateLimiting("recovery-email")]
    [Route("api/recovery-email")]
    public class RecoveryEmailController : ControllerBase
    {
        private readonly RecoveryEmailVerificationService _verificationService;

        public RecoveryEmailController(
            RecoveryEmailVerificationService verificationService)
        {
            _verificationService = verificationService;
        }

        [HttpPost("send-code")]
        public async Task<IActionResult> SendCode()
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            bool sent = await _verificationService.SendCodeAsync(
                userId.Value);

            if (!sent)
            {
                return BadRequest(new
                {
                    message = "A verification code cannot be sent right now. Check whether your email is already verified or wait before requesting another code."
                });
            }

            return Ok(new
            {
                message = "Verification code sent. Check your recovery Gmail."
            });
        }

        [HttpPost("verify-code")]
        public async Task<IActionResult> VerifyCode(
            VerifyRecoveryEmailRequest request)
        {
            int? userId = GetCurrentUserId();

            if (userId == null)
            {
                return Unauthorized();
            }

            bool verified = await _verificationService.VerifyCodeAsync(
                userId.Value,
                request.Code);

            if (!verified)
            {
                return BadRequest(new
                {
                    message = "Invalid or expired verification code."
                });
            }

            return Ok(new
            {
                message = "Recovery email verified successfully."
            });
        }

        private int? GetCurrentUserId()
        {
            string? userIdClaim =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdClaim, out int userId))
            {
                return userId;
            }

            return null;
        }
    }

    public class VerifyRecoveryEmailRequest
    {
        [Required]
        [RegularExpression(
            @"^[0-9]{6}$",
            ErrorMessage = "Verification code must contain exactly six digits.")]
        public string Code { get; set; } = string.Empty;
    }
}
