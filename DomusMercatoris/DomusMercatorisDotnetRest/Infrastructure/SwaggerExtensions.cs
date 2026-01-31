using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using DomusMercatorisDotnetRest.Authentication;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DomusMercatorisDotnetRest.Infrastructure
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddDomusSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "DomusMercatoris API", Version = "v1" });
                c.SwaggerDoc("apikey", new OpenApiInfo { Title = "DomusMercatoris API Key Access", Version = "v1" });

                var xmlFiles = new[]
                {
                    $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
                    "DomusMercatoris.Service.xml",
                    "DomusMercatoris.Core.xml"
                };
                foreach (var xml in xmlFiles)
                {
                    var p = Path.Combine(AppContext.BaseDirectory, xml);
                    if (File.Exists(p))
                    {
                        c.IncludeXmlComments(p, includeControllerXmlComments: true);
                    }
                }

                // JWT Security
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Please enter token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
                });

                // API Key Security
                c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    Description = "Enter your API Key",
                    Name = ApiKeyAuthenticationHandler.HeaderName,
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Filter documents
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (docName == "v1") return true; // Show all in default
                    
                    if (docName == "apikey")
                    {
                        if (apiDesc.TryGetMethodInfo(out MethodInfo methodInfo))
                        {
                            return methodInfo.GetCustomAttributes(typeof(ApiKeyAttribute), true).Any() ||
                                   (methodInfo.DeclaringType?.GetCustomAttributes(typeof(ApiKeyAttribute), true).Any() == true);
                        }
                    }
                    return false;
                });
            });
            return services;
        }
    }
}
