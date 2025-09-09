using System.Runtime.Serialization;

namespace FinoBackend.Commons.Enums;

public enum FileCategory
{
    [EnumMember(Value = "bank_statement")]
    BankStatement,
    [EnumMember(Value = "delivery_receipt")]
    DeliveryReceipt
}