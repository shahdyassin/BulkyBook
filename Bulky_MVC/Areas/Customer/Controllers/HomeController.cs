using System.Diagnostics;
using System.Security.Claims;
using Bulky.DataAccess.Repository;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bulky_MVC.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unit;

        public HomeController(ILogger<HomeController> logger , IUnitOfWork unit)
        {
            _logger = logger;
            _unit = unit;
        }

        public IActionResult Index()
        {
            IEnumerable<Product> productList = _unit.Product.GetAll(includeProperities:"Category");
            return View(productList);
        }
        public IActionResult Details(int id)
        {
            ShoppingCart shoppingCart = new()
            {
                Product = _unit.Product.Get(u => u.Id == id, includeProperities: "Category"),
                Count = 1,
                ProductId = id
            };
            
            return View(shoppingCart);
           
        }

        [HttpPost]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCart.AppUserId = userId;

            //ShoppingCart cartFromDb = _unit.ShoppingCart.Get(u => u.AppUserId == userId &&
            //u.ProductId == shoppingCart.ProductId);

            //if (cartFromDb != null)
            //{
            //    //shopping cart exists
            //    cartFromDb.Count += shoppingCart.Count;
            //    _unit.ShoppingCart.Update(cartFromDb);
            //    // _unit.Save();
            //}
            //else
            //{
            //    //add cart record
               _unit.ShoppingCart.Add(shoppingCart);
               _unit.Save();

            //}
            //TempData["success"] = "Cart updated successfully";

            return View() /*RedirectToAction(nameof(Index))*/;
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
