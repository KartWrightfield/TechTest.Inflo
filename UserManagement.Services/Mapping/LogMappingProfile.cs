using AutoMapper;
using UserManagement.Data.Entities;
using UserManagement.Shared.Models.Logs;

namespace UserManagement.Services.Mapping;

public class LogMappingProfile : Profile
{
    public LogMappingProfile()
    {
        CreateMap<Log, LogViewModel>();
        CreateMap<Log, LogDetailViewModel>();
    }
}
