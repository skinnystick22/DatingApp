﻿using System;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Data;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace API.Helpers;

public class LogUserActivity : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();
        var userId = int.Parse(resultContext.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value);
        var repository = resultContext.HttpContext.RequestServices.GetService<IDatingRepository>();
        var user = await repository.GetUser(userId, true);
        user.LastActive = DateTime.Now;
        await repository.SaveAll();
    }
}