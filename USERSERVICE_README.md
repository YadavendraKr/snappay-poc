# UserService - gRPC Microservice

## Overview
UserService is a gRPC-based microservice that provides user/customer profile information with role and status management. It serves as the central authority for user details and is accessed by other microservices (e.g., CustomerService) via gRPC communication.

## Architecture

### gRPC Services
UserService exposes three main gRPC operations:
1. **GetUserDetails** - Retrieves user details by user ID
2. **GetUserByEmail** - Retrieves user details by email address
3. **CreateUser** - Creates a new user in the system

### REST API Endpoints
UserService also provides REST endpoints for direct access:
- `GET /api/users/{id}` - Get user by ID
- `GET /api/users/email/{email}` - Get user by email
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

## User Model

```
User
├── Id (int) - Unique identifier
├── Name (string) - User full name
├── Email (string) - Email address
├── Phone (string) - Phone number
├── Role (string) - User role (Admin, User, Manager, etc.)
├── Status (string) - Account status (Active, Inactive, Suspended)
├── CreatedAt (DateTime) - Creation timestamp
└── UpdatedAt (DateTime) - Last update timestamp
```

## Integration with CustomerService

### How CustomerService Accesses UserService

CustomerService uses gRPC to communicate with UserService for retrieving user role and status information:

1. **Customer model** in CustomerService refers to users in UserService
2. **CustomerService gRPC Client** (`UserServiceGrpcClient`) establishes a connection to UserService
3. **Low-latency communication** via binary protocol ensures minimal overhead

### API Endpoint for Customer with User Details

New endpoint in CustomersController:
```
GET /api/customers/{id}/with-user-details
```

Response includes:
```json
{
  "customerId": 1,
  "name": "John Doe",
  "email": "john@example.com",
  "phone": "+1234567890",
  "wallet": 5000,
  "createdAt": "2024-01-01T00:00:00Z",
  "userRole": "Admin",
  "userStatus": "Active"
}
```

## Configuration

### UserService Launch URL
- **HTTP/REST**: `http://localhost:5300`
- **gRPC**: `http://localhost:5300`

### CustomerService Configuration
Edit `CustomerService/appsettings.json`:
```json
{
  "Services": {
    "UserServiceUrl": "http://localhost:5300"
  }
}
```

## Performance Optimizations

1. **Binary Protocol** - gRPC uses Protocol Buffers (binary serialization) for reduced payload size
2. **HTTP/2 Multiplexing** - Multiple concurrent requests over single connection
3. **Connection Pooling** - Reused channels for efficient resource utilization
4. **Minimal Latency** - Direct service-to-service communication without intermediaries

## Running UserService

### Prerequisites
- .NET 10.0 SDK or later
- Visual Studio or VS Code

### Start UserService
```bash
cd UserService
dotnet run
```

The service will start on `http://localhost:5300` and swagger UI will be available at `http://localhost:5300/swagger`

### gRPC Health Check
UserService is ready to serve gRPC requests once it's running.

## Testing gRPC Communication

### Using grpcurl (CLI tool)
```bash
# Install grpcurl
# Get user details
grpcurl -plaintext -d '{"userId": 1}' localhost:5300 UserService.Protos.UserService/GetUserDetails

# Get user by email
grpcurl -plaintext -d '{"email": "admin@snappay.com"}' localhost:5300 UserService.Protos.UserService/GetUserByEmail
```

### Using CustomerService
```bash
# Call the new endpoint that internally uses gRPC
curl http://localhost:5100/api/customers/1/with-user-details
```

## Data Model

### Seeded Users
By default, UserService comes with two seeded users:

1. **Admin User**
   - ID: 1
   - Name: Admin User
   - Email: admin@snappay.com
   - Role: Admin
   - Status: Active

2. **Regular User**
   - ID: 2
   - Name: Regular User
   - Email: user@snappay.com
   - Role: User
   - Status: Active

## Proto Definition

The gRPC service is defined in `Protos/user.proto`:

```protobuf
service UserService {
  rpc GetUserDetails (GetUserDetailsRequest) returns (UserDetailsResponse);
  rpc GetUserByEmail (GetUserByEmailRequest) returns (UserDetailsResponse);
  rpc CreateUser (CreateUserRequest) returns (UserDetailsResponse);
}
```

## Error Handling

### gRPC Error Codes
- `NOT_FOUND` (5) - User not found
- `INVALID_ARGUMENT` (3) - Invalid request parameters
- `INTERNAL` (13) - Internal server error

### REST API Error Handling
- `404 Not Found` - Resource not found
- `400 Bad Request` - Invalid input
- `500 Internal Server Error` - Server error

## Database

UserService uses Entity Framework Core with an in-memory database for POC purposes. The database is automatically created and seeded when the service starts.

To persist data in a real scenario, configure a proper database provider (SQL Server, PostgreSQL, etc.) in `OnConfiguring` method of `UserDbContext`.

## Future Enhancements

1. Persistent database integration (SQL Server/PostgreSQL)
2. Authentication and authorization via JWT tokens
3. User role-based access control (RBAC)
4. Audit logging for user actions
5. User activity tracking
6. Integration with external identity providers
