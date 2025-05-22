using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pet_Shop2.Extensions;
using Pet_Shop2.Helper;
using Pet_Shop2.Models;
using Pet_Shop2.ModelsView;
using System.Net.Mail;
using System.Security.Claims;

namespace Pet_Shop2.Controllers
{
    [Authorize]
    public class Cus_Account : Controller
    {
        PetShopContext db;
        public INotyfService notyfService { get; set; }

        public Cus_Account(PetShopContext db, INotyfService _notyfService)
        {
            notyfService = _notyfService;
            this.db = db;
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult ValidatePhone(string Phone)
        {
            try
            {
                var cus = db.Accounts.AsNoTracking().SingleOrDefault(x => x.Phone.ToLower() == Phone);
                if (cus != null) return Json(new { data = "Số điện thoại đã được sử dụng" });
                return Json(new { data = true });
            }
            catch

            {
                return Json(new { data = false });
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult ValidateEmail(string email)
        {
            try
            {
                var cus = db.Accounts.AsNoTracking().SingleOrDefault(x => x.Email == email.Trim());
                if (cus != null) return Json(new { data = "Email này đã được sử dụng" });
                return Json(new { data = true });
            }
            catch
            {
                return Json(new { data = false });
            }
        }
        [AllowAnonymous]
        public IActionResult Login()
        {
            var taikhoanID = HttpContext.Session.GetString("CustomerId");
            var returnUrl = HttpContext.Request.Query["ReturnUrl"].ToString();
            if (taikhoanID == null)
            {
                // Lưu trang trước đó vào session
                HttpContext.Session.SetString("ReturnUrl", returnUrl);
                return View();
            }
            else
            {
                return RedirectToAction("index", "home");
            }
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool IsEmail = Utilities.IsValidEmail(model?.UserName);
                    
                    var cus = db.Accounts.AsNoTracking().SingleOrDefault(x => x.Email == model.UserName&& IsEmail==true||x.UserName==model.UserName);
                    if (cus == null)
                    {
                        notyfService.Error("Thông tin đăng nhập chưa chính xác !");
                        return RedirectToAction("Login", "Cus_Account");
                    } //A bấm f10 là tới nha f11 là từng dòng lệnh . Để em debug cho a xem
                    string? pass = model?.Password?.ToMD5();
                    if (cus.Password != pass)
                    {
                        //Thông báo thông tin chưa chính xác
                        notyfService.Error("Thông tin đăng nhập chưa chính xác !");
                        return View();
                    }
                    //Kiểm tra xem tài khoản có còn hoạt động không
                    if (cus.Active == false)
                    {
                        //notyfService.Error("Tài khoản của bạn đã bị khóa !");
                        return RedirectToAction("Login", "Cus_Account");
                    }

                    //Lưu Sesion 
                    HttpContext.Session.SetString("CustomerId", cus.Id.ToString());
                    HttpContext.Session.SetString("UserName", cus.UserName.ToString());
                    //Lấy link trước đó nếu có
                    var returnUrl = HttpContext.Session.GetString("ReturnUrl");
                    //Lấy id tài khoản
                    var taikhoanID = HttpContext.Session.GetString("CustomerId");

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name,cus.UserName),
                        new Claim("CustomerId",cus.Id.ToString()),
                        new Claim("UserName",cus.UserName)
                    };
                    ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    //Thông báo cho người dùng biết đã đăng nhập thành công
                    notyfService.Success("Đăng nhập thành công"); //Đăng nhập thành công

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        // Chuyển hướng đến trang trước đó
                        return Redirect(returnUrl);
                    }
                    else
                    {
                        return RedirectToAction("Login", "cus_Account");
                    }
                }
            }
            catch
            {
                notyfService.Error("Đăng nhập không thành công");
                return RedirectToAction("register", "cus_Account");
            }
            notyfService.Error("Đăng nhập không thành công");
            return RedirectToAction("register", "cus_Account");
        }

        public IActionResult Logout()
        {
            ///HttpContext.SignOutAsync();
            HttpContext.Session.Remove("CustomerId");
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            notyfService.Success("Đăng xuất thành công");
            return RedirectToAction("Index", "Home");
        }


        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterAccount acc)
        {
            try
            {
                if (ModelState.IsValid)
                {

                    Account Acc = new Account
                    {
                        FullName = acc?.FullName,
                        UserName = acc?.UserName,
                        Phone = acc?.Phone?.Trim().ToLower(),
                        Email = acc?.Email?.Trim().ToLower(),
                        Password = acc?.Password?.Trim().ToMD5(),
                        Active = true,
                        RoleId = 2,
                        CreateDate = DateTime.Now
                    };
                    try
                    {

                        db.Accounts.Add(Acc);

                        db.SaveChanges();

                        //Lưu Session mã kh
                        HttpContext.Session.SetString("CustomerId", Acc.Id.ToString());
                        HttpContext.Session.SetString("UserName", acc.UserName.ToString());
                        var taikhoanID = HttpContext.Session.GetString("CustomerId");
                        //Identity
                        var claims = new List<Claim>
                        {
                            new Claim(ClaimTypes.Name,acc.Email),
                            new Claim("CustomerId",Acc.Id.ToString()),
                            new Claim("UserName",Acc?.UserName)
                        };


                        ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "login");
                        ClaimsPrincipal cliamsPrincipal = new ClaimsPrincipal(claimsIdentity);
                        await HttpContext.SignInAsync(cliamsPrincipal);
                        notyfService.Success("Đăng ký thành công");
                        return RedirectToAction("Login", "Cus_Account");

                    }
                    catch
                    {
                        notyfService.Error("Đăng ký thất bại");
                        return View(acc);
                    }

                }

                notyfService.Success("Đăng ký không thành công");
                return RedirectToAction("Register", "Cus_Account");
            }
            catch
            {
                notyfService.Error("Đăng ký thất bại");
                return View(acc);
            }
        }

        [AllowAnonymous]
        public IActionResult ForgetPassword()
        {
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public IActionResult ForgetPassword(LoginViewModel cus)
        {
            if (cus != null)
            {
                Account? acc = db.Accounts.SingleOrDefault(x => x.Phone == cus.Phone && x.Email == cus.UserName);
                if (acc != null)
                {


                    string? userName = acc.UserName;
                    Random rd = new Random();
                    int randompass = rd.Next(100000, 999999);
                    string style = "<style type='text/css'>\r\n    #outlook a {\r\n        padding:0;\r\n    }\r\n    .ExternalClass {\r\n        width:100%;\r\n    }\r\n    .ExternalClass,\r\n    .ExternalClass p,\r\n    .ExternalClass span,\r\n    .ExternalClass font,\r\n    .ExternalClass td,\r\n    .ExternalClass div {\r\n        line-height:100%;\r\n    }\r\n    .es-button {\r\n        mso-style-priority:100!important;\r\n        text-decoration:none!important;\r\n    }\r\n    a[x-apple-data-detectors] {\r\n        color:inherit!important;\r\n        text-decoration:none!important;\r\n        font-size:inherit!important;\r\n        font-family:inherit!important;\r\n        font-weight:inherit!important;\r\n        line-height:inherit!important;\r\n    }\r\n    .es-desk-hidden {\r\n        display:none;\r\n        float:left;\r\n        overflow:hidden;\r\n        width:0;\r\n        max-height:0;\r\n        line-height:0;\r\n        mso-hide:all;\r\n    }\r\n    .es-button-border:hover a.es-button {\r\n        background:#ffffff!important;\r\n        border-color:#ffffff!important;\r\n    }\r\n    .es-button-border:hover {\r\n        background:#ffffff!important;\r\n        border-style:solid solid solid solid!important;\r\n        border-color:#259cd8 #259cd8 #259cd8 #259cd8!important;\r\n    }\r\n    @media only screen and (max-width:600px) {p, ul li, ol li, a { font-size:16px!important; line-height:150%!important } h1 { font-size:20px!important; text-align:center; line-height:120%!important } h2 { font-size:16px!important; text-align:left; line-height:120%!important } h3 { font-size:20px!important; text-align:center; line-height:120%!important } h1 a { font-size:20px!important } h2 a { font-size:16px!important; text-align:left } h3 a { font-size:20px!important } .es-menu td a { font-size:14px!important } .es-header-body p, .es-header-body ul li, .es-header-body ol li, .es-header-body a { font-size:10px!important } .es-footer-body p, .es-footer-body ul li, .es-footer-body ol li, .es-footer-body a { font-size:12px!important } .es-infoblock p, .es-infoblock ul li, .es-infoblock ol li, .es-infoblock a { font-size:12px!important } *[class='gmail-fix'] { display:none!important } .es-m-txt-c, .es-m-txt-c h1, .es-m-txt-c h2, .es-m-txt-c h3 { text-align:center!important } .es-m-txt-r, .es-m-txt-r h1, .es-m-txt-r h2, .es-m-txt-r h3 { text-align:right!important } .es-m-txt-l, .es-m-txt-l h1, .es-m-txt-l h2, .es-m-txt-l h3 { text-align:left!important } .es-m-txt-r img, .es-m-txt-c img, .es-m-txt-l img { display:inline!important } .es-button-border { display:block!important } a.es-button { font-size:14px!important; display:block!important; border-left-width:0px!important; border-right-width:0px!important } .es-btn-fw { border-width:10px 0px!important; text-align:center!important } .es-adaptive table, .es-btn-fw, .es-btn-fw-brdr, .es-left, .es-right { width:100%!important } .es-content table, .es-header table, .es-footer table, .es-content, .es-footer, .es-header { width:100%!important; max-width:600px!important } .es-adapt-td { display:block!important; width:100%!important } .adapt-img { width:100%!important; height:auto!important } .es-m-p0 { padding:0px!important } .es-m-p0r { padding-right:0px!important } .es-m-p0l { padding-left:0px!important } .es-m-p0t { padding-top:0px!important } .es-m-p0b { padding-bottom:0!important } .es-m-p20b { padding-bottom:20px!important } .es-mobile-hidden, .es-hidden { display:none!important } tr.es-desk-hidden, td.es-desk-hidden, table.es-desk-hidden { width:auto!important; overflow:visible!important; float:none!important; max-height:inherit!important; line-height:inherit!important } tr.es-desk-hidden { display:table-row!important } table.es-desk-hidden { display:table!important } td.es-desk-menu-hidden { display:table-cell!important } .es-menu td { width:1%!important } table.es-table-not-adapt, .esd-block-html table { width:auto!important } table.es-social { display:inline-block!important } table.es-social td { display:inline-block!important } }\r\n      </style>"; 
                    string Mailbody = $@"<!DOCTYPE html>
<html xmlns:o='urn:schemas-microsoft-com:office:office' style='width:100%;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0'>
 <head>
    
  <meta charset='UTF-8'>
  <meta content='width=device-width, initial-scale=1' name='viewport'>
  <title>RESET PASSWORD</title>
  {style}
 </head>
<body style=""width:100%;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;-webkit-text-size-adjust:100%;-ms-text-size-adjust:100%;padding:0;Margin:0"">
  <div class=""es-wrapper-color"" style=""background-color:#FAFAFA"">
   <table class=""es-wrapper"" width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;padding:0;Margin:0;width:100%;height:100%;background-repeat:repeat;background-position:center top"">
     <tbody><tr style=""border-collapse:collapse"">
      <td valign=""top"" style=""padding:0;Margin:0"">
       
       <table cellpadding=""0"" cellspacing=""0"" class=""es-header"" align=""center"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%;background-color:transparent;background-repeat:repeat;background-position:center top"">
         <tbody><tr style=""border-collapse:collapse"">
          <td class=""es-adaptive"" align=""center"" style=""padding:0;Margin:0"">
           <table class=""es-header-body"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#259cd8;width:600px"" cellspacing=""0"" cellpadding=""0"" bgcolor=""#259cd8"" align=""center"">
             <tbody><tr style=""border-collapse:collapse"">
              <td style=""Margin:0;padding-top:20px;padding-bottom:20px;padding-left:20px;padding-right:20px;background-color:#259cd8"" bgcolor=""#259cd8"" align=""left"">
               <table class=""es-left"" cellspacing=""0"" cellpadding=""0"" align=""left"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left"">
                 <tbody><tr style=""border-collapse:collapse"">
                  <td class=""es-m-p20b"" align=""left"" style=""padding:0;Margin:0;width:270px"">
                   <table width=""100%"" cellspacing=""0"" cellpadding=""0"" role=""presentation"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px"">
                     <tbody><tr style=""border-collapse:collapse"">
                      <td class=""es-m-p0l es-m-txt-c"" align=""left"" style=""padding:0;Margin:0;font-size:0""><a href=""https://viewstripo.email"" target=""_blank"" style=""-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;font-size:14px;text-decoration:none;color:#1376C8""><img src=""https://tlr.stripocdn.email/content/guids/CABINET_66498ea076b5d00c6f9553055acdb37a/images/12051527590691841.png"" alt style=""display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic"" width=""183""></a></td>
                     </tr>
                   </tbody></table></td>
                 </tr>
               </tbody></table><!--[if mso]></td><td style=""width:20px""></td><td style=""width:270px"" valign=""top""><![endif]-->
               <table class=""es-right"" cellspacing=""0"" cellpadding=""0"" align=""right"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right"">
                 <tbody><tr style=""border-collapse:collapse"">
                  <td align=""left"" style=""padding:0;Margin:0;width:270px"">
                   <table width=""100%"" cellspacing=""0"" cellpadding=""0"" role=""presentation"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px"">
                     <tbody><tr style=""border-collapse:collapse"">
                      <td class=""es-m-txt-c"" align=""right"" style=""padding:0;Margin:0;padding-top:10px""><span class=""es-button-border"" style=""border-style:solid;border-color:#259cd8;background:#FFFFFF;border-width:2px;display:inline-block;border-radius:10px;width:auto""><a href=""#"" class=""es-button"" style=""mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:14px;color:#259cd8;border-style:solid;border-color:#FFFFFF;border-width:15px 20px 15px 20px;display:inline-block;background:#FFFFFF;border-radius:10px;font-weight:bold;font-style:normal;line-height:17px;width:auto;text-align:center"">VTUANMAI</a></span></td>
                     </tr>
                   </tbody></table></td>
                 </tr>
               </tbody></table><!--[if mso]></td></tr></table><![endif]--></td>
             </tr>
           </tbody></table></td>
         </tr>
       </tbody></table>
       <table class=""es-content"" cellspacing=""0"" cellpadding=""0"" align=""center"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;table-layout:fixed !important;width:100%"">
         <tbody><tr style=""border-collapse:collapse"">
          <td style=""padding:0;Margin:0;background-color:#FAFAFA"" bgcolor=""#fafafa"" align=""center"">
           <table class=""es-content-body"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-color:#FFFFFF;width:600px"" cellspacing=""0"" cellpadding=""0"" bgcolor=""#ffffff"" align=""center"">
             <tbody><tr style=""border-collapse:collapse"">
              <td style=""padding:0;Margin:0;padding-left:20px;padding-right:20px;padding-top:40px;background-color:transparent;background-position:left top"" bgcolor=""transparent"" align=""left"">
               <table width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px"">
                 <tbody><tr style=""border-collapse:collapse"">
                  <td valign=""top"" align=""center"" style=""padding:0;Margin:0;width:560px"">
                   <table style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-position:left top"" width=""100%"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                     <tbody><tr style=""border-collapse:collapse"">
                      <td align=""center"" style=""padding:0;Margin:0;padding-top:5px;padding-bottom:5px;font-size:0""><img src=""https://tlr.stripocdn.email/content/guids/CABINET_dd354a98a803b60e2f0411e893c82f56/images/23891556799905703.png"" alt style=""display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic"" width=""175""></td>
                     </tr>
                     <tr style=""border-collapse:collapse"">
                      <td align=""center"" style=""padding:0;Margin:0;padding-top:15px;padding-bottom:15px""><h1 style=""Margin:0;line-height:24px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:20px;font-style:normal;font-weight:normal;color:#333333""><strong>BẠN QUÊN</strong></h1><h1 style=""Margin:0;line-height:24px;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:20px;font-style:normal;font-weight:normal;color:#333333""><strong>&nbsp;MẬT KHẨU ?</strong></h1></td>
                     </tr>
                     <tr style=""border-collapse:collapse"">
                      <td align=""left"" style=""padding:0;Margin:0;padding-left:40px;padding-right:40px""><p style=""Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-size:16px;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;line-height:24px;color:#666666;text-align:center"">Chào bạn,&nbsp;{userName}</p></td>
                     </tr>
                     <tr style=""border-collapse:collapse"">
                      <td align=""left"" style=""padding:0;Margin:0;padding-right:35px;padding-left:40px""><p style=""Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-size:16px;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;line-height:24px;color:#666666;text-align:center"">Bạn vừa xác nhận thay đổi mật khẩu</p></td>
                     </tr>
                     <tr style=""border-collapse:collapse"">
                      <td align=""center"" style=""padding:0;Margin:0;padding-top:25px;padding-left:40px;padding-right:40px""><p style=""Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-size:16px;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;line-height:24px;color:#666666"">Bên dưới là mật khẩu của bạn, Vui lòng không tiết lộ tài khoản và mật khẩu của bạn ra bên ngoài !</p></td>
                     </tr>
                     <tr style=""border-collapse:collapse"">
                      <td align=""center"" style=""Margin:0;padding-left:10px;padding-right:10px;padding-top:40px;padding-bottom:40px""><span class=""es-button-border"" style=""border-style:solid;border-color:#259cd8;background:#FFFFFF;border-width:2px;display:inline-block;border-radius:10px;width:auto""><a href=""#"" class=""es-button"" style=""mso-style-priority:100 !important;text-decoration:none;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:arial, 'helvetica neue', helvetica, sans-serif;font-size:14px;color:#259cd8;border-style:solid;border-color:#FFFFFF;border-width:15px 20px 15px 20px;display:inline-block;background:#FFFFFF;border-radius:10px;font-weight:bold;font-style:normal;line-height:17px;width:auto;text-align:center"">Mật khẩu: {randompass}</a></span></td>
                     </tr>
                   </tbody></table></td>
                 </tr>
               </tbody></table></td>
             </tr>
             <tr style=""border-collapse:collapse"">
              <td style=""padding:0;Margin:0;padding-left:10px;padding-right:10px;padding-top:20px;background-position:center center"" align=""left""><!--[if mso]><table style=""width:580px"" cellpadding=""0"" cellspacing=""0""><tr><td style=""width:199px"" valign=""top""><![endif]-->
               <table class=""es-left"" cellspacing=""0"" cellpadding=""0"" align=""left"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:left"">
                 <tbody><tr style=""border-collapse:collapse"">
                  <td align=""left"" style=""padding:0;Margin:0;width:199px"">
                   <table style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-position:center center"" width=""100%"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                     <tbody><tr style=""border-collapse:collapse"">
                      <td class=""es-m-txt-c"" align=""right"" style=""padding:0;Margin:0;padding-top:15px""><p style=""Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-size:16px;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;line-height:24px;color:#666666""><strong>Theo dõi:</strong></p></td>
                     </tr>
                   </tbody></table></td>
                 </tr>
               </tbody></table><!--[if mso]></td><td style=""width:20px""></td><td style=""width:361px"" valign=""top""><![endif]-->
               <table class=""es-right"" cellspacing=""0"" cellpadding=""0"" align=""right"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;float:right"">
                 <tbody><tr style=""border-collapse:collapse"">
                  <td align=""left"" style=""padding:0;Margin:0;width:361px"">
                   <table style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px;background-position:center center"" width=""100%"" cellspacing=""0"" cellpadding=""0"" role=""presentation"">
                     <tbody><tr style=""border-collapse:collapse"">
                      <td class=""es-m-txt-c"" align=""left"" style=""padding:0;Margin:0;padding-bottom:5px;padding-top:10px;font-size:0"">
                       <table class=""es-table-not-adapt es-social"" cellspacing=""0"" cellpadding=""0"" role=""presentation"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px"">
                         <tbody><tr style=""border-collapse:collapse"">
                          <td valign=""top"" align=""center"" style=""padding:0;Margin:0;padding-right:10px""><img src=""https://tlr.stripocdn.email/content/assets/img/social-icons/rounded-gray/facebook-rounded-gray.png"" alt=""Fb"" title=""Facebook"" width=""32"" style=""display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic""></td>
                          <td valign=""top"" align=""center"" style=""padding:0;Margin:0;padding-right:10px""><img src=""https://tlr.stripocdn.email/content/assets/img/social-icons/rounded-gray/twitter-rounded-gray.png"" alt=""Tw"" title=""Twitter"" width=""32"" style=""display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic""></td>
                          <td valign=""top"" align=""center"" style=""padding:0;Margin:0;padding-right:10px""><img src=""https://tlr.stripocdn.email/content/assets/img/social-icons/rounded-gray/instagram-rounded-gray.png"" alt=""Ig"" title=""Instagram"" width=""32"" style=""display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic""></td>
                          <td valign=""top"" align=""center"" style=""padding:0;Margin:0;padding-right:10px""><img src=""https://tlr.stripocdn.email/content/assets/img/social-icons/rounded-gray/youtube-rounded-gray.png"" alt=""Yt"" title=""Youtube"" width=""32"" style=""display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic""></td>
                          <td valign=""top"" align=""center"" style=""padding:0;Margin:0;padding-right:10px""><img src=""https://tlr.stripocdn.email/content/assets/img/social-icons/rounded-gray/linkedin-rounded-gray.png"" alt=""In"" title=""Linkedin"" width=""32"" style=""display:block;border:0;outline:none;text-decoration:none;-ms-interpolation-mode:bicubic""></td>
                         </tr>
                       </tbody></table></td>
                     </tr>
                   </tbody></table></td>
                 </tr>
               </tbody></table><!--[if mso]></td></tr></table><![endif]--></td>
             </tr>
             <tr style=""border-collapse:collapse"">
              <td style=""Margin:0;padding-top:5px;padding-bottom:20px;padding-left:20px;padding-right:20px;background-position:left top"" align=""left"">
               <table width=""100%"" cellspacing=""0"" cellpadding=""0"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px"">
                 <tbody><tr style=""border-collapse:collapse"">
                  <td valign=""top"" align=""center"" style=""padding:0;Margin:0;width:560px"">
                   <table width=""100%"" cellspacing=""0"" cellpadding=""0"" role=""presentation"" style=""mso-table-lspace:0pt;mso-table-rspace:0pt;border-collapse:collapse;border-spacing:0px"">
                     <tbody><tr style=""border-collapse:collapse"">
                      <td align=""center"" style=""padding:0;Margin:0""><p style=""Margin:0;-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-size:14px;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;line-height:21px;color:#666666"">Liên hệ: <a target=""_blank"" style=""-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;font-size:14px;text-decoration:none;color:#666666"" href=""#"">0966160922</a> | <a target=""_blank"" href=""#"" style=""-webkit-text-size-adjust:none;-ms-text-size-adjust:none;mso-line-height-rule:exactly;font-family:helvetica, 'helvetica neue', arial, verdana, sans-serif;font-size:14px;text-decoration:none;color:#666666"">trankhoa192837@gmail.com</a></p></td>
                     </tr>
                   </tbody></table></td>
                 </tr>
               </tbody></table></td>
             </tr>
           </tbody></table></td>
         </tr>
       </tbody></table>
       
       
    </td>
     </tr>
   </tbody></table>
  </div>
 </body>
</html>";

                    acc.Password =HashMD5.ToMD5(randompass.ToString());
                    db.SaveChanges();

                    string subject = "THƯ RESET MẬT KHẨU TÀI KHOẢN VTUANMAI!";
                    string mailTitle = "[Cửa hàng PetShop !]";
                    string fromEmail = "trankhoa192837@gmail.com";
                    string fromEmailPassword = "nnpbsdgxqcrdxcyj";

                    //Email Content
                    MailMessage mailMessage = new MailMessage(new MailAddress(fromEmail, mailTitle), new MailAddress(cus.UserName));
                    mailMessage.Subject = subject;
                    mailMessage.Body = Mailbody;
                    mailMessage.IsBodyHtml = true;

                    //Server Details
                    SmtpClient smtp = new SmtpClient();
                    smtp.Host = "smtp.gmail.com";
                    smtp.Port = 587;
                    smtp.EnableSsl = true;
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;

                    //Credentials
                    System.Net.NetworkCredential credential = new System.Net.NetworkCredential();
                    credential.UserName = fromEmail;
                    credential.Password = fromEmailPassword;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = credential;

                    smtp.Send(mailMessage);
                    notyfService.Success("Cập nhật mật khẩu thành công !");
                    return RedirectToAction("notyfication","cus_account");
                }
                else
                {
                    notyfService.Error("Tài khoản của bạn không tồn tại !");
                    return View();
                }

            }
            notyfService.Error("Tài khoản của bạn không tồn tại !");
            return View();
        }

        public IActionResult ChangePassword()
        {
            var taikhoanID = HttpContext.Session.GetString("CustomerId");
            var acc = db.Accounts.SingleOrDefault(x => x.Id == int.Parse(taikhoanID));
            return View(acc);
        }

        [HttpPost]
        public IActionResult ChangePassword(int ID,string newpass="")
        {
            var acc = db.Accounts.SingleOrDefault(x => x.Id == ID);
            if (acc != null) 
            { 
                if(!string.IsNullOrEmpty(newpass))
                {
                    acc.Password=newpass.ToMD5();
                    db.SaveChanges();
                    return Json(new { success = true });
                }
                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false });
            }
        }
    }
}
