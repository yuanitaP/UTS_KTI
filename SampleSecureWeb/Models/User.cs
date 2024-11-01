using System.ComponentModel.DataAnnotations;
namespace SampleSecureWeb.Models;

public class User
{
    [Key]
    public string Username{get;set;} = null!;
    public string Password{get;set;} = null!;
    public string RoleName{get;set;} = null!;

    public int LoginAttempts { get; set; } = 0; //menghitung jumlah percobaan login
    public bool IsLocked { get; set; } = false; // informasi status akun terkunci/tidak
    public DateTime? LockoutEnd { get; set; } // waktu akun terkunci
}
