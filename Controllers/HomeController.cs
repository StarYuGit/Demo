using System.Diagnostics;
using Demo.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        // 模擬資料庫：用 static 確保每次 Request 來的時候資料都還在
        private static List<LeaveRequest> _mockDatabase = new List<LeaveRequest>();
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }
        //申請與查詢合併頁面
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Leave()
        {
            // 將目前資料庫的紀錄傳給前端顯示
            return View(_mockDatabase);
        }
        [HttpPost]
        public async Task<IActionResult> SubmitLeave(LeaveRequest request)
        {
            // 1. 完善基礎資料與狀態
            request.Id = _mockDatabase.Count + 1; // 簡單產生單號
            request.Status = "未審核"; // 初始狀態

            // 2. 存入模擬資料庫
            _mockDatabase.Add(request);

            // 3. 通知 Power Automate
            try
            {
                using var client = new HttpClient();
                // 替換成你在 Power Automate 產生的 HTTP 觸發器 URL
                var powerAutomateUrl = "https://prod-xx.westus.logic.azure.com:443/workflows/...";

                // 準備要傳給 PA 的資料 (JSON 格式)
                var payload = new
                {
                    LeaveId = request.Id,
                    EmployeeName = request.EmployeeName,
                    LeaveType = request.LeaveType,
                    Reason = request.Reason
                };

                // 發送 POST 請求給 PA
                await client.PostAsJsonAsync(powerAutomateUrl, payload);
            }
            catch (Exception ex)
            {
                // Demo 期間如果 PA 網址沒設定好，先攔截錯誤避免當機
                Console.WriteLine("通知 Power Automate 失敗: " + ex.Message);
            }

            // 4. 重新導向回查詢頁面
            return RedirectToAction("Leave");
        }
        // 為了讓 API 乾淨，我們另外建一個接收狀態的物件
        public class StatusUpdateRequest
        {
            public int LeaveId { get; set; }
            public string? NewStatus { get; set; } // 預期收到 "已核准" 或 "已退回"
        }

        // 開放一個 API 端點讓 Power Automate 呼叫
        [HttpPost]
        [Route("api/leave/update-status")] // 設定 API 路徑
        public IActionResult UpdateLeaveStatus([FromBody] StatusUpdateRequest updateData)
        {
            // 在模擬資料庫中尋找這筆請假單
            var targetLeave = _mockDatabase.FirstOrDefault(x => x.Id == updateData.LeaveId);

            if (targetLeave == null)
            {
                return NotFound(new { message = "找不到此請假單號" });
            }

            // 更新狀態
            targetLeave.Status = updateData.NewStatus;

            // 回傳 200 OK 給 Power Automate
            return Ok(new { message = "狀態更新成功", currentStatus = targetLeave.Status });
        }

        // 1. 顯示簽核管理頁面 (GET: /Home/Approval)
        public IActionResult Approval()
        {
            // 💡 改用 _mockDatabase：撈出狀態為「未審核」的請假單
            var pendingRequests = _mockDatabase
                                    .Where(r => r.Status == "未審核")
                                    .ToList();

            return View(pendingRequests);
        }

        // 2. 處理核准或駁回動作 (POST: /Home/ProcessApproval)
        [HttpPost]
        public IActionResult ProcessApproval(int id, string action)
        {
            // 💡 改用 _mockDatabase：尋找指定的請假單
            var leaveRequest = _mockDatabase.FirstOrDefault(r => r.Id == id);

            if (leaveRequest == null)
            {
                return NotFound("找不到此張假單");
            }

            // 根據主管點擊的按鈕更新狀態
            if (action == "Approve")
            {
                leaveRequest.Status = "已核准";
            }
            else if (action == "Reject")
            {
                leaveRequest.Status = "已退回";
            }

            // (因為是參考型別 (Reference Type)，直接修改 List 裡的物件屬性就等於存檔了)

            // 簽核完畢後，重新導回 Approval 頁面整理清單
            return RedirectToAction("Approval");
        }

        public IActionResult Employee(string searchName, int? selectedId)
        {
            var viewModel = new EmployeeIndexViewModel
            {
                SearchName = searchName
            };

            if (!string.IsNullOrEmpty(searchName))
            {
                // 找出所有同名的員工
                viewModel.SearchResults = MockDatabase.Employees
                    .Where(e => e.Name == searchName)
                    .ToList();

                if (!viewModel.SearchResults.Any())
                {
                    viewModel.Message = "找不到此員工，請確認姓名是否正確。";
                }
            }

            // 若有選取特定項目，展開該員工卡片
            if (selectedId.HasValue)
            {
                viewModel.SelectedEmployee = MockDatabase.Employees.FirstOrDefault(e => e.Id == selectedId.Value);
            }

            return View(viewModel);

        }
        // 處理更新等級
        [HttpPost]
        public IActionResult UpdateGrade(int id, string newGrade, string searchName)
        {
            var employee = MockDatabase.Employees.FirstOrDefault(e => e.Id == id);

            if (employee != null && !string.IsNullOrEmpty(newGrade))
            {
                employee.Grade = newGrade;
            }

            // 更新成功後，重新導向並保留搜尋狀態與選取的項目
            return RedirectToAction("Employee", new { searchName = searchName, selectedId = id });
        }
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
