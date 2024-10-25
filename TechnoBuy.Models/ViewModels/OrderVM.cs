using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TechnoBuy.Models.ViewModels
{
    public class OrderVM
    {
        public Order Order { get; set; }

        [ValidateNever]
        public IEnumerable<Order> Orders { get; set; }

        [ValidateNever]
        public IEnumerable<SelectListItem> StatusList { get; set; }
    }
}
