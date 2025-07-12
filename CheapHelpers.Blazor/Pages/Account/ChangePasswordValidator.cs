//using FluentValidation;
//using CheapHelpers.Blazor.Shared;
//using CheapHelpers.Context;
//using Microsoft.EntityFrameworkCore;
//using CheapHelpers.EF;

//TODO: find a better way then fluent validation, it looks good until you get in complex situations

//namespace CheapHelpers.Blazor.Pages.Account
//{
//    public class ChangePasswordValidator : BaseValidator<ChangePassword.ChangePasswordViewModel>
//    {
//        public ChangePasswordValidator(IDbContextFactory<CheapContext> f)
//        {
//            RuleFor(x => x.NewPassword)
//                .NotEmpty()
//                .WithMessage("Password is empty!")
//                .MinimumLength(8)
//                .WithMessage("minimum 8 characters");

//            RuleFor(x => x.OldPassword)
//                .NotEmpty()
//                .WithMessage("Password is empty!");

//            RuleFor(x => x.ConfirmPassword)
//                .NotEmpty()
//                .WithMessage("Password is empty!")
//                .Must((a, b) => CheckEqual(a, b, f))
//                .WithMessage("The password and confirmation password do not match");
//        }
//        public bool CheckEqual(ChangePassword.ChangePasswordViewModel a, string pw, IDbContextFactory<CheapContext> f)
//        {
//            if (a.NewPassword == pw)
//            {
//                return true;
//            }
//            return false;
//        }


//    }
//}
