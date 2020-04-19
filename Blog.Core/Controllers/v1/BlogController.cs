using Blog.Core.IServices;
using Blog.Core.Model;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Blog.Core.Controllers.v1
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
   // [Authorize]
    public class BlogController : ControllerBase
    {
        readonly IAdvertisementServices _advertisementServices;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="advertisementServices"></param>
        public BlogController(IAdvertisementServices advertisementServices)
        { 
            _advertisementServices = advertisementServices;
        }
        // GET: api/Blog/5
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<List<Advertisement>> Get(int id)
        {
            return await _advertisementServices.Query(d => d.Id == id);
        }
    }
}