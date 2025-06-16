using ECommerce.DataAccess.Repository;
using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace ECommerce.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ShoppingCartController : Controller
    {
        IUnitOfWork _UnitOfWork { get; set; }
        IEnumerable<ShoppingCart> shoppingCarts { get; set; }
        public ShoppingCartController(IUnitOfWork UnitOfWork)
        {
            _UnitOfWork = UnitOfWork;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCarts = _UnitOfWork.ShoppingCart.GetAll(i=>i.ApplicationUserId==userId,includeProperties: "Product");
            ShoppingCartVM shoppingCartVM = new ShoppingCartVM()
            {
                shoppingCartList = shoppingCarts,
                OrderHeader = new OrderHeader()
            };
            foreach (var item in shoppingCartVM.shoppingCartList)
            {
                item.Price = GetPriceBasedOnQuantity(item);
                shoppingCartVM.OrderHeader.OrderTotal += (item.Price*item.Count);

            }

            return View(shoppingCartVM);
        }

        public IActionResult plus(int cartid)
        {
            var shoppingcart = _UnitOfWork.ShoppingCart.Get(x=> x.Id == cartid);
            shoppingcart.Count++;
            _UnitOfWork.ShoppingCart.Update(shoppingcart);
            _UnitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult minus(int cartid)
        {
            var shoppingcart = _UnitOfWork.ShoppingCart.Get(x=> x.Id == cartid);
            if (shoppingcart.Count <= 0)
            {
                _UnitOfWork.ShoppingCart.Remove(shoppingcart);
            }
            else
            {
                shoppingcart.Count--;
                _UnitOfWork.ShoppingCart.Update(shoppingcart);
            }
            
            _UnitOfWork.Save();
            return RedirectToAction(nameof(Index));
        } 
        
        public IActionResult remove(int cartid)
        {
            var shoppingcart = _UnitOfWork.ShoppingCart.Get(x=> x.Id == cartid,Tracked:true);
            _UnitOfWork.ShoppingCart.Remove(shoppingcart);
            HttpContext.Session.SetInt32(SD.SessionCart, _UnitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == shoppingcart.ApplicationUserId).Count()-1);
            _UnitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCarts = _UnitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == userId, includeProperties: "Product");
            ShoppingCartVM shoppingCartVM = new ShoppingCartVM()
            {
                shoppingCartList = shoppingCarts,
                OrderHeader = new OrderHeader()
            };
            shoppingCartVM.OrderHeader.ApplicationUser = _UnitOfWork.ApplicationUser.Get(u=>u.Id == userId);
            shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
            shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;


            foreach (var item in shoppingCartVM.shoppingCartList)
            {
                item.Price = GetPriceBasedOnQuantity(item);
                shoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);

            }

            return View(shoppingCartVM);


        }
        [HttpPost]
        [ActionName("summary")]
        public IActionResult summaryPost(ShoppingCartVM shoppingCartVM)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
            shoppingCartVM.shoppingCartList = _UnitOfWork.ShoppingCart.GetAll(i => i.ApplicationUserId == userId, includeProperties: "Product");
            shoppingCartVM.OrderHeader.ApplicationUserId = userId;
            shoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ApplicationUser applicationUser = _UnitOfWork.ApplicationUser.Get(u => u.Id == userId);


            foreach (var item in shoppingCartVM.shoppingCartList)
            {
                item.Price = GetPriceBasedOnQuantity(item);
                shoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);

            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                //Customer user
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;

            }
            else
            {
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            _UnitOfWork.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _UnitOfWork.Save();

            foreach (var item in shoppingCartVM.shoppingCartList)
            {
                OrderDetail detail = new OrderDetail
                {
                    ProductId = item.ProductId,
                    OrderHeaderId = shoppingCartVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count,
                };
                _UnitOfWork.OrderDetail.Add(detail);
                _UnitOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {

                var domin = "https://localhost:7241/";
                var options = new SessionCreateOptions
                {
                    //PaymentMethodTypes = new List<string> { "card" },
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = domin + $"Customer/ShoppingCart/OrderConfirmation{shoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domin + "Customer/ShoppingCart/index",
                };

                foreach (var item in shoppingCartVM.shoppingCartList)
                
                    {
                       var sesssionLinItem = new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                UnitAmount = (long)(item.Price * 100),
                                Currency = "usd",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = item.Product.Title,
                                },
                            },
                            Quantity = item.Count,
                        };
                    options.LineItems.Add(sesssionLinItem);

                   }


                    var service = new SessionService();
                    Session session = service.Create(options);
                _UnitOfWork.OrderHeader.UpdateStripPaymentId(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _UnitOfWork.Save();
                 HttpContext.Response.Headers.Add("Location", session.Url);
                    return new StatusCodeResult(303);
                }
            
            return RedirectToAction(nameof(OrderConfirmation), new { id = shoppingCartVM.OrderHeader.Id });

        }

        public IActionResult OrderConfirmation (int id)
        {
            OrderHeader orderHeader = _UnitOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment) // order by Customer User
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _UnitOfWork.OrderHeader.UpdateStripPaymentId(id, session.Id, session.PaymentIntentId);
                    _UnitOfWork.OrderHeader.UpdateStatus(id,SD.StatusApproved, SD.PaymentStatusApproved);
                    _UnitOfWork.Save();
                }
                HttpContext.Session.Clear();

            }

            List<ShoppingCart> shoppingCarts = _UnitOfWork.ShoppingCart.GetAll(u=>u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
            _UnitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _UnitOfWork.Save();

            return View(id);
            }


        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if (shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }
            else if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }
            else
            {
                return shoppingCart.Product.Price100;
            }
        }
    }
}
