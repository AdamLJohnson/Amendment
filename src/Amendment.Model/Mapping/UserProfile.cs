using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amendment.Model.DataModel;
using Amendment.Model.ViewModel.User;
using AutoMapper;

namespace Amendment.Model.Mapping
{
    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDetailsViewModel>();
            CreateMap<User, UserEditViewModel>()
                .ForMember(dest => dest.Password, opt => opt.Ignore());
                //.ForMember(dest => dest.Roles, opt => opt.MapFrom((s,d) => s.UserXRoles?.Select(r => new RoleViewModel(){ Id = r.Role.Id, Name = r.Role.Name, IsSelected = true })));
            CreateMap<UserCreateViewModel, User>()
                .ForMember(dest => dest.Password, opt => opt.Ignore());
            CreateMap<UserEditViewModel, User>()
                //.ForMember(dest => dest.Password, opt => opt.ResolveUsing(src => string.IsNullOrEmpty(src.Password) ? null : src.Password));
                .ForMember(dest => dest.Password, opt => opt.UseDestinationValue());
            //.ForMember(dest => dest.UserXRoles, opt => opt.ResolveUsing(d => d.Roles.Select( r => new UserXRole(){ RoleId = r.Id, UserId = d.Id })));
        }
    }
}
