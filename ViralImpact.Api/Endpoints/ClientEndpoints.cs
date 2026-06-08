using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ViralImpact.Api.Data;
using ViralImpact.Api.Dtos;
using ViralImpact.Api.Entities;
using ViralImpact.Api.Mapping;
using ViralImpact.Api.Services;

namespace ViralImpact.Api.Endpoints;

public static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/client").WithParameterValidation().RequireCors("UnityPolicy");

        group.WithName("GetClient");

        group.MapGet("/", () => Results.Ok(new { message = "Client endpoint is working!" })).WithName("GetClientRoot");

        //POST /client/register

        group.MapPost("/register", async (RegisterDto registerDto, UserManager<User> userManager) =>
        {
            var user = new User
            {
                UserName = registerDto.Username,
                Name = registerDto.Name,
                GroupId = registerDto.GroupId
            };

            var result = await userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Results.BadRequest(new { message = "Registration failed", errors });
            }

            return Results.Created($"/client/register/{user.Id}", new { userId = user.Id, username = user.UserName });
        }).WithName("Register");

        //POST /client/login

        group.MapPost("/login", async (LoginDto loginDto, UserManager<User> userManager, SignInManager<User> signInManager, IJwtTokenService jwtTokenService) =>
        {
            var user = await userManager.FindByNameAsync(loginDto.Username);

            if (user == null)
            {
                return Results.Unauthorized();
            }

            var result = await signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded)
            {
                return Results.Unauthorized();
            }

            var roles = await userManager.GetRolesAsync(user);
            var token = jwtTokenService.GenerateUserToken(user, roles);

            return Results.Ok(new AuthResponseDto(
                user.Id,
                user.UserName!,
                user.Name,
                token
            ));
        }).WithName("Login");

        //POST /client/gamestats

        group.MapPost("/gamestats", async (CreateGameStatsDto gameStatsDto, UsersDbContext dbContext, HttpContext httpContext) =>
        {
            // Validate that the user exists
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            GameStats gameStats = gameStatsDto.ToEntity();
            gameStats.UserId = userId;
            dbContext.GameStats.Add(gameStats);
            await dbContext.SaveChangesAsync();

            return Results.CreatedAtRoute("GetGameStats", new { id = gameStats.Id }, gameStats.ToDto());
        }).WithName("CreateGameStats").RequireAuthorization();

        //GET /client/gamestats/{id}

        group.MapGet("/gamestats/{id}", async (int id, UsersDbContext dbContext) =>
        {
            var gameStats = await dbContext.GameStats.FirstOrDefaultAsync(g => g.Id == id);

            if (gameStats == null)
            {
                return Results.NotFound(new { message = $"GameStats not found: {id}" });
            }

            return Results.Ok(gameStats.ToDto());
        }).WithName("GetGameStats").RequireAuthorization();

        //PUT /client/gamestats/{id}

        group.MapPut("/gamestats/{id}", async (int id, UpdateGameStatsDto updateDto, UsersDbContext dbContext) =>
        {
            var gameStats = await dbContext.GameStats.FirstOrDefaultAsync(g => g.Id == id);

            if (gameStats == null)
            {
                return Results.NotFound(new { message = $"GameStats not found: {id}" });
            }

            gameStats.UpdateFromDto(updateDto);
            dbContext.GameStats.Update(gameStats);
            await dbContext.SaveChangesAsync();

            return Results.Ok(gameStats.ToDto());
        }).WithName("UpdateGameStats").RequireAuthorization();

        //DELETE /client/gamestats/{id}

        group.MapDelete("/gamestats/{id}", async (int id, UsersDbContext dbContext) =>
        {
            var gameStats = await dbContext.GameStats.FirstOrDefaultAsync(g => g.Id == id);

            if (gameStats == null)
            {
                return Results.NotFound(new { message = $"GameStats not found: {id}" });
            }

            dbContext.GameStats.Remove(gameStats);
            await dbContext.SaveChangesAsync();

            return Results.NoContent();
        }).WithName("DeleteGameStats").RequireAuthorization();

        //POST /client/conversations

        group.MapPost("/conversations", async (CreateConversationSessionDto sessionDto, UsersDbContext dbContext, HttpContext httpContext) =>
        {
            // Validate that the user exists
            var userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
            {
                return Results.Unauthorized();
            }

            ConversationSession session = sessionDto.ToEntity();
            session.UserId = userId;
            foreach (var turn in session.Turns)
            {
                turn.ConversationSessionId = session.Id;
            }

            dbContext.ConversationSessions.Add(session);
            await dbContext.SaveChangesAsync();

            return Results.Created($"/client/conversations/{session.Id}", session.ToDto());
        }).WithName("PostConversationSession").RequireAuthorization();

        return group;
    }
}
