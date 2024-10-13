using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoBuy.Models.ViewModels
{
    public class ProductDetailVM
    {
        [ValidateNever]
        public Product Product { get; set; }

        [ValidateNever]
        public ProductComment NewComment { get; set; }
        [ValidateNever]
        public List<ProductComment> ProductComments { get; set; }
    }
}
