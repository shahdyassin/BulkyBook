using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Bulky_MVC.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unit;
        private readonly AppDbContext _context;

        public HomeController(ILogger<HomeController> logger , IUnitOfWork unit , AppDbContext context)
        {
            _logger = logger;
            _unit = unit;
            _context = context;
        }

        public IActionResult Index()
        {
            //var claimsIdentity = (ClaimsIdentity)User.Identity;
            //var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            //if(claim != null)
            //{
            //    HttpContext.Session.SetInt32(SD.SessionCart, _unit.ShoppingCart.GetAll(u => u.AppUserId == claim.Value).Count());
            //}

            IEnumerable<Product> productList = _unit.Product.GetAll(includeProperities:"Category,ProductImages");
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unit.Product.Get(u => u.Id == id, includeProperities: "Category,ProductImages"),
                Count = 1,
                ProductId = id
            };
            
            return View(shoppingCart);
           
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            shoppingCart.AppUserId = userId;

            
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == shoppingCart.ProductId);
            if (product == null)
            {
                return NotFound();
            }

            
            var cartFromDb = _unit.ShoppingCart.Get(u => u.AppUserId == userId && u.ProductId == shoppingCart.ProductId);

            if (cartFromDb != null)
            {
                
                cartFromDb.Count += shoppingCart.Count;
                _unit.ShoppingCart.Update(cartFromDb);
                _unit.Save();
            }
            else
            {
                
                var newCart = new ShoppingCart
                {
                    ProductId = shoppingCart.ProductId,
                    AppUserId = shoppingCart.AppUserId,
                    Product = product,
                    Count = shoppingCart.Count
                };

                await _context.ShoppingCarts.AddAsync(newCart);
                await _context.SaveChangesAsync();
                HttpContext.Session.SetInt32(SD.SessionCart, _unit.ShoppingCart.GetAll(u => u.AppUserId == userId).Count());
            }

            

            TempData["success"] = "Cart updated successfully";
            return RedirectToAction("Index"); 
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
