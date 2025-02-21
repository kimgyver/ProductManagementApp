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
