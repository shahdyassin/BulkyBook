
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
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unit;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductController(IUnitOfWork unit , IWebHostEnvironment webHostEnvironment)
        {
            _unit = unit;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            List<Product> product = _unit.Product.GetAll(includeProperities:"Category").ToList();
            
            return View(product);
        }
        public IActionResult Upsert(int? id) //Update & Insert
        {
            
            //ViewBag.CategoryList = CategoryList;    
            //ViewData["CategoryList"] = CategoryList;
            ProductVM productVM = new()
            {
                CategoryList = _unit.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                }),
                Product = new Product()
            };
            if(id == null || id == 0)
            {
                //Create
                return View(productVM);
            }
            else
            {
                //Update 
                productVM.Product = _unit.Product.Get(u => u.Id == id , includeProperities: "ProductImages");
                return View(productVM);
            }
            
        }
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM , List<IFormFile> files)
        {
           
            if (ModelState.IsValid)
            {

                if (productVM.Product.Id == 0)
                {
                    _unit.Product.Add(productVM.Product);
                }
                else
                {
                    _unit.Product.Update(productVM.Product);
                }

                _unit.Save();
                string wwwRootpath = _webHostEnvironment.WebRootPath;
                if(files != null)
                {

                    foreach(IFormFile file in files)
                    {
                        string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string productPath = @"Images\Products\Product-" + productVM.Product.Id;
                        string finalPath = Path.Combine(wwwRootpath, productPath);


                        if(!Directory.Exists(finalPath)) 
                            Directory.CreateDirectory(finalPath);


                        using (var fileStream = new FileStream(Path.Combine(finalPath, fileName), FileMode.Create))
                        {
                            file.CopyTo(fileStream);
                        }

                        ProductImage productImage = new()
                        {
                            ImageUrl = @"\" + productPath + @"\" + fileName,
                            ProductId = productVM.Product.Id,
                        };

                        if(productVM.Product.ProductImages == null)
                        {
                            productVM.Product.ProductImages = new List<ProductImage>();
                        }

                        productVM.Product.ProductImages.Add(productImage);
                        
                    }
                    _unit.Product.Update(productVM.Product);
                    _unit.Save();




                    //if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    //{
                    //    //delete the old image
                    //    var oldImagePath = Path.Combine(wwwRootpath , productVM.Product.ImageUrl.TrimStart('\\'));

                    //    if (System.IO.File.Exists(oldImagePath))
                    //    {
                    //        System.IO.File.Delete(oldImagePath);
                    //    }
                    //}

                    //using(var fileStream = new FileStream(Path.Combine(productPath , fileName) , FileMode.Create))
                    //{
                    //    file.CopyTo(fileStream);
                    //}

                    //productVM.Product.ImageUrl = @"\Images\Product\" + fileName;
                }
               
                TempData["Success"] = "Product Created/Updated Successfully";
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unit.Category.GetAll().Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString(),
                });
                    
                return View(productVM);
            }
            

        }
       
        public IActionResult DeleteImage(int imageId)
        {
            var imageTobeDeleted = _unit.ProductImage.Get(u => u.Id == imageId);
            int productId = imageTobeDeleted.ProductId;
            if(imageTobeDeleted != null)
            {
                if (!string.IsNullOrEmpty(imageTobeDeleted.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageTobeDeleted.ImageUrl.TrimStart('\\'));

                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }
                _unit.ProductImage.Remove(imageTobeDeleted);
                _unit.Save();

                TempData["success"] = "Deleted Successfully !";
            }
            return RedirectToAction("Upsert" ,new {id = productId });
        }


        #region API CALLS
        [HttpGet]
        public IActionResult GetAll()
        {
            List<Product> product = _unit.Product.GetAll(includeProperities: "Category").ToList();
            return Json(new { data = product });
        }


        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var productToBeDeleted = _unit.Product.Get(u => u.Id == id);
            if (productToBeDeleted == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            //var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productToBeDeleted.ImageUrl.TrimStart('\\'));

            //if (System.IO.File.Exists(oldImagePath))
            //{
            //    System.IO.File.Delete(oldImagePath);
            //}
            
            string productPath = @"Images\Products\Product-" + id;
            string finalPath = Path.Combine(_webHostEnvironment.WebRootPath, productPath);


            if (Directory.Exists(finalPath))
            {
                string[] filePaths = Directory.GetFiles(finalPath);
                foreach(string filePath in filePaths)
                {
                    System.IO.File.Delete(filePath);
                }
                Directory.Delete(finalPath);
            }

            _unit.Product.Remove(productToBeDeleted);
            _unit.Save();

            return Json(new { success = true, message = "Deleted Successfully !" });
        }

        #endregion
    }
}
