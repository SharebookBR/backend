﻿using Microsoft.Extensions.DependencyInjection;
using ShareBook.Repository;
using ShareBook.Repository.Infra;
using ShareBook.Service;

namespace ShareBook.Api.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            // Services
            services.AddScoped<IBookService, BookService>();
            services.AddScoped<IUserService, UserService>();

            // Repositories
            services.AddScoped<IBookRepository, BookRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            // UnitOfWork
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}