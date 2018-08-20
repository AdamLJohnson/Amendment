using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amendment.Model.DataModel;
using Amendment.Model.ViewModel.User;
using Amendment.Service;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Amendment.Web.Areas.Admin.Controllers
{
    public class UserController : BaseController
    {
        private readonly IUserService _userService;
        private readonly IPasswordHashService _passwordHashService;
        private readonly IMapper _mapper;
        private readonly IRoleService _roleService;

        public UserController(IUserService userService, IPasswordHashService passwordHashService, IMapper mapper, IRoleService roleService)
        {
            _userService = userService;
            _passwordHashService = passwordHashService;
            _mapper = mapper;
            _roleService = roleService;
        }

        // GET: User
        public async Task<ActionResult> Index()
        {
            var users = await _userService.GetAllAsync();
            users = users.OrderBy(u => u.Username).ToList();

            var vmUsers = _mapper.Map<List<UserDetailsViewModel>>(users);

            return View(new UserListViewModel{ Users = vmUsers });
        }

        // GET: User/Create
        public async Task<ActionResult> Create()
        {
            var availableRoles = (await _roleService.GetAllAsync()).Select(r => new RoleViewModel(){ Id = r.Id, Name = r.Name, IsSelected = false }).ToList();
            var model = new UserCreateViewModel { Roles = availableRoles };
            return View(model);
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserCreateViewModel model)
        {
            //Check for duplicates here
            if (!ModelState.IsValid)
            {
                model.Roles = (await _roleService.GetAllAsync()).Select(r => new RoleViewModel() { Id = r.Id, Name = r.Name, IsSelected = model.Roles.Any(ur => ur.Id == r.Id) }).ToList();
                return View(model);
            }

            var user = _mapper.Map<Model.DataModel.User>(model);
            user.UserXRoles = new List<UserXRole>();

            user.UserXRoles.AddRange(model.Roles.Where(sr => sr.IsSelected).Select(o => new UserXRole() { RoleId = o.Id, User = user}));
            user.Password = _passwordHashService.HashPassword(model.Password);

            var result = await _userService.CreateAsync(user, User.UserId());
            if (!result.Succeeded)
            {
                model.Roles = (await _roleService.GetAllAsync()).Select(r => new RoleViewModel() { Id = r.Id, Name = r.Name, IsSelected = model.Roles.Any(ur => ur.Id == r.Id) }).ToList();
                result.Errors.ForEach(e => ModelState.AddModelError("", e));
                return View(model);
            }
                

            return RedirectToAction(nameof(Index));
        }

        // GET: User/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var user = await _userService.GetAsync(id);
            if (user == null)
                return NotFound();

            var vmUser = _mapper.Map<UserEditViewModel>(user);
            vmUser.Roles = (await _roleService.GetAllAsync()).Select(r => new RoleViewModel() { Id = r.Id, Name = r.Name, IsSelected = vmUser.Roles.Any(ur => ur.Id == r.Id) }).ToList();

            return View(vmUser);
        }

        // POST: User/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(int id, UserEditViewModel model)
        {
            var user = await _userService.GetAsync(id);
            if (user == null)
                return NotFound();

            //Check for duplicates here
            if (!ModelState.IsValid)
            {
                model.Roles = (await _roleService.GetAllAsync()).Select(r => new RoleViewModel() { Id = r.Id, Name = r.Name, IsSelected = model.Roles.Any(ur => ur.Id == r.Id) }).ToList();
                return View(model);
            }
            
            int[] addToRoles = model.Roles.Where(r => r.IsSelected && user.UserXRoles.All(ur => ur.RoleId != r.Id)).Select(r => r.Id).ToArray();
            int[] removeFromRoles = model.Roles.Where(r => !r.IsSelected && user.UserXRoles.Any(ur => ur.RoleId == r.Id)).Select(r => r.Id).ToArray();

            user.UserXRoles.AddRange(addToRoles.Select(ar => new UserXRole(){ RoleId = ar }));
            user.UserXRoles.RemoveAll(r => removeFromRoles.Any(rr => rr == r.RoleId));

            user = _mapper.Map(model, user);
            
            if (!string.IsNullOrEmpty(model.Password))
                user.Password = _passwordHashService.HashPassword(model.Password);

            var result = await _userService.UpdateAsync(user, User.UserId());
            if (!result.Succeeded)
            {
                model.Roles = (await _roleService.GetAllAsync()).Select(r => new RoleViewModel() { Id = r.Id, Name = r.Name, IsSelected = model.Roles.Any(ur => ur.Id == r.Id) }).ToList();
                result.Errors.ForEach(e => ModelState.AddModelError("", e));
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: User/Delete/5
        public async Task<ActionResult> Delete(int id)
        {
            var user = await _userService.GetAsync(id);
            if (user == null)
                return NotFound();

            var vmUser = _mapper.Map<UserDetailsViewModel>(user);

            return View(vmUser);
        }

        // POST: User/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Delete(int id, IFormCollection collection)
        {
            var user = await _userService.GetAsync(id);
            if (user == null)
                return NotFound();

            var result = await _userService.DeleteAsync(user, User.UserId());

            return RedirectToAction(nameof(Index));
        }
    }
}