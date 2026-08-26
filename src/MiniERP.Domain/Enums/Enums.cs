namespace MiniERP.Domain.Enums;

public enum PartnerType
{
    Principal,   // HTC: đơn vị nhập khẩu & phân phối
    Dealer,      // Đại lý bán lẻ
    Bank,        // Cấp bảo lãnh
    Insurance,   // Công ty bảo hiểm
    Transporter, // Nhà vận chuyển
}

public enum ContractStatus
{
    Draft,
    DealerSigned,
    ApprovedA1,
    ApprovedA2,
    Terminated,
}

public enum OrderStatus
{
    Demand,
    Supplied,
    ApprovedA1,
    ApprovedA2,
    Completed,
    Cancelled,
}

public enum StockItemStatus
{
    InStock,
    Reserved,
    Delivered,
}

public enum StockMovementType
{
    ReceiveFromFactory,
    TransferWarehouse,
    DeliverToDealer,
}

public enum PaymentType
{
    Deposit,
    GuaranteeFee,
}

public enum GuaranteeStatus
{
    Active,
    Cleared,
    Expired,
}

public enum InvoiceType
{
    Principal, // hóa đơn HTC
    Subsidiary, // hóa đơn TCG
    Other,
}

public enum InvoiceStatus
{
    Draft,
    Issued,
    Cancelled,
}
