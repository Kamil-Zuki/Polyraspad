using Grpc.Core;
using System.Security.Claims;

namespace AgentService.Helpers;

public static class GrpcContextHelper
{
    public static Guid GetUserId(ServerCallContext context)
    {
        var userIdHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == "user_id");
        if (userIdHeader != null && Guid.TryParse(userIdHeader.Value, out var userId))
            return userId;

        var userIdClaim = context.GetHttpContext()?.User?.FindFirst("user_id")?.Value
            ?? context.GetHttpContext()?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (userIdClaim != null && Guid.TryParse(userIdClaim, out var userIdFromClaim))
            return userIdFromClaim;

        throw new RpcException(new Status(StatusCode.Unauthenticated, "User ID not found in request context"));
    }

    public static List<string> GetRoles(ServerCallContext context)
    {
        var roles = new List<string>();

        var rolesHeader = context.RequestHeaders.FirstOrDefault(h => h.Key == "roles");
        if (rolesHeader != null)
            roles.AddRange(rolesHeader.Value.Split(',', StringSplitOptions.RemoveEmptyEntries));

        var roleClaims = context.GetHttpContext()?.User?.FindAll(ClaimTypes.Role);
        if (roleClaims != null)
            roles.AddRange(roleClaims.Select(c => c.Value));

        return roles.Distinct().ToList();
    }
}
