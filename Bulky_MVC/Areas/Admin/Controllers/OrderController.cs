using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace Bulky_MVC.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unit;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public IActionResult Index()
        {
            return View();
        } 
        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader = _unit.OrderHeader.Get(u => u.Id == orderId, includeProperities: "AppUser"),
                OrderDetail = _unit.OrderDetail.GetAll(u => u.OrderHeader.Id == orderId, includeProperities: "Product")
            };
            return View(OrderVM);
        }
        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
           var orderHeaderFromDb = _unit.OrderHeader.Get(u=> u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            } 
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            _unit.OrderHeader.Update(orderHeaderFromDb);
            _unit.Save();

            TempData["success"] = "Order Details Updated Successfully!";
            return RedirectToAction("Details" , new {orderId = orderHeaderFromDb.Id});
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unit.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id , SD.StatusInProcess);
            _unit.Save();
            TempData["success"] = "Order Is Processing";
            return RedirectToAction("Details", new { orderId = OrderVM.OrderHeader.Id });
        } 
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unit.OrderHeader.Get(u=> u.Id == OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unit.OrderHeader.Update(orderHeader);
            _unit.Save();

            _unit.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id , SD.StatusShipped);
            _unit.Save();
            TempData["success"] = "Order Shipped Successfully!";
            return RedirectToAction("Details", new { orderId = OrderVM.OrderHeader.Id });
        }
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unit.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId

                };

                var service = new RefundService();
                Refund refund = service.Create(options);


                _unit.OrderHeader.UpdateStatus(orderHeader.Id , SD.StatusCancelled , SD.StatusRefunded);
            }
            else
            {
                _unit.OrderHeader.UpdateStatus(orderHeader.Id , SD.StatusCancelled , SD.StatusCancelled);

            }
            _unit.Save();
            TempData["success"] = "Order Cancelled Successfully!";
            return RedirectToAction("Details", new { orderId = OrderVM.OrderHeader.Id });
        }

        [ActionName("Details")]
        [HttpPost]
        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unit.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperities: "AppUser");
            OrderVM.OrderDetail = _unit.OrderDetail.GetAll(u => u.Id == OrderVM.OrderHeader.Id, includeProperities: "Product");

            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"Admin/Order/details?orderId={OrderVM.OrderHeader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderVM.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count
                };
                options.LineItems.Add(sessionLineItem);
            }
            var service = new SessionService();
            Session session = service.Create(options);

            _unit.OrderHeader.UpdateStripePaymentId(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unit.Save();

            Response.Headers.Add("Location", session.Url);

            return new StatusCodeResult(303);
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unit.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //Order By Company
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unit.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _unit.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _unit.Save();
                }
            }

           
            return View(orderHeaderId);
        }

        #region API CALLS
        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> orders;


            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                orders = _unit.OrderHeader.GetAll(includeProperities: "AppUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

                orders = _unit.OrderHeader.GetAll(u => u.AppUserId == userId, includeProperities: "AppUser");
            }



            switch (status)
            {
                case "pending":
                    orders = orders.Where(u=> u.PaymentStatus==SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    orders = orders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    orders = orders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    orders = orders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                   
                    break;

            }



            return Json(new { data = orders });
        }


       

        #endregion
    }
}
