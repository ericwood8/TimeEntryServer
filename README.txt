
# TimeEntry

The TimeEntry is a .NET Aspire Starter App designed to streamline workforce management with a Blazor frontend, Web API backend, and optional Redis caching. This README provides clear steps for setting up, configuring, and running the project locally, making it accessible for both new and experienced developers.

The Aspire Starter Project is composed of several key elements:
- **Blazor Frontend**: A modern, interactive frontend framework that enables web applications to run client-side in the browser using WebAssembly.
- **Web API Backend**: A robust backend service built using ASP.NET Core, designed to handle data management and business logic.
- **Redis Caching (Optional)**: Redis is used as a caching layer to improve application performance by storing frequently accessed data in memory.
- **Cloud Support**: Built to integrate with cloud services for scalability and enhanced functionality.
- **Service Layer**: Contains common services required by the application, providing modularity and separation of concerns.
- **Common Utilities**: A shared library with utilities and helper functions to simplify development and enhance reusability.

---

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation](#installation)
3. [Project Setup](#project-setup)
4. [Running the Project](#running-the-project)
5. [Additional Configuration](#additional-configuration)
6. [Troubleshooting](#troubleshooting)
7. [Contributing](#contributing)

---

## Prerequisites

Before beginning, make sure you have the following tools installed:

1. **Visual Studio 2022**
   - Download and install from [Visual Studio Official Site](https://visualstudio.microsoft.com/).
   - During installation, select **.NET desktop development** and **ASP.NET and web development** workloads.

2. **.NET 8 SDK**
   - Download from [.NET Official Website](https://dotnet.microsoft.com/download/dotnet/8.0).
   - This is essential for building and running .NET 8 applications, including the TimeEntry.

3. **Redis (Optional)**
   - If you plan to use Redis for caching, you will need to have a Redis server running locally or remotely.
   - For local testing, you can use Docker to run Redis:
     ```bash
     docker run -d --name redis -p 6379:6379 redis
     ```

---

## Installation

### Step 1: Clone the Repository

1. Open a terminal or command prompt.
2. Clone the repository with the following command:

   ```bash
   git clone https://github.com/yourusername/TimeEntry.git
   ```

3. Navigate to the project directory:

   ```bash
   cd TimeEntry
   ```

### Step 2: Open the Project in Visual Studio

1. Launch **Visual Studio 2022**.
2. Select **Open a project or solution**.
3. Navigate to the cloned `TimeEntry` folder and open the `.sln` file to load the solution in Visual Studio.

---

## Project Setup

### Step 1: Restore NuGet Packages

1. In **Solution Explorer**, right-click on the solution (top-level project name) and select **Restore NuGet Packages**.
2. This will download and install all dependencies required by the project.

### Step 2: Configure App Settings

1. In **Solution Explorer**, locate `appsettings.json` in the main project folder.
2. Open `appsettings.json` and configure the necessary settings, including:
   - **Database Connection** (e.g., SQL Server)
   - **Redis Configuration** (if using Redis caching)
   
   Example configuration for Redis:
   ```json
   "Redis": {
       "ConnectionString": "localhost:6379",
       "InstanceName": "WorkForceCache"
   }
   ```

3. Save your changes.

### Step 3: Set Up the Database (Optional)

If your project requires a database, follow these steps to set up the database:

1. Open **Package Manager Console** in Visual Studio:
   - Go to **Tools** > **NuGet Package Manager** > **Package Manager Console**.

2. Run the following command to apply migrations:

   ```powershell
   Update-Database
   ```

   This command will create the database schema based on the defined models and migrations.

---

## Running the Project

1. **Set the Startup Project**:
   - In **Solution Explorer**, right-click the main project (e.g., `TimeEntry.Web`) and select **Set as StartUp Project**.

2. **Run the Project**:
   - Click the **Run** button (or press `F5`) to start the application.
   - Visual Studio will compile the application and open it in a browser if it’s a web application.

3. **Access the Application**:
   - By default, the application will run at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).
   - Use this URL to access the Blazor frontend and interact with the Web API.

---

## Additional Configuration

### Redis Configuration (Optional)

If you are using Redis for caching, ensure that your Redis server is running. You can configure the Redis server details in `appsettings.json`.

Example Redis configuration in `appsettings.json`:

```json
"Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "WorkForceCache"
}
```

### Environment Variables

Some settings may require environment variables. To add them in Visual Studio:

1. Right-click on the project in **Solution Explorer** and go to **Properties**.
2. In the **Debug** section, add any environment variables required by the application.

---

## Troubleshooting

If you run into issues, here are some common solutions:

1. **NuGet Package Issues**:
   - Right-click the solution and select **Restore NuGet Packages** to ensure all dependencies are installed.

2. **Redis Connection Issues**:
   - Confirm that Redis is running. Use `docker ps` to check if the Redis container is active or start it using the command provided above.

3. **Database Errors**:
   - Ensure your connection string in `appsettings.json` is correctly configured.
   - Verify that your SQL Server instance is running and accessible.

4. **Port Conflicts**:
   - If the application doesn’t start, it might be due to a port conflict. Update the launch settings in `launchSettings.json` to use a different port.

If you need further assistance, check the **Issues** section on the GitHub repository or contact the maintainers.

---

## Contributing

1. **Fork the Repository**: Click on the **Fork** button on the GitHub repository.
2. **Create a New Branch**: Use a descriptive branch name, like `feature/add-new-feature`.
3. **Make Changes**: Commit your changes and push to the branch.
4. **Submit a Pull Request**: Once your changes are ready, submit a pull request for review.

Thank you for contributing to the TimeEntry project!
