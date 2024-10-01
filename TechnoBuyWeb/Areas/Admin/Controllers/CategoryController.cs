using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.DataAccess.Service.IService;
using TechnoBuy.Models;
using TechnoBuy.Utility;

namespace TechnoBuyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICartService _cartService;

        public CategoryController(IUnitOfWork unitOfWork, ICartService cartService)
        {
            _unitOfWork = unitOfWork;
            _cartService = cartService;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            List<Category> objCategoryList = _unitOfWork.Category.GetAll().ToList();

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(objCategoryList);
        }

        public IActionResult Create()
        {
            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View();
        }

        [HttpPost]
        public IActionResult Create(Category category)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Add(category);
                _unitOfWork.Save();

                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = _unitOfWork.Category.Get(c => c.Id == id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(categoryFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.Category.Update(category);
                _unitOfWork.Save();

                return RedirectToAction("Index");
            }

            return View();
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Category? categoryFromDb = _unitOfWork.Category.Get(c => c.Id == id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            var claimsIdentity = (ClaimsIdentity?)User.Identity;
            var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            ViewBag.CartQty = _cartService.GetCartQuantity(userId);

            return View(categoryFromDb);
        }

        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteCategory(int? id)
        {
            Category? categoryFromDb = _unitOfWork.Category.Get(c => c.Id == id);

            if (categoryFromDb == null)
            {
                return NotFound();
            }

            _unitOfWork.Category.Remove(categoryFromDb);
            _unitOfWork.Save();

            return RedirectToAction("Index");
        }
    }
}
