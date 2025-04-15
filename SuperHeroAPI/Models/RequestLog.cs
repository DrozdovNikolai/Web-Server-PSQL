using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SuperHeroAPI.Models
{
    [Table("ums_request_logs")]
    public class RequestLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }
        
        [Column("user_id")]
        public int? UserId { get; set; }
        
        [Column("path")]
        public string Path { get; set; } = string.Empty;
        
        [Column("method")]
        public string Method { get; set; } = string.Empty;
        
        [Column("query_string")]
        public string QueryString { get; set; } = string.Empty;
        
        [Column("request_body")]
        public string RequestBody { get; set; } = string.Empty;
        
        [Column("response_body")]
        public string ResponseBody { get; set; } = string.Empty;
        
        [Column("status_code")]
        public int StatusCode { get; set; }
        
        [Column("request_time", TypeName = "timestamp without time zone")]
        public DateTime RequestTime { get; set; }
        
        [Column("response_time", TypeName = "timestamp without time zone")]
        public DateTime ResponseTime { get; set; }
        
        [Column("duration")]
        public TimeSpan Duration { get; set; }
        
        [Column("ip_address")]
        public string IPAddress { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
} 