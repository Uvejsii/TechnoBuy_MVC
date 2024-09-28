using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.Models;

namespace TechnoBuyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            List<CartItem> cartItems = _unitOfWork.CartItem.GetAll(ci => ci.CartId == cart.Id, includeProperties: "Product").ToList();

            return View(cartItems);
        }

        public IActionResult AddToCart(int id)
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
    }
}
