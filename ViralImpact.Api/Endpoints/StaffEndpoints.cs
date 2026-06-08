using Microsoft.AspNetCore.Identity;
using ViralImpact.Api.Dtos;
using ViralImpact.Api.Entities;
using ViralImpact.Api.Services;

namespace ViralImpact.Api.Endpoints;

public static class StaffEndpoints
{
    private static readonly string[] ValidRoles = ["Admin", "Scientist", "Caretaker"];

    public static RouteGroupBuilder MapStaffEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/staff")
            .WithParameterValidation()
            .RequireCors("DashboardPolicy");

        // POST /staff/login
        group.MapPost("/login", async (
            LoginDto loginDto,
            UserManager<Caretaker> userManager,
            IJwtTokenService jwtTokenService) =>
        {
            var user = await userManager.FindByNameAsync(loginDto.Username);
            if (user == null)
                return Results.Unauthorized();

            var validPassword = await userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!validPassword)
                return Results.Unauthorized();

            var token = jwtTokenService.GenerateCaretakerToken(user, [user.StaffRole]);

            return Results.Ok(new AuthResponseDto(user.Id, user.UserName!, user.Name, token));
        }).WithName("StaffLogin").AllowAnonymous();

        // POST /staff/register  —  Admin only
        group.MapPost("/register", async (
            StaffRegisterDto dto,
            HttpContext httpContext,
            UserManager<Caretaker> userManager) =>
        {
            var callerRole = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
            if (callerRole != "Admin")
                return Results.Forbid();

            if (!ValidRoles.Contains(dto.StaffRole))
                return Results.BadRequest(new { message = $"Invalid role. Must be one of: {string.Join(", ", ValidRoles)}" });

            if (dto.StaffRole == "Caretaker" && string.IsNullOrWhiteSpace(dto.GroupId))
                return Results.BadRequest(new { message = "GroupId is required for Caretaker role." });

            var existing = await userManager.FindByNameAsync(dto.Username);
            if (existing != null)
                return Results.Conflict(new { message = "Username already exists." });

            var staff = new Caretaker
            {
                UserName = dto.Username,
                Name = dto.Name,
                StaffRole = dto.StaffRole,
                GroupId = dto.StaffRole == "Caretaker" ? dto.GroupId : null
            };

            var result = await userManager.CreateAsync(staff, dto.Password);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return Results.BadRequest(new { message = "Registration failed.", errors });
            }

            return Results.Created($"/staff/{staff.Id}", new { staff.Id, staff.UserName, staff.StaffRole });
        }).WithName("StaffRegister").RequireAuthorization();

        return group;
    }
}
