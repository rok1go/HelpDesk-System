helpdesk system

requirements:

Windows 10 or 11, .NET 8 SDK and Docker Desktop.
Docker Desktop must be running and port 5432 must be free. internet connection may be required for the first start.

commands:

    .\Scripts\start-local.cmd
    .\Scripts\start-local.cmd additional

the first command starts the database, applies migrations and opens the app.
run the second command in a new terminal to open another window using the same database.

initial admin:

email: `admin@helpdesk.local`
password: `Admin123!`

quick check:

run the first command and log in as the initial admin. open a second terminal and run the additional window command.
send a worker registration request in the second window, approve it in the admin window and create a ticket as the new worker.
refresh both windows to see the same ticket and status changes.

features:

workers can register, create tickets, add comments, view ticket history, close resolved tickets and browse published solutions.
administrators can approve registrations, take or decline tickets, resolve tickets and publish solutions.
ticket priority is calculated automatically. search and filters are available for tickets, registrations and the knowledge base.

technology:

C#, WPF, .NET 8, Entity Framework Core, PostgreSQL 17, Npgsql, Docker Compose, BCrypt, Microsoft Extensions Hosting and GitHub Actions.

data:

the database is stored in the Docker volume `helpdesk-data`, so the data remains available between restarts.
