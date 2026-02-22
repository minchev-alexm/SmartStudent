using System;

namespace SmartStudent.Models
{
    public class VisitorLog
    {
        public int Id { get; set; }

        public string IP { get; set; }

        public string Browser { get; set; }

        public string OS { get; set; }

        public string Device { get; set; }

        public string Path { get; set; }

        public DateTime VisitTime { get; set; }
    }
}