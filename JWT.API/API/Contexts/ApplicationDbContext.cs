using API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Contexts
{

    //IdentityDbContext<ApplicationUser> will let you use your own ApplicationUser class as the User entity. 
    //I.e. you can have custom properties on your users. 
    //The IdentityDbContext class inherits from IdentityDbContext<IdentityUser> which means you will have to use the IdentityUser class for your users.
    //If you want to have more properties on your user objects than the few properties that IdentityUser provide(UserName, PasswordHash and a few more)
    //then you may want to choose IdentityDbContext<ApplicationUser>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
    }
}
