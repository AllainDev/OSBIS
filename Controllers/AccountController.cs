using Microsoft.AspNetCore.Mvc;
using ORBIS.Models.ViewModels;
using ORBIS.Services;


namespace ORBIS.Controllers;

public class AccountController : Controller
{
    private readonly IRegistrationService
        _registrationService;

    public AccountController(
        IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ServiceResult result =
            await _registrationService
                .StartRegistrationAsync(
                    model.FullName,
                    model.Email,
                    model.Password,
                    cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Message);

            return View(model);
        }

        TempData["SuccessMessage"] =
            result.Message;

        return RedirectToAction(
            nameof(VerifyOtp),
            new
            {
                email = model.Email.Trim()
            });
    }

    [HttpGet]
    public IActionResult VerifyOtp(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(
                nameof(Register));
        }

        return View(new VerifyOtpViewModel
        {
            Email = email.Trim()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyOtp(
        VerifyOtpViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ServiceResult result =
            await _registrationService
                .VerifyOtpAsync(
                    model.Email,
                    model.Otp,
                    cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(
                string.Empty,
                result.Message);

            return View(model);
        }

        TempData["SuccessMessage"] =
            result.Message;

        return RedirectToAction(
            nameof(RegisterSuccess));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendOtp(
        string email,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return RedirectToAction(
                nameof(Register));
        }

        ServiceResult result =
            await _registrationService
                .ResendOtpAsync(
                    email,
                    cancellationToken);

        TempData[
            result.Succeeded
                ? "SuccessMessage"
                : "ErrorMessage"] =
            result.Message;

        return RedirectToAction(
            nameof(VerifyOtp),
            new { email });
    }

    [HttpGet]
    public IActionResult RegisterSuccess()
    {
        return View();
    }
}