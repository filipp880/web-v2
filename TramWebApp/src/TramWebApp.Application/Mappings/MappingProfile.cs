namespace TramWebApp.Application.Mappings;

using AutoMapper;
using TramWebApp.Domain.Entities;
using TramWebApp.Domain.ValueObjects;
using TramWebApp.Application.DTOs;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<TramRun, TramRunDto>()
            .ForMember(dest => dest.TelemetryPointsCount, opt => opt.MapFrom(src => src.TelemetryPoints.Count))
            .ForMember(dest => dest.InferencesCount, opt => opt.MapFrom(src => src.Inferences.Count));

        CreateMap<TramRun, TramRunListDto>()
            .ForMember(dest => dest.TelemetryPointsCount, opt => opt.MapFrom(src => src.TelemetryPoints.Count));

        CreateMap<CreateTramRunDto, TramRun>()
            .ConstructUsing(src => new TramRun(src.Name, src.Description, Guid.Empty));

        CreateMap<UpdateTramRunDto, TramRun>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.TelemetryPoints, opt => opt.Ignore())
            .ForMember(dest => dest.Inferences, opt => opt.Ignore());

        CreateMap<TramTelemetryPoint, TramTelemetryPointDto>();
        CreateMap<CreateTelemetryPointDto, TramTelemetryPoint>()
            .ConstructUsing((src, ctx) => new TramTelemetryPoint(
                ctx.Items["TramRunId"] is Guid runId ? runId : Guid.Empty,
                src.SequenceNumber,
                src.Timestamp,
                src.ControllerHandlePosition,
                src.FrontBogieSpeed,
                src.RearBogieSpeed,
                src.GpsLatitude,
                src.GpsLongitude,
                src.GpsSpeed));

        CreateMap<ModelInference, ModelInferenceDto>();
        CreateMap<ModelInference, ModelInferenceListDto>();

        CreateMap<ModelInputDto, ModelInput>()
            .ConstructUsing(src => new ModelInput(
                src.ControllerHandlePosition,
                src.FrontBogieSpeed,
                src.RearBogieSpeed,
                src.Timestamp,
                src.GpsLatitude,
                src.GpsLongitude,
                src.GpsSpeed));

        CreateMap<ModelOutput, ModelOutputDto>();
        CreateMap<ModelOutputDto, ModelOutput>();

        CreateMap<MetricsSnapshot, MetricsSnapshotDto>();

        CreateMap<User, UserDto>();
        CreateMap<User, UserProfileDto>();

        CreateMap<RegisterDto, User>()
            .ConstructUsing((src, ctx) => new User(src.Username, src.Email, ctx.Items["PasswordHash"] as string ?? "", UserRole.User));

        CreateMap<LoginResponseDto, LoginResponseDto>();
    }
}