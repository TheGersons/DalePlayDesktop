using StreamManager.Data.Models;

namespace StreamManager.Services
{
    public static class UserSession
    {
        public static AuthUser? CurrentUser { get; set; }
        public static bool IsAdmin => CurrentUser?.Rol == "admin";
        public static bool IsVendedor => CurrentUser?.Rol == "vendedor";
    }
}