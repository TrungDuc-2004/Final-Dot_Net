using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Pet_Shop2.Models;
using Pet_Shop2.ModelsView;
using System;
using System.Security.Cryptography;
using WebTinTuc.OnlinePayment;

namespace Pet_Shop2.Controllers
{
    [Authorize]
    public class Checkout : Controller
    {
        PetShopContext db;
        INotyfService notyfService;
        public Checkout(PetShopContext db, INotyfService notyfService)
        {
            this.db = db;
            this.notyfService = notyfService;
        }
        public IActionResult Index()
        {
            ViewBag.tinh = db.Locations.ToList();
            ViewBag.lstGioHang = HttpContext.Session.Get<List<CartItem>>("GioHang");
            ViewBag.CusID = HttpContext.Session.GetString("CustomerId");
            if (ViewBag.CusID != null)
                ViewBag.Acc = HttpContext.Session.GetString("UserName");


            var CusID = HttpContext.Session.GetString("CustomerId");
            if (CusID != null)
                ViewBag.Acc = db.Accounts.SingleOrDefault(x => x.Id == int.Parse(CusID));
            if (CusID != null)
            {
                var customer = db.Accounts.SingleOrDefault(x => x.Id == int.Parse(CusID));
                ViewBag.acc = customer;
                return View();
            }
            return View();

        }
        [HttpPost]
        public IActionResult Themdonhang(OrderViewModel order)
        {

            var lstCart = HttpContext.Session.Get<List<CartItem>>("GioHang");
            var IDCus = HttpContext.Session.GetString("CustomerId");


            string? province = db.Locations.SingleOrDefault(x => x.Id == order._province)?.Name;
            string? district = db.Districts.SingleOrDefault(x => x.Id == order._district)?.Name;
            string? ward = db.Wards.SingleOrDefault(x => x.WardId == order._ward)?.Name;
            var orderid = -1;
            if (IDCus != null)
            {
                var Acc = db.Accounts.SingleOrDefault(x => x.Id == int.Parse(IDCus));
                if (Acc != null)
                {
                    Acc.Location = province;
                    Acc.District = district;
                    Acc.Ward = ward;
                    Acc.Address = province + "," + district + "," + ward + "," + order.stresshouse;
                    db.SaveChanges();
                }
                Order ord = new Order()
                {
                    AccountId = Acc?.Id,
                    OrderDate = DateTime.Now,
                    ShipDate = DateTime.Now.AddDays(3),
                    Deleted = false,
                    Paid = false,
                    Note = order.ordernote,
                    TransctStatusId = 1,
                    Address = province + "," + district + "," + ward + "," + order.stresshouse,
                    Payway = order.select_pay
                };
                db.Orders.Add(ord);

                db.SaveChanges();
                orderid = ord.Id;
                decimal total = 0;
                foreach (var item in lstCart)
                {
                    total += (decimal)item.TotalMoney;
                    OrderDetail orderDetail = new OrderDetail()
                    {
                        OrderId = ord.Id,
                        ProductId = item?.product?.Id,
                        Quantity = item?.amount,
                        Total = (decimal)item.TotalMoney

                    };
                    db.OrderDetails.Add(orderDetail);
                    db.SaveChanges();
                }
                if (order.select_pay == "momo")
                {
                    return Redirect($"Payment?total={total}&sessionId={orderid}");
                }
            }
            lstCart = null;
            HttpContext.Session.Set<List<CartItem>>("GioHang", lstCart);
            return Json(new { success = true, OrderId = orderid });
        }

        public IActionResult Payment(string total = "0", string sessionId = "")
        {
            //request params need to request to MoMo system
            string endpoint = "https://test-payment.momo.vn/gw_payment/transactionProcessor";
            string partnerCode = "MOMOOJOI20210710";
            string accessKey = "iPXneGmrJH0G8FOP";
            string serectkey = "sFcbSGRSJjwGxwhhcEktCHWYUuTuPNDB";
            string orderInfo = "Quét mã QR để thanh toán";
            string returnUrl = $"https://localhost:7070/Checkout/ConfirmPaymentClient?sessionId={sessionId}";
            string notifyurl = "https://4c8d-2001-ee0-5045-50-58c1-b2ec-3123-740d.ap.ngrok.io/Cart/SavePayment"; //lưu ý: notifyurl không được sử dụng localhost, có thể sử dụng ngrok để public localhost trong quá trình test

            string amount = total;
            string orderid = DateTime.Now.Ticks.ToString(); //mã đơn hàng
            string requestId = DateTime.Now.Ticks.ToString();
            string extraData = "";

            //Before sign HMAC SHA256 signature
            string rawHash = "partnerCode=" +
                partnerCode + "&accessKey=" +
                accessKey + "&requestId=" +
                requestId + "&amount=" +
                amount + "&orderId=" +
                orderid + "&orderInfo=" +
                orderInfo + "&returnUrl=" +
                returnUrl + "&notifyUrl=" +
                notifyurl + "&extraData=" +
                extraData;

            MoMoSecurity crypto = new MoMoSecurity();
            //sign signature SHA256
            string signature = crypto.signSHA256(rawHash, serectkey);

            //build body json request
            JObject message = new JObject
            {
                { "partnerCode", partnerCode },
                { "accessKey", accessKey },
                { "requestId", requestId },
                { "amount", amount },
                { "orderId", orderid },
                { "orderInfo", orderInfo },
                { "returnUrl", returnUrl },
                { "notifyUrl", notifyurl },
                { "extraData", extraData },
                { "requestType", "captureMoMoWallet" },
                { "signature", signature }

            };

            string responseFromMomo = PaymentRequest.sendPaymentRequest(endpoint, message.ToString());

            JObject jmessage = JObject.Parse(responseFromMomo);

            return Redirect(jmessage.GetValue("payUrl").ToString());
        }
        //Khi thanh toán xong ở cổng thanh toán Momo, Momo sẽ trả về một số thông tin, trong đó có errorCode để check thông tin thanh toán
        //errorCode = 0 : thanh toán thành công (Request.QueryString["errorCode"])
        //Tham khảo bảng mã lỗi tại: https://developers.momo.vn/#/docs/aio/?id=b%e1%ba%a3ng-m%c3%a3-l%e1%bb%97i
        public IActionResult ConfirmPaymentClient(string errorCode, string sessionId)
        {
            //lấy kết quả Momo trả về và hiển thị thông báo cho người dùng (có thể lấy dữ liệu ở đây cập nhật xuống db)
            if (errorCode == "0")
            {
                return RedirectToAction(nameof(Success));
            }
            var order = db.Orders.FirstOrDefault(o => o.Id == Int32.Parse(sessionId));

            if (order == null)
            {
                notyfService.Error("Không tìm thấy đơn hàng để cập nhật.");
                return RedirectToAction("Checkout");
            }

            order.TransctStatusId = 3;
            db.SaveChanges();
            notyfService.Error("Thanh toán không thành công! Vui lòng thử lại.");
            return RedirectToAction("Checkout");
        }


        public IActionResult Success()
        {
            HttpContext.Session.Set<List<CartItem>>("GioHang", null);
            return RedirectToAction("Index");
        }
    }
}
