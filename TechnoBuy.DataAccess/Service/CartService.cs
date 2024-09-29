using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechnoBuy.DataAccess.Repository.IRepository;
using TechnoBuy.DataAccess.Service.IService;

namespace TechnoBuy.DataAccess.Service
{
    public class CartService : ICartService
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public int GetCartQuantity(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return 0;
            }

            var cartItems = _unitOfWork.CartItem.GetAll(c => c.Cart.UserId == userId);
            int totalQty = cartItems.Sum(ci => ci.Quantity);

            return totalQty;
        }
    }
}
