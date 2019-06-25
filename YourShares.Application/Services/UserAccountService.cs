using System;
using System.Threading.Tasks;
using YourShares.Application.Exceptions;
using YourShares.Application.Interfaces;
using YourShares.Application.ViewModels;
using YourShares.Data.Interfaces;
using YourShares.Data.UoW;
using YourShares.Domain.Models;
using YourShares.Domain.Util;
using YourShares.RestApi.Models;

namespace YourShares.Application.Services
{
    public class UserAccountService : IUserAccountService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<UserAccount> _userAccountRepository;

        public UserAccountService(IRepository<UserAccount> userAccountRepository, IUnitOfWork unitOfWork)
        {
            _userAccountRepository = userAccountRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<UserAccount> GetById(Guid id)
        {
            var result = _userAccountRepository.GetById(id);
            if (result == null) throw new EntityNotFoundException($"User account id {id} not found");
            return result;
        }

        public async Task<bool> CreateUserAccount(UserAccountCreateModel model, Guid userProfileId)
        {
            if (!ValidateUtils.IsMail(model.Email)) throw new MalformedEmailException();
            if (model.Password.Length < 8) throw new FormatException("Password invalid");
            _userAccountRepository.Insert(new UserAccount
            {
                Email = model.Email,
                PasswordHash = model.Password,
                // TODO Hash password
                PasswordHashAlgorithm = "HASH",
                UserProfileId = userProfileId,
                UserAccountStatusCode = RefUserAccountStatusCode.GUEST
            });
            await _unitOfWork.CommitAsync();
            return true;
        }
    }
}