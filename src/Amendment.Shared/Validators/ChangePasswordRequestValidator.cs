using Amendment.Shared.Requests;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared.Validators
{
    public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
    {
        public ChangePasswordRequestValidator()
        {
            // Current password is required unless it's a first-time login
            When(x => !x.IsFirstTimeLogin, () =>
            {
                RuleFor(x => x.CurrentPassword)
                    .NotEmpty().WithMessage("Current password is required");

                // New password must be different from current password
                RuleFor(x => x.NewPassword)
                    .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from current password");
            });

            // New password requirements
            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter")
                .Matches("[0-9]").WithMessage("Password must contain at least one number")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character");

            // Confirm password must match new password
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Please confirm your password")
                .Equal(x => x.NewPassword).WithMessage("Passwords do not match");
        }
    }
}
