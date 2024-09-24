using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.Models;

namespace TechnoBuyWeb.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;

        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            List<Product> objProductList = _unitOfWork.Product.GetAll().ToList();
            return View(objProductList);
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

            Product? objProduct = _unitOfWork.Product.Get(p => p.Id == id, includeProperties: "Category");

            if (objProduct == null) 
            {
                return NotFound();
            }

            return View(objProduct);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
