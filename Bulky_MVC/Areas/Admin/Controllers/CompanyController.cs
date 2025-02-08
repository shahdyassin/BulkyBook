
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bulky_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller
    {
        private readonly IUnitOfWork _unit;
        
        public CompanyController(IUnitOfWork unit)
        {
            _unit = unit;
           
        }
        public IActionResult Index()
        {
            List<Company> companies = _unit.Company.GetAll().ToList();
            
            return View(companies);
        }
        public IActionResult Upsert(int? id) //Update & Insert
        {
            
            //ViewBag.CategoryList = CategoryList;    
            //ViewData["CategoryList"] = CategoryList;
            
            if(id == null || id == 0)
            {
                //Create
                return View(new Company());
            }
            else
            {
                //Update 
                Company company = _unit.Company.Get(u => u.Id == id);
                return View(company);
            }
            
        }
        [HttpPost]
        public IActionResult Upsert(Company company)
        {
           
            if (ModelState.IsValid)
            {
                
               
                if(company.Id == 0)
                {
                    _unit.Company.Add(company);
                }
                else
                {
                    _unit.Company.Update(company);
                }
                
                _unit.Save();
                TempData["Success"] = "Company Created Successfully";
                return RedirectToAction("Index");
            }
            else
            {
               
                    
                return View(company);
            }
            

        }
       
       
        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Company> companies = _unit.Company.GetAll().ToList();
            return Json(new { data = companies });
        }


        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var companyToBeDeleted = _unit.Company.Get(u => u.Id == id);
            if (companyToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

           

            _unit.Company.Remove(companyToBeDeleted);
            _unit.Save();

            return Json(new { success = true, message = "Deleted Successfully !" });
        }

        #endregion
    }
}
