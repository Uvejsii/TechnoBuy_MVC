using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public IActionResult Index()
        {
            List<Order> orders = _unitOfWork.Order
                                            .GetAll(includeProperties: "OrderItems.Product,User")
                                            .OrderByDescending(x => x.OrderDate)
                                            .ToList();

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(orders);
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
