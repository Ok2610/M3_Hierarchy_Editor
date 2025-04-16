# Installation guide

File updated on 10/04/2025.

## Prerequisite

### Database
- Download [PostgreSQL (latest version)](https://www.postgresql.org/download/)   
- Create the user:   
`sudo -u <nom d'utilisateur> psql`   

- Set up the database (execution order matters):   
`psql -U <nom d'utilisateur>`   
`CREATE DATABASE mmm;` (case-sensitive)   
`psql -U <nom_utilisateur> -d mmm -f <chemin_vers_le_fichier_"views.sql">`   
`psql -U <nom_utilisateur> -d mmm -f <chemin_vers_le_fichier_"tagset-views.sql">`   
`psql -U <nom_utilisateur> -d mmm -f <chemin_vers_le_fichier_"SMALL-postgres.sql">`   

### API
- Download [dotnet (latest version)](https://dotnet.microsoft.com/fr-fr/download).   
- Download [PhotoCube-Server](https://github.com/Ok2610/PhotoCube-Server).   
- Change the "DefaultConnection" line and provide the database user and password in the appsettings.json file located at the root of the PhotoCube-Server folder.    
Example: `Server = localhost; Port = 5432; Database = mmm; User Id = <utilisateur>; Password = '<password>';`   

### Front end
- Download [UnityHUB (dernière version)](https://unity.com/download).   
- Download [Unity (version 2020.3.27f1)](https://unity.com/releases/editor/archive).   

## Launching the project

1. Run the API. Execute the following command in a terminal: `dotnet run -project C:\...pathToTheProject...\ObjectCubeServer.csproj`   
2. Launch Unity.
3. Choose the scene in Unity (there should be only one).
4. Click the Play button at the top of the Unity editor.