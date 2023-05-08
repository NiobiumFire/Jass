IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'IIS APPPOOL\Network')
BEGIN
    CREATE LOGIN [IIS APPPOOL\Network] 
      FROM WINDOWS WITH DEFAULT_DATABASE=[master], 
      DEFAULT_LANGUAGE=[us_english]
END
GO
CREATE USER [BelotUser] 
  FOR LOGIN [IIS APPPOOL\Network]
GO
EXEC sp_addrolemember 'db_owner', 'BelotUser'
GO