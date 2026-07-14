using FluentValidation;
using OSBIS.Models.ViewModels;

namespace OSBIS.Services.Validators
{
    /// <summary>
    /// Validator cho ChangePasswordViewModel.
    /// Field trong ViewModel: CurrentPassword, NewPassword, ConfirmNewPassword.
    /// </summary>
    public class ChangePasswordValidator : AbstractValidator<ChangePasswordViewModel>
    {
        public ChangePasswordValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu hiện tại");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("Vui lòng nhập mật khẩu mới")
                .MinimumLength(8).WithMessage("Mật khẩu mới tối thiểu 8 ký tự")
                .Matches(@"[A-Z]").WithMessage("Phải có ít nhất 1 chữ HOA")
                .Matches(@"[a-z]").WithMessage("Phải có ít nhất 1 chữ thường")
                .Matches(@"[0-9]").WithMessage("Phải có ít nhất 1 chữ số")
                .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại");

            RuleFor(x => x.ConfirmNewPassword)
                .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu không khớp");
        }
    }
}
