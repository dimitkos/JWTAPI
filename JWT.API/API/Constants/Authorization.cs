namespace API.Constants
{
    public class Authorization
    {
        public enum Roles
        {
            Administrator,
            Moderator,
            User
        }
        public const string defaultUsername = "user";
        public const string defaultEmail = "user@secureapi.com";
        public const string defaultPassword = "Pa$$w0rd.";
        public const Roles defaultRole = Roles.User;
    }
}
