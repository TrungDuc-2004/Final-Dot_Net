using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Pet_Shop2.ModelsView
{
    public class RegisterAccount
    {
        [Key]
        public int CustomerId { get; set; }

        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string? FullName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập Email !")]
        [MaxLength(100)]
        public string? UserName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập Email !")]
        [MaxLength(150)]
        [DataType(DataType.EmailAddress)]
        [Remote(action: "ValidateEmail", controller: "Account")]
        public string? Email { get; set; }

        [MaxLength(11)]
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại !")]
        [Display(Name = "Điện thoại")]
        [DataType(DataType.PhoneNumber)]
        [Remote(action: "ValidatePhone", controller: "Account")]
        public string? Phone { get; set; }

        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu !")]
        [MinLength(6, ErrorMessage = "Bạn cần đặt tối thiểu 6 kí tự !")]
        public string? Password { get; set; }

        [MinLength(5, ErrorMessage = "Bạn cần đặt mật khẩu tối thiểu 6 kí tự !")]
        [Display(Name = "Nhập lại mật khẩu")]
        [Compare("Password", ErrorMessage = "Vui lòng nhập mật khẩu giống nhau !")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }
    }
}
