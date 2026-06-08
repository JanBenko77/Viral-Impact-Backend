using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using ViralImpact.Api.Data;
using ViralImpact.Api.Mapping;

namespace ViralImpact.Api.Endpoints;

public static class DashboardEndpoints
{
    public static RouteGroupBuilder MapDashboardEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/dashboard")
            .WithParameterValidation()
            .RequireAuthorization()
            .RequireCors("DashboardPolicy");

        // GET /dashboard/users
        // Admin and Scientist see everyone; Caretaker sees only their group.
        group.MapGet("/users", async (HttpContext httpContext, UsersDbContext db) =>
        {
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var groupId = httpContext.User.FindFirst("GroupId")?.Value;

            var query = db.Users.AsQueryable();

            if (role == "Caretaker")
            {
                if (string.IsNullOrEmpty(groupId))
                    return Results.Forbid();
                query = query.Where(u => u.GroupId == groupId);
            }

            var users = await query.Select(u => u.ToDto()).ToListAsync();
            return Results.Ok(users);
        }).WithName("GetDashboardUsers");

        // GET /dashboard/users/{id}
        group.MapGet("/users/{id}", async (string id, HttpContext httpContext, UsersDbContext db) =>
        {
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var groupId = httpContext.User.FindFirst("GroupId")?.Value;

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return Results.NotFound(new { message = $"User not found: {id}" });

            if (role == "Caretaker" && user.GroupId != groupId)
                return Results.Forbid();

            return Results.Ok(user.ToDto());
        }).WithName("GetDashboardUser");

        // GET /dashboard/users/{id}/stats
        group.MapGet("/users/{id}/stats", async (string id, HttpContext httpContext, UsersDbContext db) =>
        {
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var groupId = httpContext.User.FindFirst("GroupId")?.Value;

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return Results.NotFound(new { message = $"User not found: {id}" });

            if (role == "Caretaker" && user.GroupId != groupId)
                return Results.Forbid();

            var stats = await db.GameStats
                .Where(s => s.UserId == id)
                .Select(s => s.ToDto())
                .ToListAsync();

            return Results.Ok(stats);
        }).WithName("GetDashboardUserStats");

        // GET /dashboard/users/{id}/conversations
        group.MapGet("/users/{id}/conversations", async (string id, HttpContext httpContext, UsersDbContext db) =>
        {
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            var groupId = httpContext.User.FindFirst("GroupId")?.Value;

            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
                return Results.NotFound(new { message = $"User not found: {id}" });

            if (role == "Caretaker" && user.GroupId != groupId)
                return Results.Forbid();

            var conversations = await db.ConversationSessions
                .Include(c => c.Turns)
                .Where(c => c.UserId == id)
                .Select(c => c.ToDto())
                .ToListAsync();

            return Results.Ok(conversations);
        }).WithName("GetDashboardUserConversations");

        // GET /dashboard/groups  —  Admin and Scientist only
        group.MapGet("/groups", async (HttpContext httpContext, UsersDbContext db) =>
        {
            var role = httpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            if (role == "Caretaker")
                return Results.Forbid();

            var groups = await db.Users
                .Select(u => u.GroupId)
                .Distinct()
                .OrderBy(g => g)
                .ToListAsync();

            return Results.Ok(groups);
        }).WithName("GetDashboardGroups");

        return group;
    }
}
