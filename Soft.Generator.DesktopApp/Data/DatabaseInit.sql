CREATE DATABASE IF NOT EXISTS SoftGeneratorDA;
USE SoftGeneratorDA;

CREATE TABLE Company (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name NVARCHAR(500) NOT NULL,
    Email NVARCHAR(400) NOT NULL unique,
    Password NVARCHAR(100) NOT NULL
);

CREATE TABLE Permission (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name NVARCHAR(100) NOT NULL,
    Code NVARCHAR(100) NOT NULL
);

CREATE TABLE Framework (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Name NVARCHAR(500) NOT NULL,
    Code NVARCHAR(500) NOT NULL
);

CREATE TABLE DomainFolderPath (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    Path NVARCHAR(1000) NOT NULL
);

CREATE TABLE Setting (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    HasGoogleAuth BOOL NOT NULL,
    PrimaryColor NCHAR(7) NOT NULL,
    HasLatinTranslate BOOL NOT NULL,
    HasDarkMode BOOL NOT NULL,
    HasNotifications BOOL NOT NULL,
    FrameworkId INT NOT NULL,
    FOREIGN KEY (FrameworkId) REFERENCES Framework(Id)
);

CREATE TABLE Application (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    Name NVARCHAR(500) NOT NULL,
    CompanyId INT NOT NULL,
    SettingId BIGINT NOT NULL,
    FOREIGN KEY (CompanyId) REFERENCES Company(Id),
    FOREIGN KEY (SettingId) REFERENCES Setting(Id)
);

CREATE TABLE GeneratedFile (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    DisplayName NVARCHAR(500) NOT NULL,
    ClassName NVARCHAR(500) NOT NULL,
    Namespace NVARCHAR(1000) NOT NULL,
    Regenerate BOOL NOT NULL,
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
