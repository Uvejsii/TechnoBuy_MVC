using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using TechnoBuy.DataAccess.Repository;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.DataAccess.Service.IService;
using TechnoBuy.Models;
using TechnoBuy.Models.ViewModels;
using TechnoBuy.Utility;

namespace TechnoBuyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartService _cartService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(IUnitOfWork unitOfWork, ICartService cartService, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
            _webHostEnvironment = webHostEnvironment;
        }

        public IActionResult Index(string searchQuery, int? categoryId, int pageNum = 1)
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            int pageSize = 6;
            int totalProducts = _unitOfWork.Product.GetAll().Count();

            List<Product> objProductList = _unitOfWork.Product
                                            .GetAll(p => (string.IsNullOrEmpty(searchQuery) || p.Name.Contains(searchQuery)) &&
                                            (!categoryId.HasValue || p.CategoryId == categoryId), 
                                            includeProperties: "Category", pageNumber: pageNum, pageSize: pageSize)
                                            .ToList();

            var categories = _unitOfWork.Category.GetAll().ToList();

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);
            ViewBag.PageNum = pageNum;
            ViewBag.HasNextPage = (pageNum * pageSize) < totalProducts;
            ViewBag.Categories = new SelectList(categories, "Id", "Name");

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

            return RedirectToAction("Index", new {pageNum = pageNum });
        }

        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                }),
                Product = new Product()
            };

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            if (id == null || id == 0)
            {
                // Create
                return View(productVM);
            }
            else
            {
                // Update
                productVM.Product = _unitOfWork.Product.Get(p => p.Id == id);
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, @"images\product");

                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    productVM.Product.ImageUrl = @"\images\product\" + fileName;
                }

                if (productVM.Product.Id == 0)
                {
                    _unitOfWork.Product.Add(productVM.Product);
                }
                else
                {
                    _unitOfWork.Product.Update(productVM.Product);
                }

                _unitOfWork.Save();
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
                {
                    Text = c.Name,
                    Value = c.Id.ToString(),
                });
                return View(productVM);
            }
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            ProductVM productVM = new ProductVM
            {
                Product = _unitOfWork.Product.Get(p => p.Id == id)
            };

            productVM.CategoryList = _unitOfWork.Category.GetAll().Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString(),
            });

            if (productVM.Product == null)
            {
                return NotFound();
            }

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(productVM);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteProduct(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            ProductVM productVM = new ProductVM
            {
                Product = _unitOfWork.Product.Get(p => p.Id == id)
            };

            if (productVM.Product == null)
            {
                return NotFound();
            }

            string wwwRootPath = _webHostEnvironment.WebRootPath;

            if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
            {
                var imagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _unitOfWork.Product.Remove(productVM.Product);
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }
    }
}
