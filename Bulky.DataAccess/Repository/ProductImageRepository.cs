using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class ProductImageRepository : Repository<ProductImage> , IProductImageRepository
    {
        private AppDbContext _context;
        public ProductImageRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public void Update(ProductImage obj)
        {
            _context.ProductImages.Update(obj);
        }
    }
}
