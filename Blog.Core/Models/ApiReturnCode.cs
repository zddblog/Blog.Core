using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Blog.Core.Models
{
    public class ApiReturnCode
    {
        /// <summary>
        /// token令牌
        /// </summary>
        public string token { get; set; }

        /// <summary>
        /// 刷新Token
        /// </summary>
        public string refreshToken { get; set; }
    }
}
