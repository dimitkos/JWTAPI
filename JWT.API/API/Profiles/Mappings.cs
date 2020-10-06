using API.Models;
using AutoMapper;

namespace API.Profiles
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            CreateMap<RegisterModel, ApplicationUser>();
        }
    }
}
