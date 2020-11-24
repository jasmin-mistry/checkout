using Core.Commands;
using FluentValidation;

namespace Core.Validations
{
    public class PaymentProcessValidator : AbstractValidator<PaymentProcessCommand>
    {
        public PaymentProcessValidator()
        {
            RuleFor(x => x.CardNumber).NotEmpty().Matches(@"(\d{4}[-.\s]?){3}(\d{4})|\d{4}[-.\s]?\d{6}[-.\s]?\d{5}");
            RuleFor(x => x.ExpiryMonth).NotEmpty().Length(3);
            RuleFor(x => x.ExpiryYear).NotEmpty().MinimumLength(2).MaximumLength(4);
            RuleFor(x => x.Amount).NotEmpty().GreaterThan(0);
            RuleFor(x => x.Currency).NotEmpty().Length(3);
            RuleFor(x => x.Cvv).NotEmpty().Length(3);
        }
    }
}