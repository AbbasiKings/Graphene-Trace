using GrapheneTrace.Core.Enums;

namespace GrapheneTrace.Core.DataTransferObjects
{
    public class LoginResponseDto
    {
        public bool Status { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public UserRole Role { get; set; }
        public string UserName { get; set; }
    }
}