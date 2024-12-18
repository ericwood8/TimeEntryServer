# TimeEntry
This is the API layer for the TimeEntryUI and TimeEntryDB repos. 

The TimeEntry is a .NET Aspire designed to streamline workforce management.
Its purpose is for employees to be able to:
  1) enter their time for the day
  2) request leave, clearance, expense reimbursement, and overtime

The Time Entry Server repo piece uses C#, has minimal APIs, uses .NET Aspire, exposes endpoints including ones for Swagger to use, uses entity framework (EF) 
to connect to the MS SQL Server database. 

--

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

1. **.Visual Studio 2022 or later.**
   - Download and install from [Visual Studio Official Site](https://visualstudio.microsoft.com/).  **Free community version is fine.**
   - During installation, select **.NET desktop development** and **ASP.NET and web development** workloads.

2. **.NET 9 SDK**
   - Download from [.NET Official Website] (https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
   - This is essential for building and running .NET 9 applications, including the TimeEntry.
  
3. **Microsoft SQL Server**
    - Download and install MS SQL Server:  https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    - **The free Express version is fine** 

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

1. Launch ** Visual Studio **.
2. Select **Open a project or solution**.
3. Navigate to the cloned `TimeEntry` folder and open the `.sln` file to load the solution in Visual Studio.

---

## Project Setup

### Step 1: Restore NuGet Packages

1. In **Solution Explorer**, right-click on the solution (top-level project name) and select **Restore NuGet Packages**.
2. This will download and install all dependencies required by the project.

### Step 2: Configure App Settings

1. In **Solution Explorer**, locate `appsettings.json` in the main project folder.
2. Open `appsettings.json` for the ApiService and AppHost projects and configure the necessary settings, including:
   - **Database Connection String ** (e.g., SQL Server)
3. Open `TimeEntryContextFactory.cs` for the Data project and configure the necessary settings, including:
   - **Database Connection String ** (e.g., SQL Server)
4. Save your changes.

### Step 3: Set Up the Database 

1. Unzip the zip files found in TimeEntryDB.
2. Follow the TimeEntryDB instructions to attach the database.

---

## Running the Project

1. **Set the Startup Project**:
   - In **Solution Explorer**, right-click the TimeEntry.AppHost project and select **Set as StartUp Project**.

2. **Run the Project**:
   - Click the **Run** button (or press `F5`) to start the application.
   - Visual Studio will compile the application and open it in the default browser.

3. **Access the Application**:
   - By default, the application will run at `https://localhost:5001` (HTTPS) or `http://localhost:5000` (HTTP).
   - Use this URL to access the Angular frontend and interact with the Web API.

---

## Contributing

1. **Fork the Repository**: Click on the **Fork** button on the GitHub repository.
2. **Create a New Branch**: Use a descriptive branch name, like `feature/add-new-feature`.
3. **Make Changes**: Commit your changes and push to the branch.
4. **Submit a Pull Request**: Once your changes are ready, submit a pull request for review.

Thank you for contributing to the TimeEntry project!
