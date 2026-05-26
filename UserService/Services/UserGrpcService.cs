using Grpc.Core;
using UserService.Models;
using UserService.Protos;
using Microsoft.EntityFrameworkCore;

namespace UserService.Services
{
    public class UserGrpcService : Protos.UserService.UserServiceBase
    {
        private readonly UserDbContext _context;
        private readonly ILogger<UserGrpcService> _logger;

        public UserGrpcService(UserDbContext context, ILogger<UserGrpcService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public override async Task<UserDetailsResponse> GetUserDetails(GetUserDetailsRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"gRPC: Getting user details for userId: {request.UserId}");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.UserId); // Use _context
            
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            return MapToUserDetailsResponse(user);
        }

        public override async Task<UserDetailsResponse> GetUserByEmail(GetUserByEmailRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"gRPC: Getting user by email: {request.Email}");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email); // Use _context
            
            if (user == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "User not found"));
            }

            return MapToUserDetailsResponse(user);
        }

        public override async Task<UserDetailsResponse> CreateUser(CreateUserRequest request, ServerCallContext context)
        {
            _logger.LogInformation($"gRPC: Creating new user with email: {request.Email}");
            var newUser = new User
            {
                Name = request.Name,
                Email = request.Email,
                Phone = request.Phone,
                Role = request.Role,
                Status = "Active",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(); // Use _context

            return MapToUserDetailsResponse(newUser);
        }

        private UserDetailsResponse MapToUserDetailsResponse(User user)
        {
            return new UserDetailsResponse
            {
                UserId = user.Id,
                Name = user.Name,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt.Ticks
            };
        }
    }
}
