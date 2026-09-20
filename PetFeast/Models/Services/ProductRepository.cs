using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetFeast.Data;
using PetFeast.Models.Products;
using PetFeast.Models.Interfaces;

namespace PetFeast.Models.Services
{
    public class ProductRepository : IProductRepository
    {
        private readonly PetFeastDBContext _context;

        public ProductRepository(PetFeastDBContext context)
        {
            _context = context;
        }

        public IEnumerable<Product> GetAll()
        {
            return _context.Products
                           .Include(p => p.Category)
                           .ToList();
        }

        public Product? GetById(int id)
        {
            return _context.Products
                           .Include(p => p.Category)
                           .FirstOrDefault(p => p.ProductId == id);
        }

        public void Add(Product product)
        {
            _context.Products.Add(product);
        }

        public void Update(Product product)
        {
            _context.Products.Update(product);
        }

        public void Delete(int id)
        {
            var product = _context.Products.Find(id);

            if (product != null)
            {
                _context.Products.Remove(product);
            }
        }

        public void Save()
        {
            _context.SaveChanges();
        }
        public IEnumerable<Product> GetBestSellingProducts(int count)
        {
            // Lấy sản phẩm bán chạy
            var bestSellingIds = _context.OrderDetails
                .GroupBy(od => od.ProductId)
                .OrderByDescending(g => g.Sum(x => x.Quantity))
                .Select(g => g.Key)
                .Take(count)
                .ToList();

            // Lấy sản phẩm theo thứ tự bán chạy
            var bestSellingProducts = _context.Products
                .Include(p => p.Category)
                .Where(p => bestSellingIds.Contains(p.ProductId))
                .ToList()
                .OrderBy(p => bestSellingIds.IndexOf(p.ProductId))
                .ToList();

            // Nếu chưa đủ số lượng thì lấy thêm sản phẩm khác
            if (bestSellingProducts.Count < count)
            {
                var remainingProducts = _context.Products
                    .Include(p => p.Category)
                    .Where(p => !bestSellingIds.Contains(p.ProductId))
                    .OrderByDescending(p => p.ProductId)
                    .Take(count - bestSellingProducts.Count)
                    .ToList();

                bestSellingProducts.AddRange(remainingProducts);
            }

            return bestSellingProducts;
        }
    }
}
