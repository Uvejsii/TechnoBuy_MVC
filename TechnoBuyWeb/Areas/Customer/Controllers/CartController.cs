using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;
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

        public IActionResult AddToCart(int id, bool isDetailPage)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var product = _unitOfWork.Product.Get(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
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

            _unitOfWork.Save();

            if (isDetailPage)
            {
                return RedirectToAction("Detail", "Home", new { id = product.Id });
            }

            return RedirectToAction("Index", "Home");
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

            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pending,
                TotalAmount = 0,
                OrderItems = new List<OrderItem>(),
                PaymentMethod = paymentMethod
            };

            decimal totalAmount = 0;

            var domain = "https://localhost:7038/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"Customer/Cart/OrderConfirmation",
                CancelUrl = domain + "Customer/Cart/CancelOrder",
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

                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Product = product,
                    Quantity = item.Quantity,
                    Order = order
                };

                order.OrderItems.Add(orderItem);

                totalAmount += product.Price * item.Quantity;

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

            _unitOfWork.Order.Add(order);
            _unitOfWork.CartItem.RemoveRange(cart.CartItems);
            _unitOfWork.Save();

            if (paymentMethod == PaymentMethod.Cash)
            {
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

        public IActionResult OrderConfirmation()
        {
            var service = new SessionService();
            Session session = service.Get(TempData["Session"].ToString());

            if (session.PaymentStatus == "paid")
            {
                var transaction = session.PaymentIntentId.ToString();
                return View("Success");
            }

            return RedirectToAction("Login", "Account");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}
