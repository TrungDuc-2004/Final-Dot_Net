using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pet_Shop2.Helper;
using Pet_Shop2.Models;
using X.PagedList;
namespace Pet_Shop2.Areas.Admin.Controllers
{
    [Area("Admin")]
    [LoginRequired]
    public class CategoriesController : Controller
    {
        PetShopContext db = new PetShopContext();
        INotyfService notyfService;
        private readonly IWebHostEnvironment _environment;
        public CategoriesController(IWebHostEnvironment environment,INotyfService notyfService)
        {
            _environment = environment;
            this.notyfService = notyfService;
        }
        public IActionResult Index(int? page)
        {
            var pageNumber = page ?? 1;
            int pageSize = 10;
            var OnePage = db.Categories.ToPagedList(pageNumber, pageSize);
            return View(OnePage);

        }
        public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(Category item, IFormFile imageFile)
        {
            if (item != null)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Tạo tên file mới
                    string fileName = $"{DateTime.Now.ToString("dd-MM-yyyy") + item.Alias + "categories"}{Path.GetExtension(imageFile.FileName)}";
                    item.Image= await Utilities.UploadFile(imageFile, @"Categories", fileName);
                }

                db.Categories.Add(item);
                db.SaveChanges();
                notyfService.Success("Thêm loại sản phẩm thành công");
                return RedirectToAction("Index");
            }
            return View(item);
        }
        public IActionResult Edit(int? id)
        {
            return View(db.Categories.SingleOrDefault(x => x.Id == id));
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Category item, IFormFile imageFile, bool checkchange)
        {
            var tmp = db.Categories.SingleOrDefault(x => x.Id == item.Id);
            if (checkchange == true)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Tạo tên file mới
                    string fileName = $"{DateTime.Now.ToString("dd-MM-yyyy") + item.Alias + "categories"}{Path.GetExtension(imageFile.FileName)}";
                    if (tmp != null)
                    { tmp.Image = fileName; db.SaveChanges(); }

                        // Đường dẫn lưu trữ
                        string uploadsFolder = Path.Combine(_environment.WebRootPath, "Image_Categories");
                    string filePath = Path.Combine(uploadsFolder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Lưu trữ đường dẫn của hình ảnh vào cơ sở dữ liệu hoặc làm bất kỳ xử lý nào khác bạn cần thiết ở đây.
                    
                }
            }
            
            if(tmp!=null)
            {
                tmp.Name = item.Name;
                tmp.Description = item.Description;
                tmp.Published= item.Published;
                tmp.Title= item.Title;
                tmp.Alias = item.Alias;
                db.SaveChanges();
                notyfService.Success("Sửa thành công");
                return RedirectToAction("Index");
            }    
            return View(item);

        }
        public IActionResult Delete(int id)
        {
            var tmp = db.Categories.SingleOrDefault(x=>x.Id==id);
            if(tmp!=null)
            {
                db.Categories.Remove(tmp);
                db.SaveChanges();
                return Json(new {success=true});
            }
            return Json(new { success = false });
        }

    }
}
