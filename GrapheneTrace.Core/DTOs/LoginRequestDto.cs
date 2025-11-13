using System.ComponentModel.DataAnnotations;

namespace GrapheneTrace.Core.DataTransferObjects
{
    public class LoginRequestDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}