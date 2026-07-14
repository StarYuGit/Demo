namespace Demo.Models
{
    public class PaWebhookRequest
    {
        public int leaveId { get; set; } // 傳回id
        public string? approval_status { get; set; } // 審核結果
        public string? comments { get; set; } // 審核意見
    }
}
