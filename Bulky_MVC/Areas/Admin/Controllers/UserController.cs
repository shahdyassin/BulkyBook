
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Bulky_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class UserController : Controller
    {
        
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IUnitOfWork _unit;
        
        public UserController(UserManager<IdentityUser> userManager , RoleManager<IdentityRole> roleManager , IUnitOfWork unit)
        {
            
            _userManager = userManager;
            _roleManager = roleManager;
            _unit = unit;
           
        }
        public IActionResult Index()
        {
           return View();
        }
       
       public IActionResult RoleManagement(string userId)
       {
            

            RoleManagementVM RoleVM = new RoleManagementVM()
            {
                AppUser = _unit.AppUser.Get(u => u.Id == userId , includeProperities:"Company"),
                RoleList = _roleManager.Roles.Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Name
                }),
                CompanyList = _unit.Company.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };

            RoleVM.AppUser.Role = _userManager.GetRolesAsync(_unit.AppUser.
                Get(u => u.Id == userId)).GetAwaiter().GetResult().FirstOrDefault();
            return View(RoleVM);
       }
        [HttpPost]
       public IActionResult RoleManagement(RoleManagementVM roleManagementVM)
       {
            
            string oldRole = _userManager.GetRolesAsync(_unit.AppUser.
                Get(u => u.Id == roleManagementVM.AppUser.Id))
                .GetAwaiter().GetResult().FirstOrDefault();

            AppUser appUser = _unit.AppUser.Get(u => u.Id == roleManagementVM.AppUser.Id);

            if (!(roleManagementVM.AppUser.Role == oldRole))
            {
                // A Role was Updated
                

                if(roleManagementVM.AppUser.Role == SD.Role_Company)
                {
                    appUser.CompanyId = roleManagementVM.AppUser.CompanyId;
                }
                if(oldRole == SD.Role_Company)
                {
                    appUser.CompanyId = null; 
                }
                _unit.AppUser.Update(appUser);
                _unit.Save();


                _userManager.RemoveFromRoleAsync(appUser , oldRole).GetAwaiter().GetResult();
                _userManager.AddToRoleAsync(appUser , roleManagementVM.AppUser.Role).GetAwaiter().GetResult();
            }
            else
            {
                if(oldRole == SD.Role_Company && appUser.CompanyId != roleManagementVM.AppUser.CompanyId)
                {
                    appUser.CompanyId = roleManagementVM.AppUser.CompanyId ;
                    _unit.AppUser.Update(appUser);
                    _unit.Save();
                }
            }

           
           return RedirectToAction("Index");
       }
       
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<AppUser> appUsers = _unit.AppUser.GetAll(includeProperities:"Company").ToList();


            foreach(var user in appUsers)
            {
                
                user.Role = _userManager.GetRolesAsync(user).GetAwaiter().GetResult().FirstOrDefault();


                if(user.Company == null)
                {
                    user.Company = new()
                    {
                        Name = ""
                    };
                }
            }


            return Json(new { data = appUsers });
        }


        [HttpPost]
        public IActionResult LockUnlock([FromBody]string id)
        {

            var objFromDb = _unit.AppUser.Get(u => u.Id == id);

            if(objFromDb == null)
            {
                return Json(new { success = false, message = "Error While Locking/UnLocking" });
            }

            if(objFromDb.LockoutEnd != null && objFromDb.LockoutEnd > DateTime.Now)
            {
                //User is Currently Locked and we need to unlock them
                objFromDb.LockoutEnd = DateTime.Now;

            }
            else
            {
                objFromDb.LockoutEnd = DateTime.Now.AddYears(1000);
            }
            _unit.AppUser.Update(objFromDb);
            _unit.Save();
            return Json(new { success = true, message = "Operation Successful !" });
        }

        #endregion
    }
}
