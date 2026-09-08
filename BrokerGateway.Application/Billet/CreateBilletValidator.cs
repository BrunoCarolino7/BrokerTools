using FluentValidation;

namespace BrokerGateway.Application.Billet;

public class CreateBilletValidator :AbstractValidator<CreateBilletCommand>
{
    public CreateBilletValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
        
        RuleFor(x => x.Description)
            .MaximumLength(200)
            .NotEmpty();
    }
}