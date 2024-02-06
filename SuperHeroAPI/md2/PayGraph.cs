using System;
using System.Collections.Generic;

namespace SuperHeroAPI.md2;

public partial class PayGraph
{
    public int Id { get; set; }

    public int? ContractId { get; set; }

    public DateOnly? ExpirationDate { get; set; }

    public decimal? DepositedAmount { get; set; }

    public decimal? AllSum { get; set; }

    public decimal? LeftToPay { get; set; }

    public DateOnly? Date40 { get; set; }

    public string? Bank { get; set; }
}
