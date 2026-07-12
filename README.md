# Auth API

Production-ready authentication service built with ASP.NET Core, featuring JWT authentication, refresh tokens, and role-based authorization.


## 🚀 Features

- ✅ JWT Authentication with Access + Refresh Tokens
- ✅ Token Rotation for enhanced security
- ✅ Role-Based Authorization (Admin, User)
- ✅ Claim-Based Identity
- ✅ Password Hashing with BCrypt
- ✅ Request Validation with FluentValidation
- ✅ Result Pattern for standardized responses
- ✅ Global Exception Handling
- ✅ Structured Logging with Serilog
- ✅ Unit Testing with xUnit (Business Logic 90% Coverage)
- ✅ CI/CD Pipeline with GitHub Actions
- ✅ Docker Support

## 🔐 Security Features

- **Password Hashing**: BCrypt with automatic salt generation
- **JWT Tokens**: Signed with HMAC-SHA256
- **Refresh Token Rotation**: New token issued on each refresh
- **Token Expiry**: Access token (15 min), Refresh token (7 days)
- **Environment Variables**: Secrets stored securely
- **Input Validation**: All requests validated with FluentValidation


## 🛠️ Tech Stack

- **Backend:** ASP.NET Core 10.0
- **Database:** SQL Server (Local) / PostgreSQL (Production)
- **ORM:** Entity Framework Core
- **Authentication:** JWT, OAuth 2.0 (coming soon)
- **Validation:** FluentValidation
- **Testing:** xUnit, Moq, FluentAssertions
- **Logging:** Serilog
- **Containerization:** Docker
- **CI/CD:** GitHub Actions

## 🔮 Future Enhancements

- OAuth 2.0 Social Login (Google, GitHub)
- Two-Factor Authentication (2FA)
- Rate Limiting for API protection
- API Versioning
- Redis Caching for improved performance

## 🚀 Getting Started

### 1. Clone the repository
```bash
git clone https://github.com/codepnw/AuthAPI.git
cd AuthAPI
```

### 2. Setup environment variables
```bash
cp example.env .env
```
Edit .env file with your values:

```env
DB_PASSWORD=YourStrongPassword123!
JWT_SECRET_KEY=YourSuperSecretKeyAtLeast32CharactersLong!
```

### 3. Run with Docker Compose
```bash
docker compose up -d
```

### 4. Access the API
- **API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger

## 📚 API Documentation

**Authentication Endpoints**

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `POST` | `/api/auth/register` | Register new user |
| `POST` | `/api/auth/login` | Login |
| `POST` | `/api/auth/refresh` | Refresh access token |

- **Example: Login**
```bash
POST /api/auth/login
Content-Type: application/json
{
  "email": "user@example.com",
  "password": "Password123!"
}
```

- **Example: Response**
```bash
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "abc123...",
  "accessTokenExpiry": "2026-01-26T15:30:00Z"
}
```

**Protected Endponts**

Add JWT token in header:

```bash
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/auth/me` | Get current user |
| `GET` | `/api/products/admin-only` | Demo data for admin only |

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Test coverage services layer (business logic)
dotnet test -p:CollectCoverage=true -p:Include="[AuthAPI]AuthAPI.Services.*"
```

## 🏗️ Project Structure

```
AuthAPI/
├── Controllers/          # API endpoints
├── Services/             # Business logic
├── Models/               # Entities & DTOs
├── Data/                 # DbContext & Migrations
├── Validators/           # FluentValidation rules
├── Middleware/           # Global exception handler
├── DTOs/                 # Request/Response models
└── Program.cs            # Application entry point

AuthAPI.Tests/
├── Services/             # Unit tests
└── Helpers/              # Test utilities
```

## 📊 Logging

Logs are written to:
- **Console**: For development
- **File**: logs/log-YYYYMMDD.txt (rolling daily, retained 30 days)

Example log entry:
```
[14:30:15 INF] Login successful for user 1
[14:31:22 WRN] Login failed: wrong password for email admin@test.com
```

## 🚢 Deployment

**Build Docker image**
```
docker build -t authapi .
```

**Run with Docker**
```
docker run -d \
  -p 5000:8080 \
  -e ConnectionStrings__Default="YourConnectionString" \
  -e Jwt__Key="YourSecretKey" \
  authapi
```

## 📄 CI/CD

GitHub Actions automatically:
- Builds the project on every push
- Runs all unit tests
- Reports test results
- (Optional) Deploys to production

## 📝 Environment Variables

| Variable | Description | Example |
| :--- | :--- | :--- |
| `ConnectionStrings__Default` | Database connection string | `Server=localhost;Database=AuthAPI;...` |
| `Jwt__Key` | JWT signing key (min 32 chars) | `YourSuperSecretKey...` |
| `Jwt__Issuer` | JWT issuer | `AuthAPI` |
| `Jwt__Audience` | JWT audience | `AuthAPIClients` |
| `Jwt__AccessTokenExpiryMinutes` | Access token expiry | `15` |
| `Jwt__RefreshTokenExpiryDays` | Refresh token expiry | `7` |

See example.env for complete list.

## 👤 Author

**Phanuwat Kasemsuk**

- **Email**: phanuwat.code@gmail.com
