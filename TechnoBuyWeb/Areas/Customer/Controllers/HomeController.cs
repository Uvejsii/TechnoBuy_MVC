using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using System.Security.Claims;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.DataAccess.Service.IService;
using TechnoBuy.Models;

namespace TechnoBuyWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartService _cartService;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, ICartService cartService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _cartService = cartService;
        }

        public IActionResult Index(string searchQuery, int? categoryId)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            List<Product> objProductList = _unitOfWork.Product
                .GetAll(p => (string.IsNullOrEmpty(searchQuery) || p.Name.Contains(searchQuery)) &&
                              (!categoryId.HasValue || p.CategoryId == categoryId)).ToList();

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            var categories = _unitOfWork.Category.GetAll().ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.SelectedCategory = categoryId;

            return View(objProductList);
        }

        public IActionResult GetProductByCatId(int categoryId)
        {
            var products = _unitOfWork.Product.GetAll(p => p.CategoryId == categoryId).ToList();

            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Detail(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            Product? objProduct = _unitOfWork.Product.Get(p => p.Id == id, includeProperties: "Category");

            if (objProduct == null)
            {
                return NotFound();
            }

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(objProduct);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
