using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;
using System.Reflection;

namespace DomusMercatorisDotnetRest.Infrastructure
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddDomusSwagger(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "DomusMercatoris API", Version = "v1" });
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
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Please enter token",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "bearer"
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
                    }
                });
            });
            return services;
        }
    }
}
