namespace Demo.Models
{
    public class Employee
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Grade { get; set; }
    }
    // 模擬記憶體資料庫
    public static class MockDatabase
    {
        public static List<Employee> Employees = new List<Employee>
        {
            new Employee { Id = 1, Name = "王大明", Grade = "等級一" },
            new Employee { Id = 2, Name = "李小華", Grade = "等級二" },
            new Employee { Id = 3, Name = "張三", Grade = "等級三" }
        };
    }
}
