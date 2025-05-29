using System;
using System.Collections.Generic;

namespace Pet_Shop2.Models;

public partial class Order
{
    public int Id { get; set; }

    public int? AccountId { get; set; }

    public string? Address { get; set; }

    public DateTime? OrderDate { get; set; }

    public DateTime? ShipDate { get; set; }

    public int? TransctStatusId { get; set; }

    public bool? Deleted { get; set; }

    public bool? Paid { get; set; }

    public DateTime? PaymentDate { get; set; }

    public int? PaymentId { get; set; }

    public string? Note { get; set; }
    public string? Payway { get; set; } //cod or momo

    public virtual Account? Account { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

    public virtual Transaction? TransctStatus { get; set; }
}