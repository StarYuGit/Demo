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
        #region 請假系統
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
                var powerAutomateUrl = "https://default895757f9726548b0b7cb03ceb12732.28.environment.api.powerplatform.com:443/powerautomate/automations/direct/cu/10/workflows/8f233be04bd949f5bb18c76c8effb7e5/triggers/manual/paths/invoke?api-version=1";

                // 準備要傳給 PA 的資料 (JSON 格式)
                var payload = new
                {
                    LeaveId = request.Id,
                    EmployeeName = request.EmployeeName,
                    leaveType = request.LeaveType,
                    leaveStartDate = request.StartDate.ToString("yyyy-MM-dd"),
                    leaveEndDate = request.EndDate.ToString("yyyy-MM-dd"),
                    reason = request.Reason
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
        // 顯示簽核管理頁面 (GET: /Home/Approval)
        public IActionResult Approval()
        {
            // 撈出狀態為「未審核」的請假單
            var pendingRequests = _mockDatabase
                                    .Where(r => r.Status == "未審核")
                                    .OrderBy(r => r.StartDate) // 依照請假開始時間排序 (貼心小功能)
                                    .ToList();

            return View(pendingRequests);
        }
        // 處理簽核動作
        [HttpPost]
        public IActionResult ProcessApproval(int id, string action, string managerComment)
        {
            var leaveRequest = _mockDatabase.FirstOrDefault(r => r.Id == id);

            if (leaveRequest == null)
            {
                return NotFound("找不到此張假單");
            }

            // 更新狀態
            if (action == "Approve")
            {
                leaveRequest.Status = "已核准";
            }
            else if (action == "Reject")
            {
                leaveRequest.Status = "已駁回";
            }

            // 儲存主管寫的審核理由 (對應你 Model 的小寫開頭 managerComment)
            leaveRequest.managerComment = managerComment;

            // 簽核完畢後，重新導回 Approval 清單頁面
            return RedirectToAction("Approval");
        }
        // 接收api
        [HttpPost]
        [Route("api/leave/update-status")] // PA 要打的網址將會是：https://你的網域/api/leave/update-status
        public IActionResult UpdateLeaveStatusFromPa([FromBody] PaWebhookRequest webhookData)
        {
            // 檢查收到的資料是否完整
            if (webhookData == null || webhookData.leaveId <= 0)
            {
                return BadRequest(new { message = "資料格式錯誤或缺少id" });
            }

            // 💡 這裡請換成你「實體資料庫」的查詢方式 (例如 Entity Framework 的 _context)
            var targetLeave = _mockDatabase.FirstOrDefault(x => x.Id == webhookData.leaveId);

            if (targetLeave == null)
            {
                return NotFound(new { message = $"找不到請假單號 #{webhookData.leaveId}" });
            }

            // 更新實體資料庫中的狀態與主管留言
            if (webhookData.approval_status == "Approve")
                targetLeave.Status = "已核准";
            if (webhookData.approval_status == "Reject")
                targetLeave.Status = "已駁回";
            targetLeave.managerComment = webhookData.comments;

            // 回傳 200 OK 給 Power Automate，代表我們已經成功收到並更新了
            return Ok(new
            {
                message = "狀態更新成功",
                leaveId = targetLeave.Id,
                currentStatus = targetLeave.Status
            });
        }
        // 簽核頁面
        [HttpGet]
        public IActionResult ReviewLeave(int id)
        {
            // 找出網址傳進來的特定假單
            var leaveRequest = _mockDatabase.FirstOrDefault(r => r.Id == id);

            if (leaveRequest == null)
            {
                return NotFound("找不到此張假單，可能已經被刪除或編號錯誤！");
            }

            return View(leaveRequest);
        }
        // 發送api

        // 接收api
        #endregion
        #region 員工資料頁面
        // 員工資料頁面
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
        #endregion
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        // 錯誤訊息
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
