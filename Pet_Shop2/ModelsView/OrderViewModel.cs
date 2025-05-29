using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Pet_Shop2.ModelsView
{
    public class OrderViewModel
    {
        public string ordernote { get; set;}
        public int _province { get; set; } = 0;
        public int _district { get; set; } = 0;
        public int _ward { get; set; } = 0;
        public string stresshouse { get; set; } = "";
        public string select_pay { get; set; } = "cod";

    }
}
