using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blog.Core.Models;
using Blog.Core.Services;
using Blog.Core.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Blog.Core.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class AccountController : ControllerBase
    {
        private readonly ICacheService _cacheService;

        public AccountController(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }
        /// <summary>
        /// 获取令牌
        /// </summary>
        /// <param name="ID">ID</param>
        /// <param name="name">账号</param>
        /// <returns></returns>
        [HttpPost]
        public IActionResult GetJwt(int ID, string name)
        {
            
            _cacheService.Add("ceshi2","哎呀哎呀",1);
            TokenModel tokenModel = new TokenModel
            {
                ID = ID,
                Name = name 
            };
            var apiReturn = new ApiReturnCode();
            apiReturn.token = TokenHelper.GetJWT(tokenModel);
            apiReturn.refreshToken = Guid.NewGuid().ToString();
            return Ok(apiReturn);
        }
    }
}
