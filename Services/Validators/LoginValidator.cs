using FluentValidation;
using OSBIS.Models.ViewModels;

namespace OSBIS.Services.Validators
{
    /// <summary>
    /// Validator cho LoginViewModel — tuân thủ OWASP A03/A07.
    /// LoginViewModel dùng UsernameOrEmail (chấp nhận cả 2 dạng).
    /// </summary>
    public class LoginValidator : AbstractValidator<LoginViewModel>
    {
        public LoginValidator()
        {
            RuleFor(x => x.UsernameOrEmail)
                .NotEmpty().WithMessage("Vui lòng nhập tên đăng nhập hoặc email")
                .MaximumLength(255).WithMessage("Tối đa 255 ký tự");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu");
        }
    }
}
