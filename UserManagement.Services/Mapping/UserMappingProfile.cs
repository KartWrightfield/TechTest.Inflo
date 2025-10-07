using AutoMapper;
using UserManagement.Data.Entities;
using UserManagement.Shared.Models.Users;

namespace UserManagement.Services.Mapping;

public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<UserInputViewModel, User>()
            .ForMember(dest => dest.DateOfBirth, opt =>
                opt.MapFrom(src => src.DateOfBirth));

        CreateMap<User, UserViewModel>();
        CreateMap<User, UserListItemViewModel>();
        CreateMap<UserViewModel, UserInputViewModel>();
    }
}
