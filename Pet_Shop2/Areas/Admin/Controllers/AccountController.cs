using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pet_Shop2.Helper;
using Pet_Shop2.Extensions;
using Pet_Shop2.Models;
using X.PagedList;

namespace Pet_Shop2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [LoginRequired]
    public class AccountController : Controller
    {
        PetShopContext db = new PetShopContext();
        public INotyfService notyfService { get; }
        public AccountController(INotyfService notyfService)
        {
            this.notyfService = notyfService;
        }
        public IActionResult Index(int ? page)
        {
            var pageNumber = page ?? 1;
            int pageSize = 5;
            var OnePage = db.Accounts.Include(x=>x.Role).ToPagedList(pageNumber, pageSize);
            return View(OnePage);
        }

        public IActionResult Create()
        {
            ViewBag.role = db.Roles.ToList();
            return View();
        }
        [HttpPost]
        public IActionResult Create(Account acc)
        {
            if (acc != null)
            {
                acc.CreateDate = DateTime.Now;
                //string password = HashMD5(acc.Password);
                acc.Password =acc?.Password?.ToMD5() ;
                db.Accounts.Add(acc);
                db.SaveChanges();
                return RedirectToAction("Index", "Account");
            }
            return View(acc);
        }

        [HttpPost]
        public ActionResult Delete(int? id)
        {
            if (id != null)
            {
                try
                {
                    var account = db.Accounts
                                    .Include(a => a.Orders)
                                    .SingleOrDefault(x => x.Id == id);

                    if (account == null)
                        return Json(new { success = false, message = "Không tìm thấy tài khoản." });

                    if (account.Orders != null && account.Orders.Any())
                    {
                        return Json(new { success = false, message = "Không thể xóa tài khoản vì đã có đơn hàng liên quan." });
                    }

                    db.Accounts.Remove(account);
                    db.SaveChanges();

                    return Json(new { success = true, message = "Tài khoản đã được xóa thành công." });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
                }
            }

            return Json(new { success = false, message = "ID không hợp lệ." });
        }
        public IActionResult Edit(int? id)
        {
            ViewBag.role = db.Roles.ToList();
            return View(db.Accounts.SingleOrDefault(x=>x.Id==id));
        }
        [HttpPost]
        public IActionResult Edit(Account acc)
        {
            if(acc != null)
            {
                var tmp = db.Accounts.SingleOrDefault(x=>x.Id == acc.Id);
                if(tmp != null)
                {
                    tmp.Phone= acc.Phone;
                    tmp.Email= acc.Email;
                    tmp.Password= acc?.Password?.ToMD5();
                    tmp.Active= acc?.Active;
                    tmp.FullName= acc?.FullName;
                    tmp.RoleId=acc.RoleId;
                    tmp.UserName=acc.UserName;
                    db.SaveChanges();
                }

                return RedirectToAction("Index");
            }    
            return View(acc);
        }

        [HttpPost]
        public IActionResult Edit_Active(int id)
        {
            var tmp = db.Accounts.SingleOrDefault(x => x.Id == id);
            if (tmp != null)
            {
                if (tmp.Active == true) { tmp.Active = false; }
                else tmp.Active = true;
                db.SaveChanges();
            }
            /*notyfService.Success("Thay đổi hoạt động cho tài khoản" + tmp?.Email);*/
            return RedirectToAction("Index");
        }
    }
}
