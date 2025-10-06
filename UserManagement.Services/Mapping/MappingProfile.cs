using AutoMapper;
using UserManagement.Data.Entities;
using UserManagement.Shared.Models.Users;

namespace UserManagement.Services.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<UserInputViewModel, User>()
            .ForMember(dest => dest.DateOfBirth, opt =>
                opt.MapFrom(src => src.DateOfBirth));

        CreateMap<User, UserViewModel>();
        CreateMap<User, UserListItemViewModel>();
        CreateMap<UserViewModel, UserInputViewModel>();
    }
}
