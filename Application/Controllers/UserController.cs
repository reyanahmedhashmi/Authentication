﻿using System;
using System.Threading.Tasks;
using Application.InputDTOs;
using Identity.Entities;
using Identity.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Application.Controllers
{
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly IRoleService _roleService;
        private readonly ILogger<HomeController> _logger;
        private readonly IAuthenticationService _authenticationService;

        public UserController(ILogger<HomeController> logger, IUserService userService,
            IRoleService roleService, IAuthenticationService authenticationService)
        {
            _userService = userService;
            _logger = logger;
            _roleService = roleService;
            _authenticationService = authenticationService;
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> LoginAsync(LoginDTO loginDTO)
        {
            if (ModelState.IsValid)
            {
                var output = await _userService.ValidateUser(loginDTO.Username, loginDTO.Password);
                if (output.Result)
                {
                    var applicationUser = output.ApplicationUser;
                    var roles = await _roleService.GetRolesByUserId(applicationUser);
                    var token = _authenticationService.GenerateToken(applicationUser, roles);
                    HttpContext.Session.SetString("Token", token.ToString());
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Invalid login!");
            }
            return View(loginDTO);
        }

        [AllowAnonymous]
        public IActionResult Logout()
        {
            _userService.Signout();
            HttpContext.Session.Clear();
            HttpContext.Session.SetString("Token", "");
            return RedirectToAction("Index", "Home");
        }


        [HttpGet("Register")]
        public async Task<IActionResult> Register()
        {
            ViewBag.ListOfRoles = await _roleService.ListOfRoles();
            return View();
        }


        [HttpPost("Register")]
        public async Task<IActionResult> Register(UserRegistration userRegistration)
        {
            ViewBag.ListOfRoles = await _roleService.ListOfRoles();
            if (ModelState.IsValid)
            {
                var identityUser = new ApplicationUser()
                {
                    UserName = userRegistration.Email,
                    Email = userRegistration.Email,
                    FirstName = userRegistration.FirstName,
                    LastName = userRegistration.LastName,
                    IsEnabled = true
                };

                var output = await _userService.Add(identityUser, userRegistration.Password, userRegistration.RoleId);
                if (output.Result)
                {
                    ViewBag.result = "Record Inserted Successfully!";
                    ModelState.Clear();
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in output.Errors)
                {
                    ModelState.AddModelError(error.Code, error.Description);
                }

                return View(userRegistration);
            }
            return View(userRegistration);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<ActionResult> Index()
        {
            var result = await _userService.GetAll();
            return View(result);
        }

        [HttpGet("Delete")]
        public async Task<ActionResult> Delete(Guid? id)
        {
            if (id == null || id.Value == Guid.Empty)
            {
                return View();
            }

            var output = await _userService.Remove(id.ToString());
            if (output.Result)
            {
                ViewBag.result = "Record Inserted Successfully!";
                ModelState.Clear();
                return RedirectToAction("Index");
            }

            foreach (var error in output.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return View();
        }

        [HttpGet("Update")]
        public async Task<ActionResult> Update(string userId)
        {
            var result = await _userService.Get(userId);
            var userResgistration = new UserRegistration()
            {
                Email = result.ApplicationUser.Email,
                FirstName = result.ApplicationUser.FirstName,
                LastName = result.ApplicationUser.LastName,
                Id = result.ApplicationUser.Id
            };
            return View(userResgistration);
        }

        [HttpPost("Update")]
        public async Task<ActionResult> Update(UserRegistration userRegistration)
        {
            var applicationUser = await _userService.Get(userRegistration.Id);
            applicationUser.ApplicationUser.FirstName = userRegistration.FirstName;
            applicationUser.ApplicationUser.LastName = userRegistration.LastName;
            applicationUser.ApplicationUser.Email = userRegistration.Email;
            applicationUser.ApplicationUser.UserName = userRegistration.Email;

            var output = await _userService.Update(applicationUser.ApplicationUser);

            if (output.Result)
            {
                ViewBag.result = "Record Update Successfully!";
                ModelState.Clear();
                return RedirectToAction("Index");
            }

            foreach (var error in output.Errors)
            {
                ModelState.AddModelError(error.Code, error.Description);
            }

            return View(userRegistration);
        }
    }
}