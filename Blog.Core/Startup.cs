using Autofac;
using Autofac.Extras.DynamicProxy;
using Blog.Core.ServiceImpl;
using Blog.Core.Services;
using Blog.Core.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Blog.Core
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }
        /// <summary>
        /// Api版本信息
        /// </summary>
        private IApiVersionDescriptionProvider provider;
        public IConfiguration Configuration { get; }

        private string ApiName = "测试Api";
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            #region 版本控制
            services.AddApiVersioning(option =>
            {
                // 可选，为true时API返回支持的版本信息
                option.ReportApiVersions = true;
                // 不提供版本时，默认为1.0
                option.AssumeDefaultVersionWhenUnspecified = true;
                // 请求中未指定版本时默认为1.0
                option.DefaultApiVersion = new ApiVersion(1, 0);
            }).AddVersionedApiExplorer(option =>
            {
                // 版本名的格式：v+版本号
                option.GroupNameFormat = "'v'V";
                option.AssumeDefaultVersionWhenUnspecified = true;
            });

            this.provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
            #endregion

            #region 格式化时间
            services.AddControllers()
                            .AddJsonOptions(configure =>
                            {
                                configure.JsonSerializerOptions.Converters.Add(new DatetimeJsonConverter());
                            });
            #endregion

            #region 配置swagger
            services.AddSwaggerGen(c =>
            {
                /*  //注意不能用大写V1，不然老报错，Not Found /swagger/v1/swagger.json
                  c.SwaggerDoc("v1", new OpenApiInfo
                  {
                      Version = "v1",
                      Title = $"{ApiName}v1",
                      //服务描述
                      Description = "A simple example ASP.NET Core Web API",
                      //API服务条款的URL
                      TermsOfService = new Uri("http://tempuri.org/terms"),
                      Contact = new OpenApiContact
                      {
                          Name = "Joe Developer",
                          Email = "joe.developer@tempuri.org"
                      },
                      License = new OpenApiLicense
                      {
                          Name = "Apache 2.0",
                          Url = new Uri("http://www.apache.org/licenses/LICENSE-2.0.html")
                      }

                  });*/

                #region 多版本控制 
                foreach (var item in provider.ApiVersionDescriptions)
                {
                    // 添加文档信息
                    c.SwaggerDoc(item.GroupName, new OpenApiInfo
                    {
                        Title = "CoreWebApi",
                        Version = item.ApiVersion.ToString(),
                        Description = "ASP.NET CORE WebApi",
                        //API服务条款的URL
                        TermsOfService = new Uri("http://tempuri.org/terms"),
                        Contact = new OpenApiContact
                        {
                            Name = "Joe Developer",
                            Email = "joe.developer@tempuri.org"
                        },
                        License = new OpenApiLicense
                        {
                            Name = "Apache 2.0",
                            Url = new Uri("http://www.apache.org/licenses/LICENSE-2.0.html")
                        }
                    });
                }
                #endregion



                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
                {
                    Description = "在下框中输入请求头中需要添加Jwt授权Token：Bearer Token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });

                var baseUrl = AppContext.BaseDirectory;
                //就是这里
                var xmlPath = Path.Combine(baseUrl, "Blog.Core.xml");//这个就是刚刚配置的xml文件名
                c.IncludeXmlComments(xmlPath, true);//默认的第二个参数是false，这个是controller的注释，记得修改

                var xmlModelPath = Path.Combine(baseUrl, "Blog.Core.Model.xml");//这个就是Model层的xml文件名
                c.IncludeXmlComments(xmlModelPath);


            });

            #endregion

            #region 添加验证服务

            // 添加验证服务
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    // 是否开启签名认证
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(TokenHelper.secretKey)),
                    // 发行人验证，这里要和token类中Claim类型的发行人保持一致
                    ValidateIssuer = true,
                    ValidIssuer = "API",//发行人
                    // 接收人验证
                    ValidateAudience = true,
                    ValidAudience = "User",//订阅人
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });
            #endregion

            #region 添加redis缓存
            services.AddSingleton(typeof(ICacheService), new RedisCacheService(new RedisCacheOptions()
            {
                Configuration = Configuration.GetSection("Cache:ConnectionString").Value,
                InstanceName = Configuration.GetSection("Cache:InstanName").Value
            }));

            #endregion

        }




        // 注意在CreateDefaultBuilder中，添加Autofac服务工厂
        public void ConfigureContainer(ContainerBuilder builder)
        {
            //builder.RegisterType<AdvertisementServices>().As<IAdvertisementServices>()
            var cacheType = new List<Type>();
            //业务逻辑层所在程序集命名空间
            Assembly service = Assembly.Load("Blog.Core.Services");
            builder.RegisterAssemblyTypes(service)
                     .AsImplementedInterfaces()
                     .InstancePerDependency()
                     .EnableInterfaceInterceptors()//引用Autofac.Extras.DynamicProxy;
                     .InterceptedBy(cacheType.ToArray());//允许将拦截器服务的列表分配给注册。

            //接口层所在程序集命名空间
            Assembly repository = Assembly.Load("Blog.Core.Repository");
            builder.RegisterAssemblyTypes(repository)
                     .AsImplementedInterfaces()
                     .InstancePerDependency();

            /*  #region 带有接口层的服务注入 
              var cacheType = new List<Type>();
              var basePath = AppContext.BaseDirectory;
              var servicesDllFile = Path.Combine(basePath, "Blog.Core.Services.dll");
              var repositoryDllFile = Path.Combine(basePath, "Blog.Core.Repository.dll");

              if (!(File.Exists(servicesDllFile) && File.Exists(repositoryDllFile)))
              {
                  throw new Exception("Repository.dll和service.dll 丢失，因为项目解耦了，所以需要先F6编译，再F5运行，请检查 bin 文件夹，并拷贝。");
              }

              // 获取 Service.dll 程序集服务，并注册
              var assemblysServices = Assembly.LoadFrom(servicesDllFile);
              builder.RegisterAssemblyTypes(assemblysServices)
                        .AsImplementedInterfaces()
                        .InstancePerDependency()
                        .EnableInterfaceInterceptors()//引用Autofac.Extras.DynamicProxy;
                        .InterceptedBy(cacheType.ToArray());//允许将拦截器服务的列表分配给注册。

              // 获取 Repository.dll 程序集服务，并注册
              var assemblysRepository = Assembly.LoadFrom(repositoryDllFile);
              builder.RegisterAssemblyTypes(assemblysRepository)
                     .AsImplementedInterfaces()
                     .InstancePerDependency();
              #endregion*/

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

            }
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                foreach (var item in provider.ApiVersionDescriptions)
                {
                    //c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoreAPI"); 单版本
                    c.SwaggerEndpoint($"/swagger/{item.GroupName}/swagger.json", "CoreAPI" + item.ApiVersion);
                }
                // c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"{ApiName} v1");

                //路径配置，设置为空，表示直接在根域名（localhost:8001）访问该文件,注意localhost:8001/swagger是访问不到的，去launchSettings.json把launchUrl去掉，如果你想换一个路径，直接写名字即可，比如直接写c.RoutePrefix = "doc";
                c.RoutePrefix = "";
            });

            app.UseRouting();

            app.UseAuthentication();

            // 启用认证中间件
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    /// <summary>
    /// 格式化时间
    /// </summary>
    public class DatetimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                if (DateTime.TryParse(reader.GetString(), out DateTime date))
                    return date;
            }
            return reader.GetDateTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
