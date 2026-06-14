using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mikroservisnaApp.Migrations
{
    public partial class PredavacNoIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
        ALTER TABLE [Angazovanja] DROP CONSTRAINT [FK_Angazovanja_Predavaci_PredavacId];
        SELECT * INTO [Predavaci_Backup] FROM [Predavaci];
        DROP TABLE [Predavaci];
        CREATE TABLE [Predavaci] (
            [Id] int NOT NULL,
            [Ime] nvarchar(max) NULL,
            [Prezime] nvarchar(max) NULL,
            [Titula] nvarchar(max) NULL,
            [OblastStrucnosti] nvarchar(max) NULL,
            CONSTRAINT [PK_Predavaci] PRIMARY KEY ([Id])
        );
        INSERT INTO [Predavaci] SELECT * FROM [Predavaci_Backup];
        DROP TABLE [Predavaci_Backup];
        ALTER TABLE [Angazovanja] ADD CONSTRAINT [FK_Angazovanja_Predavaci_PredavacId] 
            FOREIGN KEY ([PredavacId]) REFERENCES [Predavaci] ([Id]) ON DELETE CASCADE;
    ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SELECT * INTO [Predavaci_Backup] FROM [Predavaci];
                DROP TABLE [Predavaci];
                CREATE TABLE [Predavaci] (
                    [Id] int NOT NULL IDENTITY,
                    [Ime] nvarchar(max) NULL,
                    [Prezime] nvarchar(max) NULL,
                    [Titula] nvarchar(max) NULL,
                    [OblastStrucnosti] nvarchar(max) NULL,
                    CONSTRAINT [PK_Predavaci] PRIMARY KEY ([Id])
                );
                INSERT INTO [Predavaci] SELECT * FROM [Predavaci_Backup];
                DROP TABLE [Predavaci_Backup];
            ");
        }
    }
}