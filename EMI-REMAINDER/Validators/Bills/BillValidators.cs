using EMI_REMAINDER.DTOs.Bills;
using FluentValidation;

namespace EMI_REMAINDER.Validators.Bills;

public class CreateBillValidator : AbstractValidator<CreateBillRequest>
{
    private static readonly string[] ValidCategories = { "EMI", "Utilities", "Subscriptions", "Credit Card" };
    private static readonly string[] ValidFrequencies = { "Monthly", "Quarterly", "Yearly", "One-time" };

    public CreateBillValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required.")
            .Must(c => ValidCategories.Contains(c))
            .WithMessage($"Category must be one of: {string.Join(", ", ValidCategories)}.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.")
            .LessThanOrEqualTo(10_000_000).WithMessage("Amount cannot exceed ₹1,00,00,000.");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("Due date is required.")
            .GreaterThanOrEqualTo(DateTime.UtcNow.Date)
            .WithMessage("Due date cannot be in the past.");

        RuleFor(x => x.Frequency)
            .Must(f => ValidFrequencies.Contains(f))
            .WithMessage($"Frequency must be one of: {string.Join(", ", ValidFrequencies)}.")
            .When(x => !string.IsNullOrEmpty(x.Frequency));

        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("Notes must not exceed 2000 characters.")
            .When(x => x.Notes is not null);

        RuleFor(x => x.Institution)
            .MaximumLength(200).WithMessage("Institution name must not exceed 200 characters.")
            .When(x => x.Institution is not null);
    }
}

public class UpdateBillValidator : AbstractValidator<UpdateBillRequest>
{
    private static readonly string[] ValidCategories = { "EMI", "Utilities", "Subscriptions", "Credit Card" };
    private static readonly string[] ValidFrequencies = { "Monthly", "Quarterly", "Yearly", "One-time" };

    public UpdateBillValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => x.Title is not null);

        RuleFor(x => x.Category)
            .Must(c => ValidCategories.Contains(c!))
            .WithMessage($"Category must be one of: {string.Join(", ", ValidCategories)}.")
            .When(x => x.Category is not null);

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.")
            .When(x => x.Amount.HasValue);

        RuleFor(x => x.Frequency)
            .Must(f => ValidFrequencies.Contains(f!))
            .WithMessage($"Frequency must be one of: {string.Join(", ", ValidFrequencies)}.")
            .When(x => x.Frequency is not null);
    }
}
