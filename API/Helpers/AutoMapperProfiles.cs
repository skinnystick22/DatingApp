using System.Linq;
using API.Controllers;
using API.Dtos;
using API.Models;
using AutoMapper;

namespace API.Helpers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<User, UserForListDto>()
            .ForMember(dest => dest.PhotoUrl,
                opt => opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url))
            .ForMember(dest => dest.Age,
                opt => opt.MapFrom(src => src.DateOfBirth.CalculateAge()));

        CreateMap<User, UserForDetailedDto>()
            .ForMember(dest => dest.PhotoUrl,
                opt => opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain).Url))
            .ForMember(dest => dest.Age,
                opt => opt.MapFrom(src => src.DateOfBirth.CalculateAge()));

        CreateMap<Photo, PhotosForDetailedDto>();
        CreateMap<UserForUpdateDto, User>();
        CreateMap<Photo, PhotoForReturnDto>();
        CreateMap<PhotoForCreationDto, Photo>();
        CreateMap<UserForRegisterDto, User>();
        CreateMap<MessageForCreationDto, Message>().ReverseMap();
        CreateMap<Message, MessageToReturnDto>()
            .ForMember(m => m.SenderPhotoUrl,
                options => options.MapFrom(u => u.Sender.Photos.FirstOrDefault(p => p.IsMain).Url))
            .ForMember(m => m.RecipientPhotoUrl,
                options => options.MapFrom(u => u.Recipient.Photos.FirstOrDefault(p => p.IsMain).Url));
    }
}