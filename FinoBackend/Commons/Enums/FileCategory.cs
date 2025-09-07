using System.Runtime.Serialization;

namespace FinoBackend.Commons.Enums;

public enum FileCategory
{
    [EnumMember(Value = "bank_statement")]
    Bank_Statement,
    [EnumMember(Value = "delivery_receipt")]
    Delivery_Receipt
}