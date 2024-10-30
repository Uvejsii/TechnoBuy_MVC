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
            int totalProducts = _unitOfWork.Product.GetAll().Count();

            List<Product> objProductList = _unitOfWork.Product
                .GetAll(p => (string.IsNullOrEmpty(searchQuery) || p.Name.Contains(searchQuery)) &&
                              (!categoryId.HasValue || p.CategoryId == categoryId), pageNumber: pageNum, pageSize: pageSize).ToList();

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);
            ViewBag.PageNum = pageNum;

            var categories = _unitOfWork.Category.GetAll().ToList();
            
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchQuery = searchQuery;
            ViewBag.HasNextPage = (pageNum * pageSize) < totalProducts;

            return View(objProductList);
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
        [ValidateAntiForgeryToken]
        public IActionResult AddComment(ProductDetailVM productDetailVM)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            productDetailVM.NewComment.AppUserId = userId;
            productDetailVM.NewComment.AppUserEmail = User.Identity.Name;

            if (!ModelState.IsValid)
            {
                productDetailVM.ProductComments = _unitOfWork.ProductComment
                    .GetAll(c => c.ProductId == productDetailVM.NewComment.ProductId)
                    .ToList();
                return PartialView("_CommentsSection", productDetailVM);
            }

            try
            {
                _unitOfWork.ProductComment.Add(productDetailVM.NewComment);
                _unitOfWork.Save();

                productDetailVM.ProductComments = _unitOfWork.ProductComment
                    .GetAll(c => c.ProductId == productDetailVM.NewComment.ProductId)
                    .ToList();

                return PartialView("_CommentsSection", productDetailVM);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding comment: {ex.Message}");
                return StatusCode(500, "Internal server error. Please try again.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteComment(int? commentId)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return RedirectToAction("Login", "Account", new {area = "Identity"});
            }

            var comment = _unitOfWork.ProductComment.Get(c => c.Id == commentId);
            if (comment != null)
            {
                _unitOfWork.ProductComment.Remove(comment);
                _unitOfWork.Save();
            }

            var productId = comment?.ProductId;
            var productComments = _unitOfWork.ProductComment.GetAll(c => c.ProductId == productId).ToList();

            var productDetailVM = new ProductDetailVM
            {
                ProductComments = productComments,
            };

            return PartialView("_CommentsSection", productDetailVM);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
