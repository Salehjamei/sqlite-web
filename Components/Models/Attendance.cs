namespace sqlite_web.Components.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; } // اتصال به دیتابیس کارمندان
        public DateTime? ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }

        // رابطه ناوبری (Navigation Property)
        public Employee Employee { get; set; }
    }
}
