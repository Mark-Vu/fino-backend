using FinoBackend.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using FinoBackend.Models;

namespace FinoBackend.Services;

public class UserService
{
    private readonly ApplicationDbContext _db;

    public UserService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<User?> GetUserByIdAsync(Guid id)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id);
    }
    
    // public Task<List<User>> GetAllUsersAsync()...
    // public Task<User> CreateUserAsync(User user)...
    // public Task<bool> DeleteUserAsync(int id)...
}