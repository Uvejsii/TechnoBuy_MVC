using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;
using System.Security.Claims;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.DataAccess.Service.IService;
using TechnoBuy.Models;
using TechnoBuy.Models.ViewModels;

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

        public IActionResult Index(string searchQuery, int? categoryId, int pageNum = 1)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            int pageSize = 8;

            List<Product> objProductList = _unitOfWork.Product
                .GetAll(p => (string.IsNullOrEmpty(searchQuery) || p.Name.Contains(searchQuery)) &&
                              (!categoryId.HasValue || p.CategoryId == categoryId), null, null, pageNum, pageSize).ToList();

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);
            ViewBag.PageNum = pageNum;

            var categories = _unitOfWork.Category.GetAll().ToList();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchQuery = searchQuery;

            return View(objProductList);
        }

        public IActionResult ChangePagination(string change)
        {
            int pageNum = ViewBag.PageNum ?? 1;

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

            var product = _unitOfWork.Product.Get(p => p.Id == id, includeProperties: "Category,ProductComments.ApplicationUser");
            List<ProductComment> productComments = _unitOfWork.ProductComment.GetAll(c => c.ProductId == id).ToList();

            if (product == null)
            {
                return NotFound();
            }

            var productDetailVM = new ProductDetailVM
            {
                Product = product,
                ProductComments = productComments,
                NewComment = new ProductComment()
            };

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(productDetailVM);
        }

        [HttpPost]
        public IActionResult AddComment(ProductDetailVM productDetailVM)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            productDetailVM.NewComment.AppUserId = userId;
            productDetailVM.NewComment.AppUserEmail = User.Identity.Name;

            if (!ModelState.IsValid)
            {
                productDetailVM.Product = _unitOfWork.Product.Get(p => p.Id == productDetailVM.NewComment.ProductId, includeProperties: "Category,ApplicationUser");
                productDetailVM.ProductComments = _unitOfWork.ProductComment.GetAll(c => c.ProductId == productDetailVM.NewComment.ProductId).ToList();
                return View("Detail", productDetailVM);
            }

            _unitOfWork.ProductComment.Add(productDetailVM.NewComment);
            _unitOfWork.Save();

            return RedirectToAction("Detail", new {id = productDetailVM.NewComment.ProductId});
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
