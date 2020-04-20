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
        /// Api�汾��Ϣ
        /// </summary>
        private IApiVersionDescriptionProvider provider;
        public IConfiguration Configuration { get; }

        private string ApiName = "����Api";
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            #region �汾����
            services.AddApiVersioning(option =>
            {
                // ��ѡ��ΪtrueʱAPI����֧�ֵİ汾��Ϣ
                option.ReportApiVersions = true;
                // ���ṩ�汾ʱ��Ĭ��Ϊ1.0
                option.AssumeDefaultVersionWhenUnspecified = true;
                // ������δָ���汾ʱĬ��Ϊ1.0
                option.DefaultApiVersion = new ApiVersion(1, 0);
            }).AddVersionedApiExplorer(option =>
            {
                // �汾���ĸ�ʽ��v+�汾��
                option.GroupNameFormat = "'v'V";
                option.AssumeDefaultVersionWhenUnspecified = true;
            });

            this.provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();
            #endregion

            #region ��ʽ��ʱ��
            services.AddControllers()
                            .AddJsonOptions(configure =>
                            {
                                configure.JsonSerializerOptions.Converters.Add(new DatetimeJsonConverter());
                            });
            #endregion

            #region ����swagger
            services.AddSwaggerGen(c =>
            {
                /*  //ע�ⲻ���ô�дV1����Ȼ�ϱ���Not Found /swagger/v1/swagger.json
                  c.SwaggerDoc("v1", new OpenApiInfo
                  {
                      Version = "v1",
                      Title = $"{ApiName}v1",
                      //��������
                      Description = "A simple example ASP.NET Core Web API",
                      //API���������URL
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

                #region ��汾���� 
                foreach (var item in provider.ApiVersionDescriptions)
                {
                    // ����ĵ���Ϣ
                    c.SwaggerDoc(item.GroupName, new OpenApiInfo
                    {
                        Title = "CoreWebApi",
                        Version = item.ApiVersion.ToString(),
                        Description = "ASP.NET CORE WebApi",
                        //API���������URL
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
                    Description = "���¿�����������ͷ����Ҫ���Jwt��ȨToken��Bearer Token",
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
                //��������
                var xmlPath = Path.Combine(baseUrl, "Blog.Core.xml");//������Ǹո����õ�xml�ļ���
                c.IncludeXmlComments(xmlPath, true);//Ĭ�ϵĵڶ���������false�������controller��ע�ͣ��ǵ��޸�

                var xmlModelPath = Path.Combine(baseUrl, "Blog.Core.Model.xml");//�������Model���xml�ļ���
                c.IncludeXmlComments(xmlModelPath);


            });

            #endregion

            #region �����֤����

            // �����֤����
            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(o =>
            {
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    // �Ƿ���ǩ����֤
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(TokenHelper.secretKey)),
                    // ��������֤������Ҫ��token����Claim���͵ķ����˱���һ��
                    ValidateIssuer = true,
                    ValidIssuer = "API",//������
                    // ��������֤
                    ValidateAudience = true,
                    ValidAudience = "User",//������
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });
            #endregion

            #region ���redis����
            services.AddSingleton(typeof(ICacheService), new RedisCacheService(new RedisCacheOptions()
            {
                Configuration = Configuration.GetSection("Cache:ConnectionString").Value,
                InstanceName = Configuration.GetSection("Cache:InstanName").Value
            }));

            #endregion

        }




        // ע����CreateDefaultBuilder�У����Autofac���񹤳�
        public void ConfigureContainer(ContainerBuilder builder)
        {
            //builder.RegisterType<AdvertisementServices>().As<IAdvertisementServices>()
            var cacheType = new List<Type>();
            //ҵ���߼������ڳ��������ռ�
            Assembly service = Assembly.Load("Blog.Core.Services");
            builder.RegisterAssemblyTypes(service)
                     .AsImplementedInterfaces()
                     .InstancePerDependency()
                     .EnableInterfaceInterceptors()//����Autofac.Extras.DynamicProxy;
                     .InterceptedBy(cacheType.ToArray());//����������������б�����ע�ᡣ

            //�ӿڲ����ڳ��������ռ�
            Assembly repository = Assembly.Load("Blog.Core.Repository");
            builder.RegisterAssemblyTypes(repository)
                     .AsImplementedInterfaces()
                     .InstancePerDependency();

            /*  #region ���нӿڲ�ķ���ע�� 
              var cacheType = new List<Type>();
              var basePath = AppContext.BaseDirectory;
              var servicesDllFile = Path.Combine(basePath, "Blog.Core.Services.dll");
              var repositoryDllFile = Path.Combine(basePath, "Blog.Core.Repository.dll");

              if (!(File.Exists(servicesDllFile) && File.Exists(repositoryDllFile)))
              {
                  throw new Exception("Repository.dll��service.dll ��ʧ����Ϊ��Ŀ�����ˣ�������Ҫ��F6���룬��F5���У����� bin �ļ��У���������");
              }

              // ��ȡ Service.dll ���򼯷��񣬲�ע��
              var assemblysServices = Assembly.LoadFrom(servicesDllFile);
              builder.RegisterAssemblyTypes(assemblysServices)
                        .AsImplementedInterfaces()
                        .InstancePerDependency()
                        .EnableInterfaceInterceptors()//����Autofac.Extras.DynamicProxy;
                        .InterceptedBy(cacheType.ToArray());//����������������б�����ע�ᡣ

              // ��ȡ Repository.dll ���򼯷��񣬲�ע��
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
                    //c.SwaggerEndpoint("/swagger/v1/swagger.json", "CoreAPI"); ���汾
                    c.SwaggerEndpoint($"/swagger/{item.GroupName}/swagger.json", "CoreAPI" + item.ApiVersion);
                }
                // c.SwaggerEndpoint($"/swagger/v1/swagger.json", $"{ApiName} v1");

                //·�����ã�����Ϊ�գ���ʾֱ���ڸ�������localhost:8001�����ʸ��ļ�,ע��localhost:8001/swagger�Ƿ��ʲ����ģ�ȥlaunchSettings.json��launchUrlȥ����������뻻һ��·����ֱ��д���ּ��ɣ�����ֱ��дc.RoutePrefix = "doc";
                c.RoutePrefix = "";
            });

            app.UseRouting();

            app.UseAuthentication();

            // ������֤�м��
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }

    /// <summary>
    /// ��ʽ��ʱ��
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
