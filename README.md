# Time Entry
This is the API layer for the TimeEntryUI and TimeEntryDB repos. 

# VERSION 
**Version 1.0**  = Initial check-in. Shows CRUD (Create, Read, Update, Delete) functionality and Master\Detail screens.

**Version 1.01** = 12/31/2024 = Threatened with the "Separation of Concerns Authority" :-), everything was switched to repositories and
   where APIs do not know anything about DbContext. 

# Time Entry
The TimeEntry is a .NET Aspire designed to streamline workforce management.
Its purpose is for employees to be able to:
  1) enter their time for the day
  2) request leave, clearance, expense reimbursement, and overtime.

This is a work-in-progress repo.  The **purpose** of this application is to show the flow of data back and forth, not really to be a useful fully functioning application.
The application has CRUD (Create, Read, Update, Delete) functionality and Master\Detail screens.

The Time Entry Server repo piece uses _C#_, _.NET 9_, has _minimal APIs_, uses _.NET Aspire_, exposes endpoints including ones for _Swagger_ to use, uses _entity framework_ (EF) 
to connect to the _MS SQL Server_ database. 

IMPORTANT - 1) Run the TimeEntryServer solution in Visual Studio. This drives everything else.
  2) Change the "SQL Connection String" in 3 spots so it will work in your environment (see DB repo for details).

![ClickHere](https://github.com/user-attachments/assets/dff45eef-31ef-4e78-9416-44e5dd0db30b)
![Employees](https://github.com/user-attachments/assets/9d1aade3-7e04-4732-ad37-224c5a1fbb1e)
![EditDepartment](https://github.com/user-attachments/assets/0a467390-34f1-4397-a500-ff17015e6bd0)

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

1. **.Visual Studio 2022 or later.**
   - Download and install from [Visual Studio Official Site](https://visualstudio.microsoft.com/).  **Free community version is fine.**
   - During installation, select **.NET Aspire** workload:  ( https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dotnet-aspire-sdk ).
     (.NET Aspire requires .NET 8 or higher for .NET Aspire to work. AS of this date, Visual Studio ships with .NET 9. The projects contained
     within this repo are .NET 9).
   - Also during installation, I recommend selecting if you do not already have them: **.NET desktop development** and **ASP.NET and web development** workloads.

2. **.NET 9 SDK**
   - Download from [.NET Official Website] (https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
   - This is essential for building and running .NET 9 applications, including the TimeEntry.
  
3. **Microsoft SQL Server**
    - Download and install MS SQL Server:  https://www.microsoft.com/en-us/sql-server/sql-server-downloads
    - **The free Express version is fine**
      
4. Communication with database
   This solution requires communicating to a database.  Change the "SQL Connection String" in 3 spots so it will work in your environment.
Search for "ConnectionString" and fix all of them.

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

## To-Do

1) Authentication.  Potential inspiration at https://github.com/tbarracha/Angspire .
2) Investigate possible over-subscription of things in Angular.
3) Perhaps infusion of AI with potential inspiration at https://github.com/thangchung/practical-dotnet-aspire and https://dev.to/thangchung/coffeeshop-app-infused-with-ai-intelligent-apps-development-202k .
4) Add API pagination rather than purely Angular pagination.
   
---

## Contributing

1. **Fork the Repository**: Click on the **Fork** button on the GitHub repository.
2. **Create a New Branch**: Use a descriptive branch name, like `feature/add-new-feature`.
3. **Make Changes**: Commit your changes and push to the branch.
4. **Submit a Pull Request**: Once your changes are ready, submit a pull request for review.

Thank you for contributing to the TimeEntry project!
