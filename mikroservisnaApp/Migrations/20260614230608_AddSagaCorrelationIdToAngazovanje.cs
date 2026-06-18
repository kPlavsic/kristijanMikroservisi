using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace mikroservisnaApp.Migrations
{
    /// <inheritdoc />
    public partial class AddSagaCorrelationIdToAngazovanje : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SagaCorrelationId",
                table: "Angazovanja",
                type: "uniqueidentifier",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SagaCorrelationId",
                table: "Angazovanja");
        }
    }
}
