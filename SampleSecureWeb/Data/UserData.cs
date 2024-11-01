using System;
using Microsoft.EntityFrameworkCore;
using SampleSecureWeb.Models;
namespace SampleSecureWeb.Data;

public class UserData : IUser
{
    private readonly ApplicationDbContext _context;

    public UserData(ApplicationDbContext context)
    {
        _context = context;
    }

    public User? GetUser(string username)
    {
        throw new NotImplementedException();
    }

    public User Login(User user)
    {
        var _user = _context.Users.FirstOrDefault(u => u.Username == user.Username);
        if(_user == null)
        {
            throw new Exception("User not found");
        }
        if(!BCrypt.Net.BCrypt.Verify(user.Password, _user.Password))
        {
            _user.LoginAttempts++;
            if (_user.LoginAttempts >= 3)
                {
                    _user.IsLocked = true;
                    _user.LockoutEnd = DateTime.UtcNow.AddMinutes(3);
                    _context.SaveChanges();
                    throw new Exception($"Account is locked due to too many failed attempts. Try again after {_user.LockoutEnd.Value.ToString("HH:mm:ss")}");
                }
                _context.SaveChanges();
                throw new Exception($"Password is incorrect. {3 - _user.LoginAttempts} attempts remaining");
        }

            _user.LoginAttempts = 0;
            _user.IsLocked = false;
            _user.LockoutEnd = null;
            _context.SaveChanges();

            return _user;
    }

    public User Registration(User user)
    {
        try
        {   
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }

    public void UpdateUser(User user)
    {
        throw new NotImplementedException();
    }
}

