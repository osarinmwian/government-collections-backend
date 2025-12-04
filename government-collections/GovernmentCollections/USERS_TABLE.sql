-- Create Users table for PIN validation
CREATE TABLE Users (
    Id int IDENTITY(1,1) PRIMARY KEY,
    UserId nvarchar(100) NOT NULL UNIQUE,
    Pin nvarchar(256) NOT NULL, -- Hashed PIN
    CreatedAt datetime2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt datetime2 NULL
);

CREATE INDEX IX_Users_UserId ON Users(UserId);