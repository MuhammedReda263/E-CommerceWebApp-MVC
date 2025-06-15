using ECommerce.DataAccess.Repository;
using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace ECommerce.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork ;
        public OrderController(IUnitOfWork UnitOfWork)
        {
            _UnitOfWork = UnitOfWork ;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            List<OrderHeader> objOrderHeaders;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Company))
            {
                objOrderHeaders = _UnitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var ClaimIdentity = (ClaimsIdentity) User.Identity;
                var userId = ClaimIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                objOrderHeaders = _UnitOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId==userId,includeProperties: "ApplicationUser").ToList();

            }

            switch (status)
			{
				case "pending":
					objOrderHeaders = objOrderHeaders.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment).ToList();
					break;
				case "inprocess":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess).ToList();
					break;
				case "completed":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped).ToList();
					break;
				case "approved":
					objOrderHeaders = objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved).ToList();
					break;
				default:
					break;
			}
			return Json(new { data = objOrderHeaders });
        }

		public ActionResult Detail(int orderId)
		{
			OrderVM orderVM = new OrderVM
			{
				OrderHeader = _UnitOfWork.OrderHeader.Get(U => U.Id == orderId,includeProperties:"ApplicationUser"),
				OrderDetail = _UnitOfWork.OrderDetail.GetAll(U=>U.OrderHeaderId == orderId,includeProperties : "Product")
			};

			return View(orderVM);

		}


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail(OrderVM OrderVM)
        {
            var orderHeaderFromDb = _UnitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            //orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }

            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }

            _UnitOfWork.OrderHeader.Update(orderHeaderFromDb);
            _UnitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Detail), new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]

        public IActionResult StartProcessing (OrderVM orderVM)
        {
            _UnitOfWork.OrderHeader.UpdateStatus(orderVM.OrderHeader.Id, SD.StatusInProcess);
            _UnitOfWork.Save();

            TempData["Success"] = "Order Start Processing Successfully.";

            return RedirectToAction(nameof(Detail), new { orderId = orderVM.OrderHeader.Id });


        }
        
        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]

        public IActionResult ShipOrder (OrderVM OrderVM)
        {
            var orderHeader = _UnitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }
            _UnitOfWork.OrderHeader.Update(orderHeader);
            _UnitOfWork.Save();

            TempData["Success"] = "Order Shipped Successfully.";

            return RedirectToAction(nameof(Detail), new { orderId = OrderVM.OrderHeader.Id });


        }

        public IActionResult CancelOrder(OrderVM OrderVM)
        {
            var orderHeader = _UnitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };

                var service = new RefundService();
                Refund refund = service.Create(options);

                _UnitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _UnitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }

            _UnitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully.";

            return RedirectToAction(nameof(Detail), new { orderId = OrderVM.OrderHeader.Id });
        }
        [HttpPost]
        [ActionName ("Details")]
        public IActionResult Details_Pay_Now(OrderVM orderVM)
        {
            orderVM.OrderHeader = _UnitOfWork.OrderHeader.Get(u=>u.Id == orderVM.OrderHeader.Id);
            orderVM.OrderDetail = _UnitOfWork.OrderDetail.GetAll(u=>u.OrderHeaderId == orderVM.OrderHeader.Id,includeProperties:"Product");
            var domin = "https://localhost:7241/";
            var options = new SessionCreateOptions
            {
                //PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domin + $"Admin/Order/PaymentConfirmation?orderHeaderId={orderVM.OrderHeader.Id}",
                CancelUrl = domin + $"Admin/Order/Detail?orderId={orderVM.OrderHeader.Id}",
            };

            foreach (var item in orderVM.OrderDetail)

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
            _UnitOfWork.OrderHeader.UpdateStripPaymentId(orderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _UnitOfWork.Save();
            HttpContext.Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
           
                     
        }


        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _UnitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment) // Order by company
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _UnitOfWork.OrderHeader.UpdateStripPaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                    _UnitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.State, SD.PaymentStatusApproved);
                    _UnitOfWork.Save();
                }

            }

    

            return View(orderHeaderId);
        }


    }
}
