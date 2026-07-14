using FluentValidation;
using OSBIS.Models.ViewModels;

namespace OSBIS.Services.Validators
{
    /// <summary>
    /// Validator cho RegisterViewModel — OWASP A03 (Input Validation) + password strength (BR02).
    /// </summary>
    public class RegisterValidator : AbstractValidator<RegisterViewModel>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Vui lòng nhập tên đăng nhập")
                .Length(4, 50).WithMessage("Tên đăng nhập 4-50 ký tự")
                .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Tên đăng nhập chỉ chứa chữ, số, '.', '_', '-'");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Vui lòng nhập email")
                .EmailAddress().WithMessage("Email không hợp lệ");

            RuleFor(x => x.FullName)
                .NotEmpty().WithMessage("Vui lòng nhập họ tên")
                .MaximumLength(100);

            RuleFor(x => x.Phone)
                .Matches(@"^(0|\+84)[0-9]{9,10}$")
                .When(x => !string.IsNullOrEmpty(x.Phone))
                .WithMessage("Số điện thoại không hợp lệ");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu")
                .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
                .Matches(@"[A-Z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ HOA")
                .Matches(@"[a-z]").WithMessage("Mật khẩu phải có ít nhất 1 chữ thường")
                .Matches(@"[0-9]").WithMessage("Mật khẩu phải có ít nhất 1 chữ số");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Xác nhận mật khẩu không khớp");
        }
    }
}
