using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            var model = new UserCreateViewModel {AvailableRoles = await _roleService.GetAll()};
            return View(model);
        }

        // POST: User/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _roleService.GetAll();
                return View(model);
            }

            var user = _mapper.Map<Model.DataModel.User>(model);
            user.Password = _passwordHashService.HashPassword(model.Password);

            await _userService.CreateAsync(user);

            return RedirectToAction(nameof(Index));
        }

        // GET: User/Edit/5
        public async Task<ActionResult> Edit(int id)
        {
            var user = await _userService.GetAsync(id);
            if (user == null)
                return NotFound();

            var vmUser = _mapper.Map<UserEditViewModel>(user);
            vmUser.AvailableRoles = await _roleService.GetAll();

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

            if (!ModelState.IsValid)
            {
                model.AvailableRoles = await _roleService.GetAll();
                return View(model);
            }

            user = _mapper.Map(model, user);

            if (!string.IsNullOrEmpty(model.Password))
                user.Password = _passwordHashService.HashPassword(model.Password);

            await _userService.UpdateAsync(user);

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

            await _userService.DeleteAsync(user);

            return RedirectToAction(nameof(Index));
        }
    }
}