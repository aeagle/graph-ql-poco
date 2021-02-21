CREATE TABLE [dbo].[Orders]
(
	[Id] INT NOT NULL IDENTITY(1,1) PRIMARY KEY,
	[Name] VARCHAR(50) NOT NULL,
	[CustomerId] INT NOT NULL, 
    CONSTRAINT [FK_Orders_Customers] FOREIGN KEY ([CustomerId]) REFERENCES [Customers]([Id])
)
