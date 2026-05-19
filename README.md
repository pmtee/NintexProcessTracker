# Processtracker

A C# .NET 8 REST API for tracking business processes - built as a portfolio project

# Tech Stack 
- C# .NET 8 Minimal API
- Entity Framework Core + SQLite
- Repository Pattern + Interfaces 
- xUnit - 6 poassing test 
- Swagger Ui 
- Vanilla HTML/CSS/JS dashboard


# Architecture 
- 'Models.cs' - BusinessProcess model
- 'IProcessRepository.cs' - Interface contract
- 'ProcessRepository.cs' - EF Core Implementation
- 'AppDbContext.cs' - Database context 
- 'Program.cs' = API endpoints with DI 
- 'ProcessTest.cs' - 6 xUnit integration tests

## Run Locally 
'''bash dotnet run
# Open http://localhost:5033/swagger 
# Open http://localhost:5033/index.html
'''
## Author 
Phetho Mogotle Tlaka . [LinkedIn] (https://linkedin.com/in/phetho-tlaka) . [Portfolio](https://pmtee.github.io)
