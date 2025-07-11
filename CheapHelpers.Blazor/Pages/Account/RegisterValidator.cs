using FluentValidation;
using CheapHelpers.Blazor.Shared;
using CheapHelpers.Context;
using Microsoft.EntityFrameworkCore;

namespace CheapHelpers.Blazor.Pages.Account
{
	public class RegisterValidator : BaseValidator<Register.RegisterViewModel>
	{
		public RegisterValidator(IDbContextFactory<MecamContext> f)
		{
			RuleFor(x => x.FirstName).NotEmpty().WithMessage("Firstname is empty!");
			RuleFor(x => x.LastName).NotEmpty().WithMessage("Lastname is empty!");
			RuleFor(x => x.Email).NotEmpty().WithMessage("Email is empty!").EmailAddress();
			RuleFor(x => x.Password)
				.NotEmpty()
				.WithMessage("Password is empty!")
				.MinimumLength(6)
				.WithMessage("Password needs to be more than 5");
			RuleFor(x => x.ConfirmPassword)
				.NotEmpty()
				.WithMessage("Password is empty!")
				.Must((a, b) => CheckEqual(a, b, f))
				.WithMessage("The password and confirmation password do not match");
		}

		public bool CheckEqual(
			Register.RegisterViewModel a,
			string pw,
			IDbContextFactory<MecamContext> f
		)
		{
			if (a.Password == pw)
			{
				return true;
			}
			return false;
		}
	}
}
