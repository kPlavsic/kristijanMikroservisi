CREATE TABLE [dbo].[OutboxMessages] (
    [Id] bigint IDENTITY(1,1) NOT NULL,
    [MessageId] nvarchar(max) NOT NULL,
    [EventType] nvarchar(max) NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
);