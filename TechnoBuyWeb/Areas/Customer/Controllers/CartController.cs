using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using Stripe.Climate;
using System.Security.Claims;
using System.Text.Json;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.DataAccess.Service.IService;
using TechnoBuy.Models;

namespace TechnoBuyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartService _cartService;

        public CartController(IUnitOfWork unitOfWork, ICartService cartService)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = _unitOfWork.Cart.Get(c => c.UserId == userId);
            if (cart == null)
            {
                return View(new List<CartItem>());
            }

            List<CartItem> cartItems = _unitOfWork.CartItem.GetAll(ci => ci.CartId == cart.Id, includeProperties: "Product,Cart.User").ToList();

            decimal totalAmount = cartItems.Sum(ci => ci.Quantity * ci.Product.Price);

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);
            ViewBag.TotalAmount = totalAmount;

            return View(cartItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public JsonResult AddToCart(int id)
        {
            Console.WriteLine($"Product ID: {JsonSerializer.Serialize(id)}");

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Json(new AddToCartResponse { RedirectUrl = Url.Action("Login", "Account"), Success = false, Message = "Please Login to add products to cart!" });
            }

            var product = _unitOfWork.Product.Get(p => p.Id == id);
            if (product == null)
            {
                return Json(new AddToCartResponse { Success = false, Message = "Item not found!" });
            }

            var cart = _unitOfWork.Cart.Get(c => c.UserId == userId);
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    EmailAddress = User.Identity.Name,
                    CartItems = new List<CartItem>()
                };
                _unitOfWork.Cart.Add(cart);
                _unitOfWork.Save();
            }

            var cartItem = _unitOfWork.CartItem.Get(ci => ci.ProductId == product.Id && ci.CartId == cart.Id);
            if (cartItem != null)
            {
                cartItem.Quantity += 1;
                _unitOfWork.CartItem.Update(cartItem);
            }
            else
            {
                cartItem = new CartItem
                {
                    CartId = cart.Id,
                    ProductId = product.Id,
                    Quantity = 1,
                    Product = product,
                };
                _unitOfWork.CartItem.Add(cartItem);
            }

            TempData["success"] = "Added to cart successfully.";
            _unitOfWork.Save();

            var cartItemCount = _unitOfWork.CartItem.GetAll(ci => ci.CartId == cart.Id).Sum(ci => ci.Quantity);

            return Json(new AddToCartResponse { Success = true, Message = TempData["success"].ToString(), CartQuantity = cartItemCount });
        }

        public IActionResult GetCartQuantityPartial()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var cartQuantity = _cartService.GetCartQuantity(userId);
            return PartialView("_CartQuantityPartial", cartQuantity);
        }

        public IActionResult Remove(int? id)
        {
            if (id == null)
            {
                return BadRequest();
            }

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = _unitOfWork.CartItem.Get(ci => ci.Id == id && ci.Cart.UserId == userId);

            if (cartItem == null)
            {
                return NotFound();
            }

            _unitOfWork.CartItem.Remove(cartItem);
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }

        public IActionResult UpdateQty(int? id, int change)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItem = _unitOfWork.CartItem.Get(ci => ci.Id == id && ci.Cart.UserId == userId);

            cartItem.Quantity += change;

            if (cartItem.Quantity < 1)
            {
                return BadRequest("Quantity cannot be less than 1"); ;
            }

            _unitOfWork.CartItem.Update(cartItem);
            _unitOfWork.Save();

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult MakeOrder(List<OrderItem> orderItems, PaymentMethod paymentMethod)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return NotFound();
            }

            var cart = _unitOfWork.Cart.Get(c => c.UserId == userId, includeProperties: "CartItems");

            decimal totalAmount = 0;

            var order = new TechnoBuy.Models.Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = 0,
                OrderItems = new List<OrderItem>(),
                PaymentMethod = paymentMethod
            };

            var domain = "https://localhost:7038/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Customer/Cart/OrderConfirmation?sessionId={{CHECKOUT_SESSION_ID}}",
                CancelUrl = domain + $"Customer/Cart/Index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in orderItems)
            {
                var product = _unitOfWork.Product.Get(p => p.Id == item.ProductId);

                if (product == null)
                {
                    return NotFound($"Product with ID {item.ProductId} not found.");
                }

                totalAmount += product.Price * item.Quantity;

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Product = product,
                    Quantity = item.Quantity
                };

                order.OrderItems.Add(orderItem);

                if (paymentMethod == PaymentMethod.Online)
                {
                    var sessionListItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long?)(product.Price * 100),
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = product.Name,
                            }
                        },
                        Quantity = item.Quantity
                    };

                    options.LineItems.Add(sessionListItem);
                }
            }

            order.TotalAmount = totalAmount;

            if (paymentMethod == PaymentMethod.Cash)
            {
                _unitOfWork.Order.Add(order);
                _unitOfWork.CartItem.RemoveRange(cart.CartItems);
                _unitOfWork.Save();

                return RedirectToAction("OrderItems");
            }

            if (paymentMethod == PaymentMethod.Online)
            {
                var service = new SessionService();
                Session session = service.Create(options);

                TempData["Session"] = session.Id;
                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }

            return BadRequest("Invalid payment method.");
        }


        public IActionResult OrderItems()
        {
            var claimsIdentity = (ClaimsIdentity?)(User.Identity);
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var lastOrder = _unitOfWork.Order.GetAll(o => o.UserId == userId, includeProperties: "OrderItems.Product,User")
                            .OrderByDescending(o => o.OrderDate)
                            .FirstOrDefault();

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(lastOrder);
        }

        public IActionResult RemoveAll()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var cartItems = _unitOfWork.CartItem.GetAll(ci => ci.Cart.UserId == userId);

            if (cartItems != null && cartItems.Any())
            {
                _unitOfWork.CartItem.RemoveRange(cartItems);
                _unitOfWork.Save();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> OrderConfirmation(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return BadRequest("Session ID not provided.");
            }

            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            if (session == null)
            {
                return NotFound("Session not found.");
            }

            if (session.PaymentStatus == "paid")
            {
                var claimsIdentity = (ClaimsIdentity?)User.Identity;
                var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (userId == null)
                {
                    return NotFound();
                }

                var cart = _unitOfWork.Cart.Get(c => c.UserId == userId, includeProperties: "CartItems");

                var order = new TechnoBuy.Models.Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    TotalAmount = 0,
                    OrderItems = new List<OrderItem>(),
                    PaymentMethod = PaymentMethod.Online
                };

                decimal totalAmount = 0;

                foreach (var cartItem in cart.CartItems)
                {
                    var product = _unitOfWork.Product.Get(p => p.Id == cartItem.ProductId);

                    if (product == null)
                    {
                        return NotFound($"Product with ID {cartItem.ProductId} not found.");
                    }

                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        Product = product,
                        Quantity = cartItem.Quantity,
                        Order = order
                    };

                    order.OrderItems.Add(orderItem);
                    totalAmount += product.Price * cartItem.Quantity;
                }

                order.TotalAmount = totalAmount;

                _unitOfWork.Order.Add(order);
                _unitOfWork.CartItem.RemoveRange(cart.CartItems);
                _unitOfWork.Save();

                return View("Success");
            }

            return View("OrderFailed");
        }


        public IActionResult Success()
        {
            return View();
        }
    }
}
