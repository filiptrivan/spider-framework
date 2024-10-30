IF DB_ID('SoftGeneratorDA') IS NULL
BEGIN
    CREATE DATABASE SoftGeneratorDA;
END;
GO

USE SoftGeneratorDA;
GO

CREATE TABLE Company (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(500) NOT NULL,
    Email NVARCHAR(400) NOT NULL UNIQUE,
    Password NVARCHAR(100) NOT NULL
);

CREATE TABLE Permission (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(100) NOT NULL UNIQUE
);

INSERT INTO Permission (Name, Code) VALUES ('Dodavanje kompanije', 'InsertCompany');
INSERT INTO Permission (Name, Code) VALUES ('Menjanje kompanije', 'UpdateCompany');
INSERT INTO Permission (Name, Code) VALUES ('Brisanje kompanije', 'DeleteCompany');

CREATE TABLE Framework (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(500) NOT NULL,
    Code NVARCHAR(500) NOT NULL
);

CREATE TABLE DomainFolderPath (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    Path NVARCHAR(1000) NOT NULL
);

CREATE TABLE Setting (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    HasGoogleAuth BIT NOT NULL,
    PrimaryColor NCHAR(7) NOT NULL,
    HasLatinTranslate BIT NOT NULL,
    HasDarkMode BIT NOT NULL,
    HasNotifications BIT NOT NULL,
    FrameworkId INT NOT NULL,
    FOREIGN KEY (FrameworkId) REFERENCES Framework(Id)
);

CREATE TABLE Application (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    Name NVARCHAR(500) NOT NULL,
    CompanyId INT NOT NULL,
    SettingId BIGINT NOT NULL,
    FOREIGN KEY (CompanyId) REFERENCES Company(Id),
    FOREIGN KEY (SettingId) REFERENCES Setting(Id)
);

CREATE TABLE GeneratedFile (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    DisplayName NVARCHAR(500) NOT NULL,
    ClassName NVARCHAR(500) NOT NULL,
    Namespace NVARCHAR(1000) NOT NULL,
    Regenerate BIT NOT NULL,
    ApplicationId BIGINT NOT NULL,
    DomainFolderPathId BIGINT NOT NULL,
    FOREIGN KEY (ApplicationId) REFERENCES Application(Id),
    FOREIGN KEY (DomainFolderPathId) REFERENCES DomainFolderPath(Id)
);

CREATE TABLE CompanyPermission (
    CompanyId INT,
    PermissionId INT,
    PRIMARY KEY (CompanyId, PermissionId),
    FOREIGN KEY (CompanyId) REFERENCES Company(Id),
    FOREIGN KEY (PermissionId) REFERENCES Permission(Id)
);