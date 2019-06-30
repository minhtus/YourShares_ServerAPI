using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YourShares.Application.Interfaces;
using YourShares.Application.ViewModels;
using YourShares.Domain.Models;
using YourShares.RestApi.ApiResponse;

namespace YourShares.RestApi.Controllers
{
    [ApiController]
    [Route("/api/rounds")]
    [Produces("application/json")]
    [Authorize]
    public class RoundController : ControllerBase
    {
        private readonly IRoundService _roundService;

        #region Constructor
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="roundService"> Injected RoundService </param>
        public RoundController(IRoundService roundService)
        {
            _roundService = roundService;
        }
        #endregion
        
        #region Get by Id
        /// <summary>
        /// Get round specified by its id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("{id}")]
        public async Task<ResponseModel<Round>> GetById([FromRoute] Guid id)
        {
            var result = await _roundService.GetById(id);
            return new ResponseBuilder<Round>()
                .Success()
                .Data(result)
                .Count(1)
                .build();
        }
        #endregion

        #region Get by company id
        /// <summary>
        /// Get all rounds of a company specified by company id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Route("companies/{id}")]
        [HttpGet]
        public async Task<ResponseModel<List<Round>>> GetByCompanyId([FromRoute] Guid id)
        {
            var result = await _roundService.GetByCompanyId(id);
            return new ResponseBuilder<List<Round>>()
                .Success()
                .Data(result)
                .Count(result.Count)
                .build();
        }
        #endregion
        
        #region Create
        /// <summary>
        /// Create a new round of company, round detail in the request body
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<Round> CreateRound([FromBody] RoundCreateModel model)
        {
            var result = await _roundService.InsertRound(model);
            Response.StatusCode = (int) HttpStatusCode.Created;
            return result;
        }
        #endregion
        
    }
}