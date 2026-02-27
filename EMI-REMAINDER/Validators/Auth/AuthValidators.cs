using EMI_REMAINDER.DTOs.Auth;
using FluentValidation;

namespace EMI_REMAINDER.Validators.Auth;

public class SendOtpValidator : AbstractValidator<SendOtpRequest>
{
    public SendOtpValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^(\+91|91)?[6-9]\d{9}$")
            .WithMessage("Invalid Indian mobile number.");
    }
}

public class VerifyOtpValidator : AbstractValidator<VerifyOtpRequest>
{
    public VerifyOtpValidator()
    {
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^(\+91|91)?[6-9]\d{9}$")
            .WithMessage("Invalid Indian mobile number.");

        RuleFor(x => x.Otp)
            .NotEmpty().WithMessage("OTP is required.")
            .Matches(@"^\d{6}$").WithMessage("OTP must be a 6-digit number.");

        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("RequestId is required.")
            .Matches(@"^[0-9a-fA-F-]{36}$").WithMessage("Invalid RequestId format.");
    }
}
