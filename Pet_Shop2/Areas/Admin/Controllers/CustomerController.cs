using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pet_Shop2.Models;
using X.PagedList;
using AspNetCoreHero.ToastNotification.Abstractions;
using Pet_Shop2.Areas.Admin.Models;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Pet_Shop2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [LoginRequired]
    public class CustomerController : Controller
    {
        private readonly PetShopContext db;
        public INotyfService NotyfService { get; }

        public CustomerController(PetShopContext context, INotyfService notyfService)
        {
            db = context;
            NotyfService = notyfService;
        }

        // GET: Admin/Customer
        public async Task<IActionResult> Index(int? page, string searchString, string activeStatus, string sortBy)
        {
            ViewBag.CurrentSearch = searchString;
            ViewBag.CurrentActiveStatus = activeStatus;
            ViewBag.CurrentSortBy = sortBy;

            var pageNumber = page ?? 1;
            int pageSize = 10; // Bạn có thể điều chỉnh pageSize nếu muốn

            IQueryable<Account> customerAccountsQuery = db.Accounts
                                                 .Where(a => a.RoleId == 2) // Chỉ lấy tài khoản là Khách Hàng
                                                 .Include(a => a.Role) // Để có thể truy cập item.Role.RoleName trong View nếu cần
                                                 .AsNoTracking();

            if (!String.IsNullOrEmpty(searchString))
            {
                string lowerSearchString = searchString.ToLower();
                customerAccountsQuery = customerAccountsQuery.Where(s =>
                                                              (s.FullName != null && s.FullName.ToLower().Contains(lowerSearchString)) ||
                                                              (s.Email != null && s.Email.ToLower().Contains(lowerSearchString)) ||
                                                              (s.Phone != null && s.Phone.Contains(searchString)) ||
                                                              (s.UserName != null && s.UserName.ToLower().Contains(lowerSearchString)));
            }

            // Lọc theo trạng thái hoạt động CHỈ KHI activeStatus có giá trị cụ thể (không phải rỗng)
            if (!String.IsNullOrEmpty(activeStatus))
            {
                if (activeStatus == "active")
                {
                    customerAccountsQuery = customerAccountsQuery.Where(a => a.Active == true);
                }
                else if (activeStatus == "inactive")
                {
                    customerAccountsQuery = customerAccountsQuery.Where(a => a.Active == false || a.Active == null);
                }
                // Nếu activeStatus là rỗng (nghĩa là chọn "Tất cả trạng thái"), thì không thêm điều kiện Where nào cả.
            }

            switch (sortBy)
            {
                case "name_asc":
                    customerAccountsQuery = customerAccountsQuery.OrderBy(a => a.FullName);
                    break;
                case "name_desc":
                    customerAccountsQuery = customerAccountsQuery.OrderByDescending(a => a.FullName);
                    break;
                case "date_asc":
                    customerAccountsQuery = customerAccountsQuery.OrderBy(a => a.CreateDate);
                    break;
                case "date_desc":
                default:
                    customerAccountsQuery = customerAccountsQuery.OrderByDescending(a => a.CreateDate);
                    break;
            }

            var activeStatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả trạng thái" }, // Value rỗng cho "Tất cả"
                new SelectListItem { Value = "active", Text = "Hoạt động" },
                new SelectListItem { Value = "inactive", Text = "Không hoạt động" }
            };
            ViewBag.ActiveStatusList = new SelectList(activeStatusOptions, "Value", "Text", activeStatus);

            var sortByOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "date_desc", Text = "Ngày tạo (Mới nhất)" },
                new SelectListItem { Value = "date_asc", Text = "Ngày tạo (Cũ nhất)" },
                new SelectListItem { Value = "name_asc", Text = "Tên (A-Z)" },
                new SelectListItem { Value = "name_desc", Text = "Tên (Z-A)" }
            };
            ViewBag.SortByList = new SelectList(sortByOptions, "Value", "Text", sortBy);

            IPagedList<Account> pagedCustomerAccounts = await customerAccountsQuery.ToPagedListAsync(pageNumber, pageSize);

            return View(pagedCustomerAccounts);
        }

        // ... (Các actions Details, Delete, DeleteConfirmed giữ nguyên như phiên bản đầy đủ gần nhất) ...
        // GET: Admin/Customer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                NotyfService.Error("ID khách hàng không được cung cấp.");
                return RedirectToAction(nameof(Index));
            }

            var account = await db.Accounts
                .Include(a => a.Role)
                .Include(a => a.Orders)
                    .ThenInclude(o => o.TransctStatus)
                .Include(a => a.Orders)
                    .ThenInclude(o => o.OrderDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.RoleId == 2);

            if (account == null)
            {
                NotyfService.Error($"Không tìm thấy khách hàng với ID: {id}.");
                return View("NotFoundCustom");
            }

            var orderViewModels = new List<OrderViewModel>();
            if (account.Orders != null)
            {
                foreach (var order in account.Orders.OrderByDescending(o => o.OrderDate))
                {
                    var orderDetailViewModels = new List<OrderDetailViewModel>();
                    if (order.OrderDetails != null)
                    {
                        foreach (var od in order.OrderDetails)
                        {
                            Product? product = null;
                            if (od.ProductId.HasValue)
                            {
                                product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == od.ProductId.Value);
                            }

                            orderDetailViewModels.Add(new OrderDetailViewModel
                            {
                                ProductId = od.ProductId ?? 0,
                                ProductName = product?.ProductName ?? "N/A",
                                Quantity = od.Quantity,
                                UnitPrice = product?.Price,
                                Discount = product?.Discount,
                                LineTotal = od.Total
                            });
                        }
                    }

                    orderViewModels.Add(new OrderViewModel
                    {
                        OrderId = order.Id,
                        OrderDate = order.OrderDate,
                        ShipDate = order.ShipDate,
                        TransactionStatus = order.TransctStatus?.Name ?? "N/A",
                        IsPaid = order.Paid ?? false,
                        PaymentDate = order.PaymentDate,
                        PaymentMethod = order.PaymentId.HasValue ? $"PTTT ID: {order.PaymentId}" : "N/A",
                        ShippingAddress = order.Address,
                        TotalOrderAmount = (order.OrderDetails != null) ? order.OrderDetails.Sum(od_sum => od_sum.Total ?? 0) : 0,
                        OrderDetails = orderDetailViewModels
                    });
                }
            }

            var viewModel = new AccountDetailsViewModel
            {
                Account = account,
                Orders = orderViewModels
            };

            return View(viewModel);
        }

        // GET: Admin/Customer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                NotyfService.Error("ID khách hàng không được cung cấp.");
                return RedirectToAction(nameof(Index));
            }

            var account = await db.Accounts
                .FirstOrDefaultAsync(m => m.Id == id && m.RoleId == 2);

            if (account == null)
            {
                NotyfService.Error($"Không tìm thấy khách hàng với ID: {id}.");
                return View("NotFoundCustom");
            }

            if (account.Active == true)
            {
                NotyfService.Warning("Không thể xóa khách hàng đang hoạt động. Vui lòng chuyển sang trạng thái 'Không hoạt động' trước.");
                ViewBag.CanDelete = false;
                ViewBag.ErrorMessage = "Khách hàng này đang ở trạng thái hoạt động. Không thể xóa.";
            }
            else
            {
                var hasNonDeletedOrders = await db.Orders.AnyAsync(o => o.AccountId == id && o.Deleted != true);
                if (hasNonDeletedOrders)
                {
                    NotyfService.Warning("Khách hàng này có đơn hàng liên quan và chưa được xử lý. Không thể xóa trực tiếp.");
                    ViewBag.CanDelete = false;
                    ViewBag.ErrorMessage = "Khách hàng này có đơn hàng liên quan chưa xử lý. Không thể xóa.";
                }
                else
                {
                    ViewBag.CanDelete = true;
                }
            }
            return View(account);
        }

        // POST: Admin/Customer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var account = await db.Accounts.FirstOrDefaultAsync(a => a.Id == id && a.RoleId == 2);

            if (account == null)
            {
                NotyfService.Error("Không tìm thấy khách hàng để xóa.");
                return RedirectToAction(nameof(Index));
            }

            if (account.Active == true)
            {
                NotyfService.Error("Không thể xóa khách hàng đang ở trạng thái hoạt động.");
                TempData["ErrorMessage"] = "Không thể xóa khách hàng đang hoạt động.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            var hasNonDeletedOrders = await db.Orders.AnyAsync(o => o.AccountId == id && o.Deleted != true);
            if (hasNonDeletedOrders)
            {
                NotyfService.Error("Khách hàng này vẫn còn đơn hàng chưa xử lý, không thể xóa.");
                TempData["ErrorMessage"] = "Khách hàng này vẫn còn đơn hàng chưa xử lý, không thể xóa.";
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            try
            {
                db.Accounts.Remove(account);
                await db.SaveChangesAsync();
                NotyfService.Success("Xóa khách hàng thành công.");
            }
            catch (DbUpdateException ex)
            {
                NotyfService.Error("Có lỗi xảy ra trong quá trình xóa: " + (ex.InnerException?.Message ?? ex.Message));
                TempData["ErrorMessage"] = "Lỗi khi xóa: " + (ex.InnerException?.Message ?? ex.Message);
                return RedirectToAction(nameof(Delete), new { id = id });
            }

            return RedirectToAction(nameof(Index));
        }

        private bool AccountExists(int id)
        {
            return (db.Accounts?.Any(e => e.Id == id && e.RoleId == 2)).GetValueOrDefault();
        }
    }
}