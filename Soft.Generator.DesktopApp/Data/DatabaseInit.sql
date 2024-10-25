CREATE DATABASE IF NOT EXISTS SoftGeneratorDA;
USE SoftGeneratorDA;

CREATE TABLE Kompanija (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Ime NVARCHAR(500) NOT NULL,
    Email NVARCHAR(400) NOT NULL unique,
    Sifra NVARCHAR(100) NOT NULL
);

CREATE TABLE Permisija (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Ime NVARCHAR(100) NOT NULL,
    Kod NVARCHAR(100) NOT NULL
);

CREATE TABLE Okvir (
    Id INT PRIMARY KEY AUTO_INCREMENT,
    Ime NVARCHAR(500) NOT NULL,
    Kod NVARCHAR(500) NOT NULL
);

CREATE TABLE PutanjaDoDomenskogFoldera (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    Putanja NVARCHAR(1000) NOT NULL
);

CREATE TABLE Podesavanje (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    ImaGooglePrijavu BOOL NOT NULL,
    PrimarnaBoja NCHAR(7) NOT NULL,
    ImaPrevodNaLatinicu BOOL NOT NULL,
    ImaTamniRezim BOOL NOT NULL,
    ImaNotifikacije BOOL NOT NULL,
    OkvirId INT NOT NULL,
    FOREIGN KEY (OkvirId) REFERENCES Okvir(Id)
);

CREATE TABLE Aplikacija (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    Ime NVARCHAR(500) NOT NULL,
    KompanijaId INT NOT NULL,
    PodesavanjeId BIGINT NOT NULL,
    FOREIGN KEY (KompanijaId) REFERENCES Kompanija(Id),
    FOREIGN KEY (PodesavanjeId) REFERENCES Podesavanje(Id)
);

CREATE TABLE GenerisaniFajl (
    Id BIGINT PRIMARY KEY AUTO_INCREMENT,
    ImeZaPrikaz NVARCHAR(500) NOT NULL,
    ImeKlase NVARCHAR(500) NOT NULL,
    ImaProstora NVARCHAR(1000) NOT NULL,
    GenerisiPonovo BOOL NOT NULL,
    AplikacijaId BIGINT NOT NULL,
	PutanjaDoDomenskogFolderaId BIGINT NOT NULL,
    FOREIGN KEY (AplikacijaId) REFERENCES Aplikacija(Id),
    FOREIGN KEY (PutanjaDoDomenskogFolderaId) REFERENCES PutanjaDoDomenskogFoldera(Id)
);

CREATE TABLE KompanijaPermisija (
    KompanijaId INT,
    PermisijaId INT,
    PRIMARY KEY (KompanijaId, PermisijaId),
    FOREIGN KEY (KompanijaId) REFERENCES Kompanija(Id),
    FOREIGN KEY (PermisijaId) REFERENCES Permisija(Id)
);
