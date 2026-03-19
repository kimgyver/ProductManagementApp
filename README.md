# Projects in solution

- API: WebAPI project
- BackgroundProcessor: Background Worker project
- API.Test: Unit test (xUnit) project

# Used AWS Resources

- ~~Lambda function: LambdaEmailSender.js (Node js)~~ -> Converted to a background worker
- SQS: SendEmailQueue, EmailFailureQueue
- SES

# Database Entities

- **User**
- **Product**

# Features

### User

- **CRUD Operations**: Create, Read, Update, Delete
- **Register user**: Create a new user record in Users table, Send a welcome email via SQS -> ~~Lambda~~ Worker -> SES
- **Password hashing**: Store user passwords securely using hashing
- **Login**: Authenticate user & provide credentials (JWT & Session)
- **Logout**: Remove (invalidate) session

### Product

- **CRUD Operations**: Create, Read, Update, Delete
  - **Permission required**: The user has to have credentials for this

# User Registration flow

- **WebAPI** Inserts user information into "Users" table
- **WebAPI** sends a message (with email information) to **SQS (SendEmailQueue)**
- ~~**SQS (SendEmailQueue)** triggers **lambda (LambdaEmailSender)**~~
- A **Background Worker (EmailBackgroundWorker)** checks (or receives messages) **SQS (SendEmailQueue)**
- If there is a message, the worker sends a welcome email to the new user via **SES**
- If email sending fails, the worker sends a message (with email failure notification) to **SQS (EmailFailureQueue)**
- A **Background Worker (EmailFailureBackgroundWorker)** checks (or receives messages) **SQS (EmailFailureQueue)**
- If there is a message, the worker calls a **WebAPI** to update the Verified flag in "Users" table to false.
  (For making a WebAPI call, client (the worker) login and client's JWT token checking precede.)

# Technologies

- .NET WebAPI & Worker projects
- Input validation & custom validators
- Hashing: hash & verify
- Credential (JWT + session) verification
- CQRS (Command Query Responsibility Segregation)
- Fluent API
- Custom Exception Handling / Global Exception Handler
- AWS SQS, SES, ~~Lambda~~
- xUnit for Unit testing

# JWT Service: Key Points

- User logs in (token issued for API access)
- Background worker/client logs in (token issued for API calls)
- Any API endpoint requiring authentication/authorization

# UserCommandService / UserQueryService: Key Points

- UserCommandService: Handles user registration, update, deletion, and marking users as unverified (e.g., after email failure).
- UserQueryService: Handles user data retrieval and authentication (login/password verification).
- Called when:
  - User registers (data saved, welcome email triggered)
  - User updates or deletes their info
  - Email verification fails (Verified flag set to false)
  - User logs in (credentials checked)
  - User data is queried (profile, list)

# ProductCommandService / ProductQueryService: Key Points

- Authenticated user queries product data
- Admin user creates, updates, or deletes products

# PasswordHasherService / SessionService: Key Points

- PasswordHasherService: Responsible for securely hashing passwords during registration and verifying passwords during login.
- SessionService: Handles session data storage and cookie authentication for login/logout.
- Both are used whenever user authentication or credential management is required.

# JWT vs Session Authentication Structure

- The codebase uses both JWT and Session/Cookie authentication in parallel.
- JWT: Used for API token issuance and client authentication (stateless, for API calls).
- Session/Cookie: Used for storing user session data and maintaining login state (stateful, for web scenarios).
- Both mechanisms are enabled; login issues a JWT token and also sets session/cookie data.
- This allows flexible authentication for both API clients and web users.
