using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pet_Shop2.Models;
using X.PagedList;

namespace Pet_Shop2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [LoginRequired]
    public class OrderController : Controller
    {
        PetShopContext db;
        INotyfService otyfService;
        public OrderController(PetShopContext db, INotyfService notyfService)
        {
            this.db = db;
            this.otyfService = notyfService;
        }
        public IActionResult Index(int? page)
        {
            var pageNumber = page ?? 1;
            int pageSize = 15;
            var OnePage = db.Orders.Include(x => x.TransctStatus)
                .ToPagedList(pageNumber, pageSize);
            return View(OnePage);

        }
        [HttpPost]
        [HttpPost]
        public IActionResult Delete(int id)
        {
            try
            {
                var orderDetails = db.OrderDetails.Where(od => od.OrderId == id).ToList();

                if (orderDetails.Any())
                {
                    db.OrderDetails.RemoveRange(orderDetails);
                }

                var order = db.Orders.SingleOrDefault(o => o.Id == id);
                if (order != null)
                {
                    db.Orders.Remove(order);
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng." });
                }

                db.SaveChanges();

                return Json(new { success = true, message = "Xóa đơn hàng thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi xóa đơn hàng: " + ex.Message });
            }
        }


        public IActionResult Details(int id)
        {
            var order = db.Orders.Include(x => x.OrderDetails).SingleOrDefault(x => x.Id == id);
            var orderdetail = db.OrderDetails.Where(x => x.OrderId == id).ToList();
            List<Product> lstProduct = new List<Product>();
            foreach (var item in orderdetail)
            {
                var tmp = db.Products.SingleOrDefault(x => x.Id == item.ProductId);
                if (tmp != null)
                {
                    lstProduct.Add(tmp);
                }
            }
            ViewBag.Order = order;
            ViewBag.Transaction = db.Transactions.ToList();
            ViewBag.lstProduct = lstProduct;
            ViewBag.CusOrder = db.Accounts.SingleOrDefault(x => x.Id == order.AccountId);
            return View(orderdetail);
        }
        [HttpPost]
        public IActionResult Edit(int orderID, int status, bool paid = false)
        {
            var order = db.Orders.SingleOrDefault(x => x.Id == orderID);
            if (order != null)
            {
                order.TransctStatusId = status;
                order.Paid = paid;
                db.SaveChanges();
            }
            otyfService.Success("Cập nhật đơn hàng thành công!");
            return RedirectToAction("Index", "order");
        }
    }
}