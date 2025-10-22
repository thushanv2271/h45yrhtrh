using FluentValidation;

namespace Application.Impairment.GetEclThresholdSummary;

/// <summary>
/// Validates the GetEclThresholdSummaryQuery
/// </summary>
internal sealed class GetEclThresholdSummaryQueryValidator : AbstractValidator<GetEclThresholdSummaryQuery>
{
    public GetEclThresholdSummaryQueryValidator()
    {
        RuleFor(x => x.IndividualSignificantThreshold)
            .GreaterThan(0)
            .WithMessage("Individual significant threshold must be greater than 0");

        RuleFor(x => x.Currency)
            .MaximumLength(10)
            .When(x => !string.IsNullOrEmpty(x.Currency))
            .WithMessage("Currency code must not exceed 10 characters");
    }
}
