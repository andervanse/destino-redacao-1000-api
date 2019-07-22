using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace destino_redacao_1000_api
{
    public class Startup
    {
        public const string AppS3BucketKey = "Website:S3Bucket";
        public static IConfiguration Configuration { get; private set; }
        private readonly ILogger<Startup> _logger;

        public Startup(IConfiguration configuration, ILogger<Startup> logger)
        {
            Configuration = configuration;
            _logger = logger;
        }

        // This method gets called by the runtime. Use this method to add services to the container
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<DynamoDbContext>();
            services.AddScoped<IEmailSender, ZohoEmailSender>();
            services.AddScoped<IEmailLoginConfirmation, EmailLoginConfirmation>();
            services.AddScoped<IUsuarioRepository, UsuarioRepository>();
            services.AddScoped<IRevisaoRepository, RevisaoRepository>();
            services.AddScoped<IUploadFile, AWSUploadFile>();
            services.AddSingleton<IConfiguration>(x => Configuration);            

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(opt => {
                        opt.TokenValidationParameters = new TokenValidationParameters {
                            ClockSkew = TimeSpan.FromHours(6),
                            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Token:Key"])),
                            ValidateAudience = true,
                            ValidAudience = Configuration["Token:Audience"],
                            ValidateIssuer = true,
                            ValidIssuer = Configuration["Token:Issuer"],
                            RequireSignedTokens = true,                            
                            RequireExpirationTime = true,
                            ValidateLifetime = true
                        };
                    });
                                
            services.AddCors();
            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                    .AddJsonOptions(options => {
                        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                        options.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                    });

            // Add S3 to the ASP.NET Core dependency injection framework.
            services.AddAWSService<Amazon.S3.IAmazonS3>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.ConfigureExceptionHandler(_logger);
            app.UseCors(builder =>
                builder.WithOrigins(Configuration["Website:CorsOrigin"])
                       .AllowAnyMethod()
                       .AllowAnyHeader());            
            app.UseAuthentication();
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
