using Pet_Shop2.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pet_Shop2.Areas.Admin.Models
{
    public class AccountDetailsViewModel
    {
        public Account Account { get; set; }
        public List<OrderViewModel> Orders { get; set; }

        public AccountDetailsViewModel()
        {
            Orders = new List<OrderViewModel>();
        }
    }

    public class OrderViewModel
    {
        [Display(Name = "Mã ĐH")]
        public int OrderId { get; set; }

        [Display(Name = "Ngày đặt")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? OrderDate { get; set; }

        [Display(Name = "Ngày giao (DK)")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy HH:mm}")]
        public DateTime? ShipDate { get; set; }

        [Display(Name = "Trạng thái")]
        public string TransactionStatus { get; set; }

        [Display(Name = "Thanh toán")]
        public bool IsPaid { get; set; }

        [Display(Name = "Ngày TT")]
        [DisplayFormat(DataFormatString = "{0:dd/MM/yyyy}")]
        public DateTime? PaymentDate { get; set; }

        [Display(Name = "PTTT")]
        public string PaymentMethod { get; set; }

        [Display(Name = "Tổng tiền")]
        [DisplayFormat(DataFormatString = "{0:#,##0} VNĐ")]
        public decimal TotalOrderAmount { get; set; }

        [Display(Name = "Địa chỉ giao")]
        public string ShippingAddress { get; set; }

        public List<OrderDetailViewModel> OrderDetails { get; set; }

        public OrderViewModel()
        {
            OrderDetails = new List<OrderDetailViewModel>();
        }
    }

    public class OrderDetailViewModel
    {
        public int ProductId { get; set; }
        [Display(Name = "Sản phẩm")]
        public string ProductName { get; set; }
        [Display(Name = "Số lượng")]
        public int? Quantity { get; set; }
        [Display(Name = "Đơn giá")]
        [DisplayFormat(DataFormatString = "{0:#,##0}")]
        public int? UnitPrice { get; set; }
        [Display(Name = "Giảm giá")]
        [DisplayFormat(DataFormatString = "{0:#,##0}")]
        public int? Discount { get; set; }
        [Display(Name = "Thành tiền")]
        [DisplayFormat(DataFormatString = "{0:#,##0} VNĐ")]
        public decimal? LineTotal { get; set; }
    }
}