namespace Demo.Models
{
    // 員工資料模型
    public class Employee
    {
        public int Id { get; set; } // 系統內部更新用，不顯示於前端
        public string? Name { get; set; }
        public string? Grade { get; set; }
    }

    // 單一頁面專用的 ViewModel
    public class EmployeeIndexViewModel
    {
        public string? SearchName { get; set; }
        public List<Employee> SearchResults { get; set; } = new List<Employee>();
        public Employee? SelectedEmployee { get; set; }
        public string? Message { get; set; }
    }

    // 模擬記憶體資料庫
    public static class MockDatabase
    {
        public static List<Employee> Employees = new List<Employee>
        {
            new Employee { Id = 1, Name = "王大明", Grade = "一等專員" },
            new Employee { Id = 2, Name = "李小華", Grade = "二等專員" }, // 完全靠等級區分的同名員工
            new Employee { Id = 3, Name = "李小華", Grade = "三等專員" },
            new Employee { Id = 4, Name = "李四", Grade = "四等專員" }
        };
    }
}
