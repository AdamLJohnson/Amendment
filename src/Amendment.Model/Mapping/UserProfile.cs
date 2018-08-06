﻿using System;
using System.Collections.Generic;
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
            CreateMap<UserEditViewModel, User>()
                //.ForMember(dest => dest.Password, opt => opt.ResolveUsing(src => string.IsNullOrEmpty(src.Password) ? null : src.Password));
                .ForMember(dest => dest.Password, opt => opt.Ignore());
        }
    }
}
