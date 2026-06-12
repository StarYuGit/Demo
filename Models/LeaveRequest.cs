namespace Demo.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }
        public string? EmployeeName { get; set; } // 員工姓名
        public string? LeaveType { get; set; }     // 假別 (特休、病假等)
        public DateTime StartDate { get; set; }   // 開始時間
        public DateTime EndDate { get; set; }     // 結束時間
        public int TotalHours { get; set; }       // 請假總時數
        public string? Reason { get; set; }        // 請假事由
        public string? Status { get; set; }        // 審核狀態 (待審核、已核准)
    }
}

