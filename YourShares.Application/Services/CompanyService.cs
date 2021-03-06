﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using YourShares.Application.Exceptions;
using YourShares.Application.Interfaces;
using YourShares.Application.SearchModels;
using YourShares.Application.ViewModels;
using YourShares.Data.Interfaces;
using YourShares.Domain.Models;
using YourShares.Domain.Util;

namespace YourShares.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository<Company> _companyRepository;
        private readonly IRepository<UserProfile> _userRepository;
        private readonly ISharesAccountService _shareAccountService;
        private readonly IRepository<Shareholder> _shareholderRepository;

        #region Contructor        
        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyService"/> class.
        /// </summary>
        /// <param name="unitOfWork">The unit of work.</param>
        /// <param name="companyRepository">The company repository.</param>
        /// <param name="userRepository">The user repository.</param>
        /// <param name="shareholderRepository">The shareholder repository.</param>
        /// <param name="shareAccountService">The share account service.</param>
        public CompanyService(IUnitOfWork unitOfWork
            , IRepository<Company> companyRepository
            , IRepository<UserProfile> userRepository
            , IRepository<Shareholder> shareholderRepository
            , ISharesAccountService shareAccountService)
        {
            _unitOfWork = unitOfWork;
            _companyRepository = companyRepository;
            _userRepository = userRepository;
            _shareholderRepository = shareholderRepository;
            _shareAccountService = shareAccountService;
        }
        #endregion

        #region Create
        /// <summary>
        /// Create the specified model.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedUser"></exception>
        public async Task<Company> CreateCompany(string userId, CompanyCreateModel model)
        {
            if (ValidateUtils.IsNullOrEmpty(userId)) throw new UnauthorizedUser();
            var company = new Company
            {
                AdminProfileId = Guid.Parse(userId),
                CompanyName = model.CompanyName,
                CompanyDescription = model.CompanyDescription,
                Address = model.Address,
                Phone = model.Phone,
                Capital = model.Capital,
                TotalShares = model.TotalShares,
            };
            var inserted = _companyRepository.Insert(company).Entity;
            await _unitOfWork.CommitAsync();
            return inserted;
        }
        #endregion

        #region Update        
        /// <summary>
        /// Update the company information.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException">Company id {id} not found</exception>
        public async Task<bool> UpdateCompany(Guid id, CompanyUpdateModel model)
        {
            // TODO check editable permission with user id
            var company = _companyRepository.GetById(id);
            if (company == null) throw new EntityNotFoundException($"Company id {id} not found");
            company.CompanyName = model.CompanyName;
            company.CompanyDescription = model.CompanyDescription;
            company.Address = model.Address;
            company.Phone = model.Phone;
            company.Capital = model.Capital;
            company.TotalShares = model.TotalShares;
            company.OptionPollAmount = model.OptionPoll;
            _companyRepository.Update(company);
            await _unitOfWork.CommitAsync();
            return true;
        }
        #endregion

        #region Search        
        /// <summary>
        /// Searches the company.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedUser"></exception>
        public async Task<List<CompanyViewSearchModel>> SearchCompany(string userId, CompanySearchModel model)
        {
            if (ValidateUtils.IsNullOrEmpty(userId)) throw new UnauthorizedUser();
            const string defaultSort = "CompanyName ASC";
            var sortType = model.IsSortDesc ? "DESC" : "ASC";
            var sortField = ValidateUtils.IsNullOrEmpty(model.SortField)
                ? defaultSort
                : $"{model.SortField} {sortType}";
            var query = _companyRepository.GetManyAsNoTracking(x =>
                    (ValidateUtils.IsNullOrEmpty(model.CompanyName)
                    || x.CompanyName.ToUpper().Contains(model.CompanyName.ToUpper()))
                ).Join(_shareholderRepository.GetManyAsNoTracking(x => x.UserProfileId == Guid.Parse(userId))
                , x => x.CompanyId, y => y.CompanyId, (x, y) => new
                {
                    x.Address,
                    x.Phone,
                    x.Capital,
                    x.CompanyId,
                    x.AdminProfileId,
                    x.CompanyName,
                    x.CompanyDescription,
                    x.OptionPollAmount,
                    x.TotalShares,
                    x.Categories,
                    x.PhotoUrl
                }).Join(_userRepository.GetAll(),
                x => x.AdminProfileId, y => y.UserProfileId, (x, y) => new CompanyViewSearchModel
                {
                    Address = x.Address,
                    Phone = x.Phone,
                    Capital = x.Capital,
                    CompanyId = x.CompanyId,
                    AdminProfileId = x.AdminProfileId,
                    CompanyName = x.CompanyName,
                    CompanyDescription = x.CompanyDescription,
                    OptionPollAmount = x.OptionPollAmount,
                    TotalShares = x.TotalShares,
                    AdminName = $"{y.FirstName} {y.LastName}",
                    Categories = x.Categories,
                    PhotoUrl = x.PhotoUrl
                })
                .OrderBy(sortField);
            var result = query.Skip((model.Page - 1) * model.PageSize)
                .Take(model.PageSize);
            return result.ToList();
        }
        #endregion

        #region Get by id        
        /// <summary>
        /// Get the company by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException">Company id {id} not found</exception>
        public async Task<Company> GetById(Guid id)
        {
            var result = _companyRepository.GetById(id);
            if (result == null) throw new EntityNotFoundException($"Company id {id} not found");
            return result;
        }
        #endregion

        #region Delete        
        /// <summary>
        /// Delete company by its identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public async Task<bool> DeleteById(Guid id)
        {
            var company = _companyRepository.GetById(id);
            _companyRepository.Delete(company);
            await _unitOfWork.CommitAsync();
            return true;
        }
        #endregion

        #region Increase OptionPool        
        /// <summary>
        /// Increases the option pool.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException">Company id {model.CompanyId} not found</exception>
        public async Task<bool> IncreaseOptionPool(CompanyIncreaseOptionPoolMode model)
        {
            var company = _companyRepository.GetById(model.CompanyId);
            if (company == null) throw new EntityNotFoundException($"Company id {model.CompanyId} not found");
            company.OptionPollAmount += model.SharesAmount;
            company.TotalShares += model.SharesAmount;

            _companyRepository.Update(company);
            await _unitOfWork.CommitAsync();
            return true;
        }
        #endregion

        #region AddOptionPoolToShareholder        
        /// <summary>
        /// Adds the option pool to shareholder.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="CompanyId">The company identifier.</param>
        /// <param name="SharesholerId">The sharesholer identifier.</param>
        /// <returns></returns>
        /// <exception cref="EntityNotFoundException">Company id {CompanyId} not found</exception>
        public async Task AddOptionPoolToShareholder(CompanyAddOptionPoolToShareholderModel model, Guid CompanyId, Guid SharesholerId)
        {
            var company = _companyRepository.GetById(CompanyId);
            if (company == null) throw new EntityNotFoundException($"Company id {CompanyId} not found");
            company.OptionPollAmount -= model.RestrictedAmount;
            await _shareAccountService.AddRestrictedShares(SharesholerId, model.RestrictedAmount, model);
        }
        #endregion

        #region Get Company List by admin        
        /// <summary>
        /// Gets the companies by admin.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public async Task<List<Company>> GetCompaniesByAdmin(Guid id)
        {
            var result = _companyRepository.GetManyAsNoTracking(x => x.AdminProfileId == id)
               .Select(x => new Company
               {
                   Address = x.Address,
                   Phone = x.Phone,
                   Capital = x.Capital,
                   CompanyId = x.CompanyId,
                   AdminProfileId = x.AdminProfileId,
                   CompanyName = x.CompanyName,
                   CompanyDescription = x.CompanyDescription,
                   OptionPollAmount = x.OptionPollAmount,
                   TotalShares = x.TotalShares
               });
            return result.ToList();
        }
        #endregion
    }
}