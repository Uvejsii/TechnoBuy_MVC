using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using System.Security.Claims;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.DataAccess.Service.IService;
using TechnoBuy.Models;
using TechnoBuy.Models.ViewModels;
using TechnoBuy.Utility;

namespace TechnoBuyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartService _cartService;

        public OrderController(IUnitOfWork unitOfWork, ICartService cartService)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
        }

        public IActionResult Index(string nameSearchQuery, string citySearchQuery, OrderStatus? orderStatus, PaymentMethod? paymentMethod, int pageNum = 1)
        {
            int pageSize = 7;

            int totalOrders = _unitOfWork.Order.GetAll().Count();
            int totalFilteredOrderStatuses = _unitOfWork.Order.GetAll().Where(o => o.Status == orderStatus).Count();
            int totalFilteredOrderPaymentMethods = _unitOfWork.Order.GetAll().Where(o => o.PaymentMethod == paymentMethod).Count();
            
            var ordersQuery = _unitOfWork.Order.GetAll(includeProperties: "OrderItems.Product,User")
                                               .Where(o => (string.IsNullOrEmpty(nameSearchQuery) || o.User.Email.ToLower().Contains(nameSearchQuery.ToLower())) &&
                                                (string.IsNullOrEmpty(citySearchQuery) || (o.User.City != null && o.User.City.ToLower().Contains(citySearchQuery.ToLower()))) &&
                                                (!orderStatus.HasValue || o.Status == orderStatus) && (!paymentMethod.HasValue || o.PaymentMethod == paymentMethod));

            var orders = ordersQuery.OrderByDescending(o => o.OrderDate)
                                    .Skip((pageNum - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();

            var orderStatuses = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>();
            var paymentMethods = Enum.GetValues(typeof(PaymentMethod)).Cast<PaymentMethod>();

            OrderVM orderVM = new()
            {
                StatusList = orderStatuses.Select(o => new SelectListItem
                {
                    Text = o.ToString(),
                    Value = o.ToString(),
                    Selected = orderStatus.HasValue && o == orderStatus.Value
                }),
                PaymentMethodList = paymentMethods.Select(pm => new SelectListItem
                {
                    Text = pm.ToString(),
                    Value = pm.ToString(),
                    Selected = paymentMethod.HasValue && pm == paymentMethod.Value
                }),
                Orders = orders
            };

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!string.IsNullOrEmpty(nameSearchQuery))
            {
                int totalFilteredOrderUsers = _unitOfWork.Order.GetAll(includeProperties: "User").Where(o => o.User.Email.Contains(nameSearchQuery)).Count();
                ViewBag.TotalOUsers = totalFilteredOrderUsers;
            }

            if (!string.IsNullOrEmpty(citySearchQuery))
            {
                int totalFilteredOrderUsersCities = _unitOfWork.Order.GetAll(includeProperties: "User").Where(o => o.User.City.ToLower().Contains(citySearchQuery.ToLower())).Count();
                ViewBag.TotalOUC = totalFilteredOrderUsersCities;
            }

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);
            ViewBag.OrderStatuses = new SelectList(
                Enum.GetValues(typeof(OrderStatus))
                    .Cast<OrderStatus>()
                    .Select(status => new { Id = (int)status, Name = status.ToString() }),
                "Id", "Name"
                );
            ViewBag.PageNum = pageNum;
            ViewBag.HasNextPage = (pageNum * pageSize) < totalOrders;
            ViewBag.TotalOS = totalFilteredOrderStatuses;
            ViewBag.TotalOPm = totalFilteredOrderPaymentMethods;

            return View(orderVM);
        }

        public IActionResult ChangePagination(string change, int currentPageNum)
        {
            int pageNum = currentPageNum;

            if (change == "+1")
            {
                pageNum++;
            }
            else if (change == "-1" && pageNum > 1)
            {
                pageNum--;
            }
                
            ViewBag.PageNum = pageNum;

            return RedirectToAction("Index", new {pageNum = pageNum});
        }

        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var orderStatuses = Enum.GetValues(typeof(OrderStatus)).Cast<OrderStatus>();

            OrderVM orderVM = new()
            {
                StatusList = orderStatuses.Select(o => new SelectListItem
                {
                    Text = o.ToString(),
                    Value = o.ToString(),
                }).ToList(),

                Order = new Order()
            };

            orderVM.Order = _unitOfWork.Order.Get(o => o.Id == id, includeProperties: "OrderItems.Product,User");

            if (orderVM.Order == null)
            {
                return NotFound();
            }

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(orderVM);
        }

        [HttpPost]
        public IActionResult UpdateOrderStatus(int? id, string status) 
        {
            if (id == null)
            {
                return NotFound();
            }

            Order? order = _unitOfWork.Order.Get(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            order.Status = Enum.Parse<OrderStatus>(status);

            _unitOfWork.Order.Update(order);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Order status updated successfully!" });
        }
    }
}
