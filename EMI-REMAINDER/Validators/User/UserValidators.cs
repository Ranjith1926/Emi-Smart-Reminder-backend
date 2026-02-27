using EMI_REMAINDER.DTOs.User;
using FluentValidation;

namespace EMI_REMAINDER.Validators.User;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100).WithMessage("Name must not exceed 100 characters.")
            .MinimumLength(2).WithMessage("Name must be at least 2 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("Invalid email address format.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));
    }
}

public class UpdatePreferencesValidator : AbstractValidator<UpdatePreferencesRequest>
{
    public UpdatePreferencesValidator()
    {
        RuleFor(x => x.ReminderDays)
            .Must(BeValidReminderDays)
            .WithMessage("ReminderDays must be comma-separated integers (e.g., '7,3,1,0').")
            .When(x => x.ReminderDays is not null);

        RuleFor(x => x.Language)
            .Must(l => new[] { "en", "hi", "ta", "te", "kn", "mr", "gu", "bn" }.Contains(l!))
            .WithMessage("Language must be a valid ISO 639-1 code (en, hi, ta, te, kn, mr, gu, bn).")
            .When(x => x.Language is not null);
    }

    private static bool BeValidReminderDays(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return value.Split(',').All(d => int.TryParse(d.Trim(), out var n) && n >= 0 && n <= 30);
    }
}
